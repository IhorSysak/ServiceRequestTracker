using Microsoft.EntityFrameworkCore;

namespace ServiceRequestTracker.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHostedService<DatabaseMigrator>();

        return services;
    }
}