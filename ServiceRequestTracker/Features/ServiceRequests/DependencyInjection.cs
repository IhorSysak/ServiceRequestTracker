using FluentValidation;

namespace ServiceRequestTracker.Features.ServiceRequests;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceRequests(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddValidatorsFromAssemblyContaining<CreateServiceRequestRequestValidator>();

        return services;
    }
}