using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TicketCode)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(t => t.Priority)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        builder.Property(t => t.DueAt).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // RowVersion: SQL Server rowversion type.
        // EF Core includes this in every UPDATE WHERE clause.
        // If another user committed a change since we loaded the row,
        // the RowVersion will differ and EF throws DbUpdateConcurrencyException.
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // === Indexes ===

        // Unique ticket code — human-readable business identifier.
        builder.HasIndex(t => t.TicketCode)
            .IsUnique();

        // Status and Priority are the most-used filter columns on the list page.
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);

        // Agent workload queries filter by AssignedToUserId.
        builder.HasIndex(t => t.AssignedToUserId);

        // Customer ticket history.
        builder.HasIndex(t => t.CustomerId);

        // Date-range filtering on list page.
        builder.HasIndex(t => t.CreatedAt);

        // Composite index for overdue query:
        //   WHERE Status NOT IN (Resolved, Closed) AND DueAt < @now
        // SQL Server can satisfy both conditions from one B-tree scan.
        builder.HasIndex(t => new { t.Status, t.DueAt })
            .HasDatabaseName("IX_Tickets_Status_DueAt");

        // === Relationships ===

        // Ticket → Customer: Restrict (cannot delete customer with tickets)
        builder.HasOne(t => t.Customer)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket → CreatedBy: Restrict (employee records are deactivated, not deleted)
        builder.HasOne(t => t.CreatedBy)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ticket → AssignedTo: SetNull (if user is somehow removed, ticket loses assignee but survives)
        // In practice employees are deactivated not deleted, so this rarely fires.
        builder.HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
