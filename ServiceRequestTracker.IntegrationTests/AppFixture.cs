using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ServiceRequestTracker.Infrastructure.Messaging;
using ServiceRequestTracker.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace ServiceRequestTracker.IntegrationTests;

public sealed class AppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management").Build();
    private ApiFactory? _factory;

    public HttpClient Client { get; private set; } = null!;

    private IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        _factory = new ApiFactory(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
            ["RabbitMq:Host"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort).ToString(),
            ["RabbitMq:User"] = RabbitMqBuilder.DefaultUsername,
            ["RabbitMq:Password"] = RabbitMqBuilder.DefaultPassword,
            ["Outbox:PollInterval"] = "00:00:00.200"
        });

        Client = _factory.CreateClient();

        await WaitForQueueAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async Task PublishAsync(params string[] payloads)
    {
        var settings = Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        await using var connection = await CreateConnectionFactory(settings).CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        foreach (var payload in payloads)
        {
            var properties = new BasicProperties
            {
                MessageId = JsonDocument.Parse(payload).RootElement.GetProperty("messageId").GetString(),
                ContentType = "application/json",
                Persistent = true
            };

            await channel.BasicPublishAsync(settings.Exchange, settings.RoutingKey, mandatory: true, properties, System.Text.Encoding.UTF8.GetBytes(payload));
        }
    }

    private async Task WaitForQueueAsync()
    {
        var settings = Services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            try
            {
                await using var connection = await CreateConnectionFactory(settings).CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();
                await channel.QueueDeclarePassiveAsync(settings.Queue);
                return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(200);
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory(RabbitMqOptions settings) => new()
    {
        HostName = settings.Host,
        Port = settings.Port,
        UserName = settings.User,
        Password = settings.Password,
        VirtualHost = settings.VirtualHost
    };

    private sealed class ApiFactory(IReadOnlyDictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }
    }
}

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "Integration";
}