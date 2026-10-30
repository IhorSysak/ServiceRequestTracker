using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceRequestTracker.Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");

        builder.HasKey(x => x.MessageId);
        builder.Property(x => x.MessageId).HasColumnName("message_id").ValueGeneratedNever();

        builder.Property(x => x.RequestId).HasColumnName("request_id");
        builder.Property(x => x.OldStatus).HasColumnName("old_status").HasMaxLength(20);
        builder.Property(x => x.NewStatus).HasColumnName("new_status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Priority).HasColumnName("priority").HasMaxLength(20).IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ReceivedAt).HasColumnName("received_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.RawPayload).HasColumnName("raw_payload").HasColumnType("jsonb").IsRequired();
    }
}