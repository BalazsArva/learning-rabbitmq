using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace LearningRabbitMQ.RandomNumberConsumer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<RequestSenderWorker>();
                    services.AddHostedService<ReplyReaderWorker>();

                    services.AddSingleton(new ConnectionFactory
                    {
                        Uri = new Uri("amqp://guest:guest@localhost:5672", UriKind.Absolute),
                    });

                    services.AddTransient(srv => srv.GetRequiredService<ConnectionFactory>().CreateConnection());
                });
    }
}