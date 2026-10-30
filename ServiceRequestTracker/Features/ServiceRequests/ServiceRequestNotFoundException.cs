namespace ServiceRequestTracker.Features.ServiceRequests;

public class ServiceRequestNotFoundException(Guid id) : Exception($"Service request '{id}' was not found")
{
    public Guid Id { get; } = id;
}