namespace LearningRabbitMQ.RandomNumberConsumer
{
    public static class BusObjectNames
    {
        public const string OutgoingRequestExchangeName = "learning-rabbitmq.random-number-consumer.requests.x";
        public const string IncomingReplyExchangeName = "learning-rabbitmq.random-number-consumer.replies.x";
        public const string IncomingReplyQueueName = "learning-rabbitmq.random-number-consumer.replies.q";
    }
}