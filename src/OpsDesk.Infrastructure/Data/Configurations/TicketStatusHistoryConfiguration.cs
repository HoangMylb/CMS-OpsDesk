using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.FromStatus).IsRequired();
        builder.Property(h => h.ToStatus).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();

        builder.Property(h => h.Notes).HasMaxLength(500);

        // Index TicketId — we always query "all history for ticket X".
        builder.HasIndex(h => h.TicketId);

        builder.HasOne(h => h.Ticket)
            .WithMany(t => t.StatusHistory)
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
