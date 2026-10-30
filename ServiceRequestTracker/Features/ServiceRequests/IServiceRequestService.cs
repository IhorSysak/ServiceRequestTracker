using ServiceRequestTracker.Domain.Enums;

namespace ServiceRequestTracker.Features.ServiceRequests;

public interface IServiceRequestService
{
    Task<ServiceRequestResponse> CreateAsync(CreateServiceRequestRequest request, CancellationToken cancellationToken);

    Task<ServiceRequestResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<ServiceRequestResponse>> ListAsync(ListServiceRequestsQuery query, CancellationToken cancellationToken);

    Task<ServiceRequestResponse> ChangeStatusAsync(Guid id, ServiceRequestStatus newStatus, CancellationToken cancellationToken);
}