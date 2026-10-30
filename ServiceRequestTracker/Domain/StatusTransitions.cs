using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Domain;

public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<ServiceRequestStatus, IReadOnlyCollection<ServiceRequestStatus>> Allowed =
        new Dictionary<ServiceRequestStatus, IReadOnlyCollection<ServiceRequestStatus>>
        {
            [ServiceRequestStatus.New] = [ServiceRequestStatus.InProgress, ServiceRequestStatus.Cancelled],
            [ServiceRequestStatus.InProgress] = [ServiceRequestStatus.Done, ServiceRequestStatus.Cancelled],
            [ServiceRequestStatus.Cancelled] = [ServiceRequestStatus.New],
            [ServiceRequestStatus.Done] = []
        };

    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to) =>
        GetAllowedTargets(from).Contains(to);

    public static IReadOnlyCollection<ServiceRequestStatus> GetAllowedTargets(ServiceRequestStatus from) =>
        Allowed[from];
}