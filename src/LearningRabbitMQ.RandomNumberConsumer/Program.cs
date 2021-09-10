using LearningRabbitMQ.RandomNumberConsumer.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
                    services.Configure<RabbitMqOptions>(hostContext.Configuration.GetSection(RabbitMqOptions.SectionName));

                    services.AddHostedService<RequestSenderWorker>();
                    services.AddHostedService<ReplyReaderWorker>();

                    services.AddSingleton(srvProvider =>
                    {
                        var rabbitMqOpts = srvProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                        return new ConnectionFactory
                        {
                            HostName = rabbitMqOpts.Host,
                            Port = rabbitMqOpts.Port,
                            VirtualHost = rabbitMqOpts.VirtualHost,
                            UserName = rabbitMqOpts.Username,
                            Password = rabbitMqOpts.Password,
                            DispatchConsumersAsync = true,
                        };
                    });

                    services.AddTransient(srv => srv.GetRequiredService<ConnectionFactory>().CreateConnection());
                });
    }
}