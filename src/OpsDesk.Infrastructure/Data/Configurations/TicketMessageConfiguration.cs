using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(m => m.IsInternal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.TicketId);

        // Message → Ticket: Cascade — messages belong to the ticket and go with it.
        // (We don't hard-delete tickets, but if we did, orphan messages are useless.)
        builder.HasOne(m => m.Ticket)
            .WithMany(t => t.Messages)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message → Author: Restrict — we want to keep messages even if employee is deactivated.
        builder.HasOne(m => m.Author)
            .WithMany(u => u.Messages)
            .HasForeignKey(m => m.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
