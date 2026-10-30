using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Features.ServiceRequests;

public record CreateServiceRequestRequest(string Title, string? Description, Priority? Priority);

public record ChangeStatusRequest(ServiceRequestStatus? Status);

public record ListServiceRequestsQuery(ServiceRequestStatus? Status, int Page = 1, int PageSize = 20);

public record ServiceRequestResponse(
    Guid Id,
    string Title,
    string? Description,
    Priority Priority,
    ServiceRequestStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);