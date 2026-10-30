using System.ComponentModel.DataAnnotations;

namespace ServiceRequestTracker.Infrastructure.Outbox;

public class OutboxOptions
{
    public const string SectionName = "Outbox";

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 50;

    public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(2);

    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan PublishTimeout { get; init; } = TimeSpan.FromSeconds(10);
}