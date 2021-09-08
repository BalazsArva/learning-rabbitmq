using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace LearningRabbitMQ.RandomNumberService
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
                    services.AddHostedService<Worker>();

                    services.AddSingleton(new ConnectionFactory
                    {
                        Uri = new Uri("amqp://guest:guest@localhost:5672", UriKind.Absolute),
                        DispatchConsumersAsync = true,
                    });

                    services.AddTransient(srv => srv.GetRequiredService<ConnectionFactory>().CreateConnection());
                });
    }
}