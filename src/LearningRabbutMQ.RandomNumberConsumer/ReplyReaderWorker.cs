using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LearningRabbitMQ.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LearningRabbutMQ.RandomNumberConsumer
{
    public class ReplyReaderWorker : IHostedService
    {
        private readonly IConnection connection;
        private readonly ILogger logger;
        private readonly IModel channel;

        private EventingBasicConsumer consumer;

        public ReplyReaderWorker(
            IConnection connection,
            ILogger<ReplyReaderWorker> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            channel = connection.CreateModel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            channel.ExchangeDeclare(BusObjectNames.IncomingReplyExchangeName, ExchangeType.Direct, true, false, null);
            channel.QueueDeclare(BusObjectNames.IncomingReplyQueueName, true, false, false, null);
            channel.QueueBind(BusObjectNames.IncomingReplyQueueName, BusObjectNames.IncomingReplyExchangeName, string.Empty, null);

            consumer = new EventingBasicConsumer(channel);

            consumer.Received += Consumer_Received;

            // TODO: Later, drop auto ack in favor of explicit ack
            channel.BasicConsume(BusObjectNames.IncomingReplyQueueName, true, consumer);

            logger.LogInformation("Reply listener started.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            channel.Close();
            connection.Close();

            logger.LogInformation("Listener stopped.");

            return Task.CompletedTask;
        }

        private void Consumer_Received(object sender, BasicDeliverEventArgs args)
        {
            var responseJson = Encoding.UTF8.GetString(args.Body.Span);
            var response = JsonConvert.DeserializeObject<GenerateRandomNumberReply>(responseJson);

            logger.LogInformation("Received random number response: {Number}.", response.RandomNumber);

            return;
        }
    }
}