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

namespace LearningRabbitMQ.RandomNumberConsumer
{
    public class ReplyReaderWorker : IHostedService
    {
        private readonly IConnection connection;
        private readonly ILogger logger;
        private readonly IModel channel;

        private AsyncEventingBasicConsumer consumer;

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
            channel.ExchangeDeclare(BusPrivateObjectNames.IncomingReplyExchange, ExchangeType.Direct, true, false, null);
            channel.QueueDeclare(BusPrivateObjectNames.IncomingReplyQueue, true, false, false, null);
            channel.QueueBind(BusPrivateObjectNames.IncomingReplyQueue, BusPrivateObjectNames.IncomingReplyExchange, string.Empty, null);

            channel.CallbackException += (sender, args) =>
            {
                logger.LogError(args.Exception, "An unhandled error occurred while processing the message.");
            };

            consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;

            channel.BasicConsume(BusPrivateObjectNames.IncomingReplyQueue, false, consumer);

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

        private Task Consumer_Received(object sender, BasicDeliverEventArgs args)
        {
            var responseJson = Encoding.UTF8.GetString(args.Body.Span);
            var response = JsonConvert.DeserializeObject<GenerateRandomNumberReply>(responseJson);

            logger.LogInformation("Received random number response: {Number}.", response.RandomNumber);

            channel.BasicAck(args.DeliveryTag, false);

            return Task.CompletedTask;
        }
    }
}