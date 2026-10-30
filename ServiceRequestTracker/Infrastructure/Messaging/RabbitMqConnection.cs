using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ServiceRequestTracker.Infrastructure.Messaging;

public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IChannel> CreateChannelAsync(CreateChannelOptions? channelOptions, CancellationToken cancellationToken)
    {
        var connection = await EnsureConnectedAsync(cancellationToken);
        return await connection.CreateChannelAsync(channelOptions, cancellationToken);
    }

    public async Task<IConnection> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null)
            {
                return _connection;
            }

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.User,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                ClientProvidedName = "service-request-tracker",
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            var connection = await factory.CreateConnectionAsync(cancellationToken);
            try
            {
                await DeclareTopologyAsync(connection, settings, cancellationToken);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }

            _connection = connection;
            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, topology declared", settings.Host, settings.Port);

            return connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task DeclareTopologyAsync(IConnection connection, RabbitMqOptions settings, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(settings.DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(settings.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(settings.DeadLetterQueue, settings.DeadLetterExchange, routingKey: string.Empty, cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            settings.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = settings.DeadLetterExchange },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(settings.Queue, settings.Exchange, settings.RoutingKey, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error while closing the RabbitMQ connection");
            }

            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}