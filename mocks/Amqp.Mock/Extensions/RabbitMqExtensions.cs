using System.Text.Json;
using System.Text.RegularExpressions;
using RabbitMQ.Client;

namespace Amqp.Mock.Extensions;

internal static class RabbitMqExtensions
{
    internal static async void AddRabbit(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Rabbit");
        var options = section.Get<RabbitOptions>() ?? new();
        services.AddSingleton<RabbitOptions>(options);

        services.AddSingleton<IConnection>(_ =>
        {
            var factory = new ConnectionFactory
            {
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                HostName = options.HostName,
                Port = options.Port
            };

            var endpoints = options.Endpoints.Select(e => new AmqpTcpEndpoint(e));
            return factory.CreateConnectionAsync(endpoints).Result;
        });

        services.AddSingleton<IChannel>(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            var channel = connection.CreateChannelAsync().Result;

            options.Queues.ForEach(queue =>
            {
                channel.ExchangeDeclareAsync(queue.ExchangeOrigin, queue.ExchangeTypeOrigin).GetAwaiter();
                channel.QueueDeclareAsync(queue.QueueOrigin, durable: false, exclusive: false, autoDelete: false, arguments: null).GetAwaiter();
                channel.QueueBindAsync(queue.QueueOrigin, queue.ExchangeOrigin, routingKey: "").GetAwaiter();

                channel.ExchangeDeclareAsync(queue.ExchangeDestination, queue.ExchangeTypeDestination).GetAwaiter();
                queue.QueuesDestination.ForEach(dest =>
                {
                    channel.QueueDeclareAsync(dest, durable: false, exclusive: false, autoDelete: false, arguments: null).GetAwaiter();
                    channel.QueueBindAsync(dest, queue.ExchangeDestination, routingKey: "").GetAwaiter();
                });

            });

            return channel;
        });

        services.AddSingleton<IProducer, Producer>();
        services.AddHostedService<Consumer>();
    }

    internal class RabbitOptions
    {
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; } = 5672;
        public string HostName { get; set; } = "localhost";
        public IEnumerable<string> Endpoints { get; set; } = ["localhost"];
        public List<QueueOptions> Queues { get; set; } = [];

        internal class QueueOptions
        {
            public string ExchangeOrigin { get; set; } = "";
            public string ExchangeTypeOrigin { get; set; } = ExchangeType.Direct;
            public string QueueOrigin { get; set; } = "";
            public string ExchangeDestination { get; set; } = "";
            public string ExchangeTypeDestination { get; set; } = ExchangeType.Direct;
            public List<string> QueuesDestination { get; set; } = [];
            public List<Condition> Conditions { get; set; } = [];
        }

        internal class Condition
        {
            public string Type { get; set; } = "";
            public string Value { get; set; } = "";
            public string Response { get; set; } = "";
            internal bool IsSatisfied { get; private set; } = false;

            public void Verify(string message)
            {
                if (Type == "contains")
                {
                    IsSatisfied = message.Contains(Value, StringComparison.InvariantCultureIgnoreCase);
                    return;
                }

                if (Type == "regex")
                {
                    IsSatisfied = Regex.IsMatch(message, Value);
                    return;
                }

                IsSatisfied = false;
            }
        };
    }
}
