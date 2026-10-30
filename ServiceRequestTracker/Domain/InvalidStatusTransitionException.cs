using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Domain;

public class InvalidStatusTransitionException(ServiceRequestStatus from, ServiceRequestStatus to)
    : Exception($"Cannot change status from {from} to {to}")
{
    public ServiceRequestStatus From { get; } = from;

    public ServiceRequestStatus To { get; } = to;
}