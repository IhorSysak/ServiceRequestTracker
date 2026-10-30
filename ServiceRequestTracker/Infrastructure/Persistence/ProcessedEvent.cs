using ServiceRequestTracker.Infrastructure.Messaging;

namespace ServiceRequestTracker.Infrastructure.Persistence;

public class ProcessedEvent
{
    private ProcessedEvent() { }

    public Guid MessageId { get; private set; }

    public Guid RequestId { get; private set; }

    public string? OldStatus { get; private set; }

    public string NewStatus { get; private set; } = string.Empty;

    public string Priority { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public string RawPayload { get; private set; } = string.Empty;

    public static ProcessedEvent Create(ServiceRequestChangedEvent message, string rawPayload, DateTime receivedAt) =>
        new()
        {
            MessageId = message.MessageId,
            RequestId = message.RequestId,
            OldStatus = message.OldStatus,
            NewStatus = message.NewStatus,
            Priority = message.Priority,
            OccurredAt = message.OccurredAt,
            ReceivedAt = receivedAt,
            RawPayload = rawPayload
        };
}