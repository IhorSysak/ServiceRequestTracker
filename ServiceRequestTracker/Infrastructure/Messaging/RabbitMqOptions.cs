using System.ComponentModel.DataAnnotations;

namespace ServiceRequestTracker.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 5672;

    [Required]
    public string User { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string VirtualHost { get; init; } = "/";

    [Required]
    public string Exchange { get; init; } = string.Empty;

    [Required]
    public string Queue { get; init; } = string.Empty;

    [Required]
    public string DeadLetterExchange { get; init; } = string.Empty;

    [Required]
    public string DeadLetterQueue { get; init; } = string.Empty;

    [Required]
    public string RoutingKey { get; init; } = string.Empty;
}