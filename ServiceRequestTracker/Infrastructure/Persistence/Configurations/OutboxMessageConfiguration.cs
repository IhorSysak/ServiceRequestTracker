using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequestTracker.Infrastructure.Outbox;

namespace ServiceRequestTracker.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Type).HasColumnName("type").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamp with time zone");
        builder.Property(x => x.Attempts).HasColumnName("attempts");
        builder.Property(x => x.LastError).HasColumnName("last_error");
        builder.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.NextAttemptAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}
