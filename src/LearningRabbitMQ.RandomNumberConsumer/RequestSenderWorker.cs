using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LearningRabbitMQ.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace LearningRabbitMQ.RandomNumberConsumer
{
    public class RequestSenderWorker : BackgroundService
    {
        private readonly IConnection connection;
        private readonly ILogger logger;
        private readonly IModel channel;

        public RequestSenderWorker(
            IConnection connection,
            ILogger<RequestSenderWorker> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            channel = connection.CreateModel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            channel.ExchangeDeclare(BusObjectNames.OutgoingRequestExchangeName, ExchangeType.Direct, true, false);
            channel.QueueBind(Addresses.RandomNumberServiceInboundQueueName, BusObjectNames.OutgoingRequestExchangeName, string.Empty, null);

            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new GenerateRandomNumberRequest
                {
                    Min = 0,
                    Max = 1_000_000,
                };

                var requestJson = JsonConvert.SerializeObject(request);
                var requestJsonBytes = Encoding.UTF8.GetBytes(requestJson);

                var props = channel.CreateBasicProperties();

                props.ReplyTo = BusObjectNames.IncomingReplyExchangeName;

                channel.BasicPublish(BusObjectNames.OutgoingRequestExchangeName, string.Empty, props, requestJsonBytes);

                logger.LogInformation("Sent request to message bus.");

                await Task.Delay(500, stoppingToken);
            }

            channel.Close();
            connection.Close();
        }
    }
}