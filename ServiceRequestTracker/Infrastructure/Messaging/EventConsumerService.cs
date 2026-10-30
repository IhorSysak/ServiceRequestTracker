using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ServiceRequestTracker.Infrastructure.Messaging;

public class EventConsumerService(
    RabbitMqConnection rabbitMq,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<EventConsumerService> logger) : BackgroundService
{
    private static readonly TimeSpan ConnectRetryDelay = TimeSpan.FromSeconds(2);

    private readonly RabbitMqOptions _settings = options.Value;
    private CancellationToken _stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        IChannel? channel = null;
        string? consumerTag = null;

        try
        {
            while (consumerTag is null)
            {
                try
                {
                    channel = await rabbitMq.CreateChannelAsync(null, stoppingToken);
                    await channel.BasicQosAsync(0, (ushort)_settings.PrefetchCount, global: false, stoppingToken);

                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.ReceivedAsync += OnReceivedAsync;

                    consumerTag = await channel.BasicConsumeAsync(_settings.Queue, autoAck: false, consumer, stoppingToken);
                    logger.LogInformation("Consuming queue {Queue} (prefetch {Prefetch})", _settings.Queue, _settings.PrefetchCount);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Cannot start the consumer, retrying in {Delay}s", ConnectRetryDelay.TotalSeconds);
                    await DisposeAsync(channel);
                    channel = null;
                    await Task.Delay(ConnectRetryDelay, stoppingToken);
                }
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            await StopAsync(channel, consumerTag);
        }
    }

    private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Channel;
        var payload = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            if (!ServiceRequestEventHandler.TryParse(payload, out var message))
            {
                logger.LogWarning("Malformed message {MessageId} rejected to the dead-letter queue: {Payload}", args.BasicProperties.MessageId, payload);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, _stoppingToken);
                return;
            }

            try
            {
                await HandleWithRetryAsync(message, payload);
            }
            catch (Exception ex) when (!_stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Message {MessageId} failed after {Retries} retries and is rejected to the dead-letter queue",
                    message.MessageId, _settings.ConsumerMaxRetries);
                await channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, _stoppingToken);
                return;
            }

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, _stoppingToken);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not acknowledge delivery {DeliveryTag}; it will be redelivered", args.DeliveryTag);
        }
    }

    private async Task HandleWithRetryAsync(ServiceRequestChangedEvent message, string payload)
    {
        for (var retry = 0; ; retry++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ServiceRequestEventHandler>();
                await handler.HandleAsync(message, payload, _stoppingToken);
                return;
            }
            catch (Exception ex) when (retry < _settings.ConsumerMaxRetries && !_stoppingToken.IsCancellationRequested)
            {
                var delay = _settings.ConsumerRetryDelay * Math.Pow(2, retry);
                logger.LogWarning(ex, "Handling message {MessageId} failed (retry {Retry}/{MaxRetries}), retrying in {Delay}s",
                    message.MessageId, retry + 1, _settings.ConsumerMaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, _stoppingToken);
            }
        }
    }

    private async Task StopAsync(IChannel? channel, string? consumerTag)
    {
        if (channel is null)
        {
            return;
        }

        try
        {
            if (consumerTag is not null)
            {
                await channel.BasicCancelAsync(consumerTag);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error while cancelling the consumer");
        }

        await DisposeAsync(channel);
        logger.LogInformation("Event consumer stopped");
    }

    private async Task DisposeAsync(IChannel? channel)
    {
        if (channel is null)
        {
            return;
        }

        try
        {
            await channel.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error while disposing the consumer channel");
        }
    }
}