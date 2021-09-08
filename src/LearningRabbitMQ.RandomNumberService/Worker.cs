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

namespace LearningRabbitMQ.RandomNumberService
{
    public class Worker : IHostedService
    {
        private readonly IConnection connection;
        private readonly ILogger logger;
        private readonly IModel channel;
        private readonly Random random = new();

        private AsyncEventingBasicConsumer consumer;

        public Worker(
            IConnection connection,
            ILogger<Worker> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            channel = connection.CreateModel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            channel.QueueDeclare(Addresses.RandomNumberServiceInboundQueueName, true, false, false, null);

            consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;

            channel.BasicConsume(Addresses.RandomNumberServiceInboundQueueName, false, consumer);

            logger.LogInformation("Listener started.");

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
            var props = args.BasicProperties;

            if (props is null)
            {
                logger.LogWarning("Received a message with empty basic properties, reply to is missing.");
                return Task.CompletedTask;
            }

            if (!props.IsReplyToPresent() || string.IsNullOrWhiteSpace(props.ReplyTo))
            {
                logger.LogWarning("Received a message with empty reply to header value.");
                return Task.CompletedTask;
            }

            var request = JsonConvert.DeserializeObject<GenerateRandomNumberRequest>(Encoding.UTF8.GetString(args.Body.Span));
            var response = new GenerateRandomNumberReply
            {
                RandomNumber = random.Next(request.Min, request.Max),
            };

            var responseJson = JsonConvert.SerializeObject(response);
            var responseJsonBytes = Encoding.UTF8.GetBytes(responseJson);

            channel.BasicPublish(props.ReplyTo, string.Empty, channel.CreateBasicProperties(), responseJsonBytes);
            channel.BasicAck(args.DeliveryTag, false);

            logger.LogInformation("Successfully responded to request with delivery tag {DeliveryTag} with result {Number}.", args.DeliveryTag, response.RandomNumber);

            return Task.CompletedTask;
        }
    }
}