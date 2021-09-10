using RabbitMQ.Client;

namespace LearningRabbitMQ.RandomNumberService.Configuration
{
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";

        public string Host { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string VirtualHost { get; set; } = ConnectionFactory.DefaultVHost;

        public string Username { get; set; } = ConnectionFactory.DefaultUser;

        public string Password { get; set; } = ConnectionFactory.DefaultPass;
    }
}