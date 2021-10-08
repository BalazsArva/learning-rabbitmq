namespace LearningRabbitMQ.Contracts
{
    public class GenerateRandomNumberRequest
    {
        public int Min { get; set; }

        public int Max { get; set; }

        public int MessageSequenceNumber { get; set; }
    }
}