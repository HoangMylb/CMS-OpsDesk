using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsDesk.Core.Entities;

namespace OpsDesk.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // IdentityUser already configures Id, Email, UserName, PasswordHash etc.
        // We only need to configure OpsDesk-specific fields here.

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // Index IsActive to efficiently filter out deactivated users.
        // Queries like "find all active agents to assign" use this frequently.
        builder.HasIndex(u => u.IsActive);

        // Index DepartmentId — used when filtering employees by department.
        builder.HasIndex(u => u.DepartmentId);
    }
}
