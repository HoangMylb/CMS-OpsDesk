using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data.Configurations;

namespace OpsDesk.Infrastructure.Data;

/// <summary>
/// The single EF Core DbContext for the application.
///
/// Why IdentityDbContext&lt;ApplicationUser&gt;?
/// IdentityDbContext automatically configures all the ASP.NET Identity tables
/// (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.) so we don't have to.
/// It uses our ApplicationUser type so Identity tables use our extended user class.
///
/// Why apply configurations via separate IEntityTypeConfiguration classes?
/// If all Fluent API configuration lived inside OnModelCreating, this method would
/// become hundreds of lines long and hard to navigate. One config class per entity
/// keeps each entity's mapping self-contained and easy to find.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketStatusHistory> TicketStatusHistories => Set<TicketStatusHistory>();
    public DbSet<TicketAssignmentHistory> TicketAssignmentHistories => Set<TicketAssignmentHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Call base first — Identity needs to configure its own tables.
        base.OnModelCreating(builder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly.
        // This scans the Infrastructure assembly for configuration classes automatically,
        // so we don't need to register each one manually here.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
