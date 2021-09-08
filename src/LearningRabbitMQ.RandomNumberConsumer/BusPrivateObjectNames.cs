namespace LearningRabbitMQ.RandomNumberConsumer
{
    public static class BusPrivateObjectNames
    {
        public const string OutgoingRequestExchange = "learning-rabbitmq.random-number-consumer.requests.x";
        public const string IncomingReplyExchange = "learning-rabbitmq.random-number-consumer.replies.x";
        public const string IncomingReplyQueue = "learning-rabbitmq.random-number-consumer.replies.q";
    }
}