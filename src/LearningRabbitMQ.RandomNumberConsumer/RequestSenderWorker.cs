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
            channel.ExchangeDeclare(BusPrivateObjectNames.OutgoingRequestExchange, ExchangeType.Direct, true, false);
            channel.QueueBind(BusSharedObjectNames.RandomNumberServiceInboundQueue, BusPrivateObjectNames.OutgoingRequestExchange, string.Empty, null);

            var i = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var request = new GenerateRandomNumberRequest
                {
                    Min = 0,
                    Max = 1_000_000,
                    MessageSequenceNumber = ++i,
                };

                var requestJson = JsonConvert.SerializeObject(request);
                var requestJsonBytes = Encoding.UTF8.GetBytes(requestJson);

                var props = channel.CreateBasicProperties();

                props.ReplyTo = BusPrivateObjectNames.IncomingReplyExchange;

                channel.BasicPublish(BusPrivateObjectNames.OutgoingRequestExchange, string.Empty, props, requestJsonBytes);

                logger.LogInformation("Sent request to message bus.");

                await Task.Delay(50, stoppingToken);
            }

            channel.Close();
            connection.Close();
        }
    }
}