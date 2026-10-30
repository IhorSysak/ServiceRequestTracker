using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ServiceRequestTracker.Infrastructure.Messaging;
using ServiceRequestTracker.Infrastructure.Persistence;

namespace ServiceRequestTracker.Infrastructure.Outbox;

public class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnection rabbitMq,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<OutboxOptions> outboxOptions,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private static readonly CreateChannelOptions ConfirmChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    private readonly RabbitMqOptions _rabbitMq = rabbitMqOptions.Value;
    private readonly OutboxOptions _outbox = outboxOptions.Value;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started (batch {BatchSize}, poll {PollInterval})", _outbox.BatchSize, _outbox.PollInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var published = 0;
                try
                {
                    published = await PublishBatchAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Outbox publisher iteration failed");
                }

                if (published < _outbox.BatchSize)
                {
                    await Task.Delay(_outbox.PollInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await DisposeChannelAsync();
            logger.LogInformation("Outbox publisher stopped");
        }
    }

    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var batchSize = _outbox.BatchSize;
        var batch = await dbContext.OutboxMessages
            .FromSql($"""
                SELECT * FROM outbox_messages
                WHERE processed_at IS NULL AND next_attempt_at <= {now}
                ORDER BY occurred_at
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
        {
            return 0;
        }

        IChannel? channel = null;
        Exception? connectionError = null;
        try
        {
            channel = await GetChannelAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            connectionError = ex;
            logger.LogWarning(ex, "RabbitMQ is unavailable, {Count} outbox message(s) will be retried later", batch.Count);
        }

        var published = 0;
        foreach (var message in batch)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (channel is null)
                {
                    throw connectionError!;
                }

                await PublishAsync(channel, message, cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow().UtcDateTime);
                published++;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                var failedAt = timeProvider.GetUtcNow().UtcDateTime;
                message.MarkFailed(ex.Message, failedAt + GetBackoff(message.Attempts + 1));

                if (connectionError is null)
                {
                    logger.LogWarning(ex, "Publishing outbox message {MessageId} failed (attempt {Attempt}), next try at {NextAttemptAt:O}",
                        message.Id, message.Attempts, message.NextAttemptAt);
                }
            }
        }

        await dbContext.SaveChangesAsync(CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);

        if (published > 0)
        {
            logger.LogInformation("Published {Count} outbox message(s)", published);
        }

        return published;
    }

    private async Task PublishAsync(IChannel channel, OutboxMessage message, CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Persistent = true,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredAt, TimeSpan.Zero).ToUnixTimeSeconds())
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_outbox.PublishTimeout);

        try
        {
            await channel.BasicPublishAsync(
                _rabbitMq.Exchange,
                _rabbitMq.RoutingKey,
                mandatory: true,
                properties,
                Encoding.UTF8.GetBytes(message.Payload),
                timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No publisher confirm within {_outbox.PublishTimeout}.");
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await DisposeChannelAsync();
        _channel = await rabbitMq.CreateChannelAsync(ConfirmChannelOptions, cancellationToken);
        return _channel;
    }

    private async Task DisposeChannelAsync()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            await _channel.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error while disposing the publisher channel");
        }

        _channel = null;
    }

    private TimeSpan GetBackoff(int attempt)
    {
        var ticks = _outbox.InitialBackoff.Ticks * Math.Pow(2, Math.Min(attempt - 1, 20));
        return TimeSpan.FromTicks((long)Math.Min(ticks, _outbox.MaxBackoff.Ticks));
    }
}