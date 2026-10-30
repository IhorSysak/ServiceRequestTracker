using Microsoft.EntityFrameworkCore;
using ServiceRequestTracker.Domain;
using ServiceRequestTracker.Domain.Enums;
using ServiceRequestTracker.Infrastructure.Persistence;

namespace ServiceRequestTracker.Features.ServiceRequests;

public class ServiceRequestService(AppDbContext dbContext, TimeProvider timeProvider, ILogger<ServiceRequestService> logger)
    : IServiceRequestService
{
    public async Task<ServiceRequestResponse> CreateAsync(CreateServiceRequestRequest request, CancellationToken cancellationToken)
    {
        var entity = ServiceRequest.Create(
            request.Title,
            request.Description,
            request.Priority ?? Priority.Normal,
            timeProvider.GetUtcNow().UtcDateTime);

        dbContext.ServiceRequests.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} created", entity.Id);
        return ToResponse(entity);
    }

    public async Task<ServiceRequestResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ServiceRequestNotFoundException(id);

        return ToResponse(entity);
    }

    public async Task<PagedResponse<ServiceRequestResponse>> ListAsync(ListServiceRequestsQuery query, CancellationToken cancellationToken)
    {
        var requests = dbContext.ServiceRequests.AsNoTracking();

        if (query.Status is { } status)
        {
            requests = requests.Where(x => x.Status == status);
        }

        var totalCount = await requests.CountAsync(cancellationToken);

        var items = await requests
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ServiceRequestResponse>(items.Select(ToResponse).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<ServiceRequestResponse> ChangeStatusAsync(Guid id, ServiceRequestStatus newStatus, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ServiceRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ServiceRequestNotFoundException(id);

        var oldStatus = entity.Status;
        entity.ChangeStatus(newStatus, timeProvider.GetUtcNow().UtcDateTime);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service request {RequestId} status changed {OldStatus} -> {NewStatus}", id, oldStatus, newStatus);
        return ToResponse(entity);
    }

    private static ServiceRequestResponse ToResponse(ServiceRequest entity) =>
        new(entity.Id, entity.Title, entity.Description, entity.Priority, entity.Status, entity.CreatedAt, entity.UpdatedAt);
}