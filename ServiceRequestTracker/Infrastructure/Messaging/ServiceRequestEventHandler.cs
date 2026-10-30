using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ServiceRequestTracker.Domain.Enums;
using ServiceRequestTracker.Infrastructure.Persistence;

namespace ServiceRequestTracker.Infrastructure.Messaging;

public class ServiceRequestEventHandler(
    IProcessedEventStore store,
    TimeProvider timeProvider,
    ILogger<ServiceRequestEventHandler> logger)
{
    public static bool TryParse(string payload, [NotNullWhen(true)] out ServiceRequestChangedEvent? message)
    {
        try
        {
            message = JsonSerializer.Deserialize<ServiceRequestChangedEvent>(payload, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            message = null;
        }

        if (message is null
            || message.MessageId == Guid.Empty
            || message.RequestId == Guid.Empty
            || string.IsNullOrWhiteSpace(message.NewStatus)
            || string.IsNullOrWhiteSpace(message.Priority))
        {
            message = null;
            return false;
        }

        return true;
    }

    public static bool IsNotable(ServiceRequestChangedEvent message) =>
        message.Priority == nameof(Priority.High) || message.NewStatus == nameof(ServiceRequestStatus.Done);

    public async Task HandleAsync(ServiceRequestChangedEvent message, string rawPayload, CancellationToken cancellationToken)
    {
        var processedEvent = ProcessedEvent.Create(message, rawPayload, timeProvider.GetUtcNow().UtcDateTime);

        if (!await store.TryAddAsync(processedEvent, cancellationToken))
        {
            logger.LogInformation("Duplicate message {MessageId} ignored", message.MessageId);
            return;
        }

        if (IsNotable(message))
        {
            logger.LogInformation(
                "Service request event {MessageId}: request {RequestId} {OldStatus} -> {NewStatus}, priority {Priority}, occurred at {OccurredAt:O}",
                message.MessageId, message.RequestId, message.OldStatus ?? "(none)", message.NewStatus, message.Priority, message.OccurredAt);
        }
    }
}