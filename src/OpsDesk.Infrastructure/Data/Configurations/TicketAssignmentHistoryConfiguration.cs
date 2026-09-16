using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class TicketAssignmentHistoryConfiguration : IEntityTypeConfiguration<TicketAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<TicketAssignmentHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.NewAssigneeId).IsRequired();
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();

        builder.HasIndex(h => h.TicketId);

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.AssignmentHistory)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict on all user FKs — keep history even if users are deactivated.
        builder.HasOne(h => h.PreviousAssignee)
            .WithMany()
            .HasForeignKey(h => h.PreviousAssigneeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(h => h.NewAssignee)
            .WithMany()
            .HasForeignKey(h => h.NewAssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
