using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Domain;

public class ServiceRequest
{
    private ServiceRequest() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Priority Priority { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static ServiceRequest Create(string title, string? description, Priority priority, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Priority = priority,
            Status = ServiceRequestStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void ChangeStatus(ServiceRequestStatus newStatus, DateTime now)
    {
        if (!StatusTransitions.CanTransition(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        UpdatedAt = now;
    }
}