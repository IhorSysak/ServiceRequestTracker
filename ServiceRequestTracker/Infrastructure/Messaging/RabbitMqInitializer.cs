namespace ServiceRequestTracker.Infrastructure.Messaging;

public class RabbitMqInitializer(RabbitMqConnection connection, ILogger<RabbitMqInitializer> logger) : BackgroundService
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await connection.EnsureConnectedAsync(stoppingToken);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "RabbitMQ is not available, retrying in {Delay}s", RetryDelay.TotalSeconds);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }
}