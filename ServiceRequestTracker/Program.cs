using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ServiceRequestTracker.Features.ServiceRequests;
using ServiceRequestTracker.Infrastructure.Http;
using ServiceRequestTracker.Infrastructure.Messaging;
using ServiceRequestTracker.Infrastructure.Outbox;
using ServiceRequestTracker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

services.AddOpenApi();
services.AddProblemDetails();
services.AddExceptionHandler<GlobalExceptionHandler>();

var connectionString = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Postgres' is not configured.");

services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
services.AddHostedService<DatabaseMigrator>();

services.AddSingleton(TimeProvider.System);
services.AddScoped<IServiceRequestService, ServiceRequestService>();
services.AddValidatorsFromAssemblyContaining<CreateServiceRequestRequestValidator>();

services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddSingleton<RabbitMqConnection>();
services.AddHostedService<RabbitMqInitializer>();

services.AddOptions<OutboxOptions>()
    .BindConfiguration(OutboxOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddHostedService<OutboxPublisherService>();

services.AddScoped<IProcessedEventStore, ProcessedEventStore>();
services.AddScoped<ServiceRequestEventHandler>();
services.AddHostedService<EventConsumerService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }