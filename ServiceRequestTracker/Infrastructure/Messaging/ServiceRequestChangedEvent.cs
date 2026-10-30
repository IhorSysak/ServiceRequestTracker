using ServiceRequestTracker.Domain;
using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Infrastructure.Messaging;

public record ServiceRequestChangedEvent(
    Guid MessageId,
    Guid RequestId,
    string? OldStatus,
    string NewStatus,
    string Priority,
    DateTime OccurredAt,
    int SchemaVersion = ServiceRequestChangedEvent.CurrentSchemaVersion)
{
    public const int CurrentSchemaVersion = 1;

    public const string MessageType = "ServiceRequestChanged";

    public static ServiceRequestChangedEvent From(ServiceRequest request, ServiceRequestStatus? oldStatus, DateTime occurredAt) =>
        new(
            MessageId: Guid.NewGuid(),
            RequestId: request.Id,
            OldStatus: oldStatus?.ToString(),
            NewStatus: request.Status.ToString(),
            Priority: request.Priority.ToString(),
            OccurredAt: occurredAt);
}