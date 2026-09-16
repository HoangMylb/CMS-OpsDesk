using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.Company)
            .HasMaxLength(200);

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        // Unique email — enforced at the database level.
        // CustomerService also validates this in code before hitting the DB,
        // but the DB constraint is the ultimate safety net.
        builder.HasIndex(c => c.Email)
            .IsUnique();

        // Customers own many tickets. Restrict delete prevents losing ticket history
        // if someone accidentally tries to delete a customer who has open tickets.
        builder.HasMany(c => c.Tickets)
            .WithOne(t => t.Customer)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
