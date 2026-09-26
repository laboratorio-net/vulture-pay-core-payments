using System.Text;
using RabbitMQ.Client;

namespace Amqp.Mock;

interface IProducer
{
    Task ProduceAsync(string message, string exchangeDestination, CancellationToken stoppingToken);
}


internal class Producer(
    ILogger<Producer> logger,
    IChannel channel) : IProducer
{
    public async Task ProduceAsync(string message, string exchangeDestination, CancellationToken stoppingToken)
    {
        var props = new BasicProperties { Persistent = true };
        byte[] body = Encoding.UTF8.GetBytes(message.ToString());
        try
        {
            await channel.BasicPublishAsync(
                exchange: exchangeDestination,
                routingKey: "",
                body: body,
                basicProperties: props,
                mandatory: true,
                cancellationToken: stoppingToken);

            logger.LogInformation("{0} [x] Message sent. Message: {message}", DateTime.Now, message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{0} [ERROR] saw nack or return.", DateTime.Now);
        }
    }
}
