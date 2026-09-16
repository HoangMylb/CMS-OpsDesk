using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        // OldValues/NewValues are JSON strings — no fixed schema, so nvarchar(max).
        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)");

        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.Timestamp).IsRequired();

        // Composite index: find all audit entries for a specific record.
        // e.g. "show me everything that happened to Ticket #42"
        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLog_EntityName_EntityId");

        // Date-range filtering — audit log is usually browsed by date.
        builder.HasIndex(a => a.Timestamp);

        // Per-user audit trail — "what did this employee do?"
        builder.HasIndex(a => a.UserId);

        // AuditLog → User: SetNull — keep audit records even if user account is deleted.
        // (We don't delete users in practice, but this is a safety measure.)
        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
