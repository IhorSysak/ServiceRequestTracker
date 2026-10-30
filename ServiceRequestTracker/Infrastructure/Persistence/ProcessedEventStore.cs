using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ServiceRequestTracker.Infrastructure.Persistence;

public interface IProcessedEventStore
{
    Task<bool> TryAddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken);
}

public class ProcessedEventStore(AppDbContext dbContext) : IProcessedEventStore
{
    public async Task<bool> TryAddAsync(ProcessedEvent processedEvent, CancellationToken cancellationToken)
    {
        dbContext.ProcessedEvents.Add(processedEvent);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(processedEvent).State = EntityState.Detached;
            return false;
        }
    }
}