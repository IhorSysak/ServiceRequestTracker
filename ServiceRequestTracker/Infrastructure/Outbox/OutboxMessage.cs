using System.Text.Json;
using ServiceRequestTracker.Infrastructure.Messaging;

namespace ServiceRequestTracker.Infrastructure.Outbox;

public class OutboxMessage
{
    private const int MaxErrorLength = 2000;

    private OutboxMessage() { }
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime NextAttemptAt { get; private set; }

    public static OutboxMessage Create(ServiceRequestChangedEvent message) =>
        new()
        {
            Id = message.MessageId,
            Type = ServiceRequestChangedEvent.MessageType,
            Payload = JsonSerializer.Serialize(message, JsonSerializerOptions.Web),
            OccurredAt = message.OccurredAt,
            NextAttemptAt = message.OccurredAt
        };

    public void MarkProcessed(DateTime now) => ProcessedAt = now;

    public void MarkFailed(string error, DateTime nextAttemptAt)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        NextAttemptAt = nextAttemptAt;
    }
}