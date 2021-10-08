using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LearningRabbitMQ.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace LearningRabbitMQ.RandomNumberService
{
    public class RandomNumberWorkerService : IHostedService
    {
        private readonly IConnection connection;
        private readonly ILogger logger;
        private readonly IModel channel;
        private readonly Random random = new();

        private AsyncEventingBasicConsumer consumer;

        public RandomNumberWorkerService(
            IConnection connection,
            ILogger<RandomNumberWorkerService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            channel = connection.CreateModel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            channel.BasicQos(0, 5, false);

            channel.ExchangeDeclare(BusPrivateObjectNames.DeadletterExchange, ExchangeType.Direct, true, false, null);
            channel.QueueDeclare(BusPrivateObjectNames.DeadletterQueue, true, false, false, null);
            channel.QueueBind(BusPrivateObjectNames.DeadletterQueue, BusPrivateObjectNames.DeadletterExchange, string.Empty, null);

            channel.QueueDeclare(BusSharedObjectNames.RandomNumberServiceInboundQueue, true, false, false, new Dictionary<string, object>()
            {
                [Headers.XDeadLetterExchange] = BusPrivateObjectNames.DeadletterExchange,
            });

            consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += Consumer_Received;

            channel.BasicConsume(BusSharedObjectNames.RandomNumberServiceInboundQueue, false, consumer);

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

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs args)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var props = args.BasicProperties;

            if (props is null)
            {
                logger.LogWarning("Received a message with empty basic properties, reply to is missing.");
                channel.BasicNack(args.DeliveryTag, false, false);

                return;
            }

            if (!props.IsReplyToPresent() || string.IsNullOrWhiteSpace(props.ReplyTo))
            {
                logger.LogWarning("Received a message with empty reply to header value.");
                channel.BasicNack(args.DeliveryTag, false, false);

                return;
            }

            if (!ExchangeExists(props.ReplyTo))
            {
                logger.LogWarning("Received a message with an invalid reply to exchange. The exchange '{ExchangeName}' does not exist.", props.ReplyTo);
                channel.BasicNack(args.DeliveryTag, false, false);

                return;
            }

            var request = JsonConvert.DeserializeObject<GenerateRandomNumberRequest>(Encoding.UTF8.GetString(args.Body.Span));
            var response = new GenerateRandomNumberReply
            {
                RandomNumber = random.Next(request.Min, request.Max),
            };

            logger.LogInformation("Message number {SequenceNumber}", request.MessageSequenceNumber);

            var responseJson = JsonConvert.SerializeObject(response);
            var responseJsonBytes = Encoding.UTF8.GetBytes(responseJson);

            channel.BasicPublish(props.ReplyTo, string.Empty, channel.CreateBasicProperties(), responseJsonBytes);
            channel.BasicAck(args.DeliveryTag, false);

            logger.LogInformation("Successfully responded to request with delivery tag {DeliveryTag} with result {Number}.", args.DeliveryTag, response.RandomNumber);
        }

        private bool ExchangeExists(string exchangeName)
        {
            // When the exchange does not exist, it causes a protocol error and that shuts down the channel. For this reason, we are not using the main channel,
            // but only a temporary one, so that we do not create race conditions (consuming happens on multiple threads).
            using var checkChannel = connection.CreateModel();

            try
            {
                checkChannel.ExchangeDeclarePassive(exchangeName);

                return true;
            }
            catch (OperationInterruptedException e) when (e.ShutdownReason.ReplyCode == Constants.NotFound)
            {
                return false;
            }
            finally
            {
                checkChannel.Close();
            }
        }
    }

    public static class BusPrivateObjectNames
    {
        public const string DeadletterExchange = "learning-rabbitmq.random-number-service.deadletter.x";
        public const string DeadletterQueue = "learning-rabbitmq.random-number-service.deadletter.q";
    }
}