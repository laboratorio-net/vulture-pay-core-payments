using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static Amqp.Mock.Extensions.RabbitMqExtensions;
using static Amqp.Mock.Extensions.RabbitMqExtensions.RabbitOptions;

namespace Amqp.Mock;

internal class Consumer(
    ILogger<Consumer> logger,
    IProducer producer,
    IChannel channel,
    IConnection connection,
    RabbitOptions options)
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        options.Queues.ForEach(async queue =>
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (chn, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                logger.LogInformation(" [x][{queue}] Received {message}", queue.QueueOrigin, message);

                var response = GetResponse(queue.Conditions, message);
                return producer.ProduceAsync(response, queue.ExchangeDestination, stoppingToken);

            };

            await channel.BasicConsumeAsync(
                queue.QueueOrigin,
                autoAck: true,
                consumer: consumer,
                cancellationToken: stoppingToken);
        });
    }

    private static string GetResponse(List<Condition> conditions, string message)
    {
        var response = message;
        if (conditions is { Count: > 0 })
        {
            foreach (var cond in conditions)
            {
                cond.Verify(message);
                if (cond.IsSatisfied)
                {
                    response = cond.Response;
                    break;
                }
            }
        }

        return response;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await channel.CloseAsync(cancellationToken);
        channel.Dispose();

        await connection.CloseAsync(cancellationToken);
        connection.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
