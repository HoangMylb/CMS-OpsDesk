using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Seeding;

/// <summary>
/// Safe and idempotent seed data for Development and Demo environments.
/// Principles:
/// - Fully idempotent: running multiple times does not create duplicates.
/// - Does not use migrations for business entities — migrations keep schema only.
/// - 100% English code, comments, and logging.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedDepartmentsAsync();
        await SeedRolesAndPermissionsAsync();
        await SeedUsersAsync();
        await SeedCustomersAsync();
        await SeedTicketsAndMessagesAsync();
    }

    private async Task SeedDepartmentsAsync()
    {
        var departments = new[]
        {
            new Department { Name = "Executive & Management", Description = "Senior leadership and operations oversight" },
            new Department { Name = "Customer Support", Description = "Frontline support and client relations" },
            new Department { Name = "Technical Operations", Description = "Infrastructure and IT services" },
            new Department { Name = "Billing & Finance", Description = "Invoicing and billing inquiries" },
        };

        foreach (var dept in departments)
        {
            if (!await _db.Departments.AnyAsync(d => d.Name == dept.Name))
            {
                _db.Departments.Add(dept);
                _logger.LogInformation("Seeded department: {Name}", dept.Name);
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task SeedRolesAndPermissionsAsync()
    {
        var roleDefinitions = new Dictionary<string, IEnumerable<string>>
        {
            ["Admin"] = Permissions.GetAll(),
            ["Manager"] =
            [
                Permissions.Dashboard.View,
                Permissions.Employee.View,
                Permissions.Customer.View,
                Permissions.Customer.Create,
                Permissions.Customer.Update,
                Permissions.Ticket.ViewAll,
                Permissions.Ticket.ViewAssigned,
                Permissions.Ticket.Create,
                Permissions.Ticket.Assign,
                Permissions.Ticket.Update,
                Permissions.Ticket.Resolve,
                Permissions.Ticket.Close,
                Permissions.Ticket.Reopen,
            ],
            ["SupportAgent"] =
            [
                Permissions.Dashboard.View,
                Permissions.Customer.View,
                Permissions.Ticket.ViewAssigned,
                Permissions.Ticket.Create,
                Permissions.Ticket.Update,
                Permissions.Ticket.Resolve,
            ],
        };

        foreach (var (roleName, permissions) in roleDefinitions)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new IdentityRole(roleName);
                await _roleManager.CreateAsync(role);
                _logger.LogInformation("Created default role: {RoleName}", roleName);
            }

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var existingPermValues = existingClaims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var perm in permissions)
            {
                if (!existingPermValues.Contains(perm))
                {
                    await _roleManager.AddClaimAsync(role, new Claim("Permission", perm));
                }
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        var supportDept = await _db.Departments.FirstOrDefaultAsync(d => d.Name == "Customer Support");
        var managementDept = await _db.Departments.FirstOrDefaultAsync(d => d.Name == "Executive & Management");

        var users = new[]
        {
            new
            {
                FullName = "System Administrator",
                Email = "admin@opsdesk.local",
                Password = "Admin@123456",
                Role = "Admin",
                Department = managementDept,
            },
            new
            {
                FullName = "Sarah Jenkins",
                Email = "manager@opsdesk.local",
                Password = "Manager@123456",
                Role = "Manager",
                Department = managementDept,
            },
            new
            {
                FullName = "Alex Rivera",
                Email = "agent1@opsdesk.local",
                Password = "Agent@123456",
                Role = "SupportAgent",
                Department = supportDept,
            },
            new
            {
                FullName = "David Chen",
                Email = "agent2@opsdesk.local",
                Password = "Agent@123456",
                Role = "SupportAgent",
                Department = supportDept,
            },
        };

        foreach (var userData in users)
        {
            if (await _userManager.FindByEmailAsync(userData.Email) != null)
                continue;

            var user = new ApplicationUser
            {
                FullName = userData.FullName,
                UserName = userData.Email,
                Email = userData.Email,
                EmailConfirmed = true,
                IsActive = true,
                DepartmentId = userData.Department?.Id,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, userData.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, userData.Role);
                _logger.LogInformation("Seeded user: {Email} with role {Role}", userData.Email, userData.Role);
            }
        }
    }

    private async Task SeedCustomersAsync()
    {
        if (await _db.Customers.AnyAsync()) return;

        var customers = new[]
        {
            new Customer
            {
                Name = "Acme Technologies",
                Email = "contact@acmetech.io",
                Phone = "+1-555-0199",
                Company = "Acme Corporation",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Customer
            {
                Name = "Global Logistics Ltd",
                Email = "ops@globallogistics.com",
                Phone = "+1-555-0245",
                Company = "Global Logistics LLC",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Customer
            {
                Name = "Horizon Healthcare",
                Email = "helpdesk@horizonhealth.org",
                Phone = "+1-555-0371",
                Company = "Horizon Health Partners",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Customer
            {
                Name = "Apex Financial Group",
                Email = "support@apexfinance.com",
                Phone = "+1-555-0482",
                Company = "Apex Capital",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        _db.Customers.AddRange(customers);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} demo customers.", customers.Length);
    }

    private async Task SeedTicketsAndMessagesAsync()
    {
        if (await _db.Tickets.AnyAsync()) return;

        var admin = await _userManager.FindByEmailAsync("admin@opsdesk.local");
        var manager = await _userManager.FindByEmailAsync("manager@opsdesk.local");
        var agent1 = await _userManager.FindByEmailAsync("agent1@opsdesk.local");
        var agent2 = await _userManager.FindByEmailAsync("agent2@opsdesk.local");

        var customers = await _db.Customers.ToListAsync();
        if (customers.Count == 0 || admin == null || manager == null || agent1 == null) return;

        var now = DateTime.UtcNow;

        // Ticket 1: Overdue Critical Ticket
        var ticket1 = new Ticket
        {
            TicketCode = $"TKT-{now.Year}-000001",
            CustomerId = customers[0].Id,
            Subject = "Production Database API Outage",
            Description = "The customer reported that their checkout API is failing with HTTP 500 error codes continuously.",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.InProgress,
            AssignedToUserId = agent1.Id,
            CreatedByUserId = manager.Id,
            CreatedAt = now.AddHours(-10),
            DueAt = now.AddHours(-6), // Overdue by 6 hours
            UpdatedAt = now.AddHours(-2)
        };
        ticket1.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = TicketStatus.New,
            ToStatus = TicketStatus.Assigned,
            ChangedByUserId = manager.Id,
            ChangedAt = now.AddHours(-9),
            Notes = "Assigned to Alex for immediate escalation."
        });
        ticket1.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = TicketStatus.Assigned,
            ToStatus = TicketStatus.InProgress,
            ChangedByUserId = agent1.Id,
            ChangedAt = now.AddHours(-8),
            Notes = "Investigating database connection pool metrics."
        });
        ticket1.Messages.Add(new TicketMessage
        {
            AuthorUserId = agent1.Id,
            Content = "Connection pool exhausted due to unclosed query handlers in v2.4 deployment. Preparing hotfix.",
            IsInternal = true,
            CreatedAt = now.AddHours(-7)
        });
        ticket1.Messages.Add(new TicketMessage
        {
            AuthorUserId = agent1.Id,
            Content = "Hello, our engineering team has isolated the cause to high connection pooling load. We are deploying a patch.",
            IsInternal = false,
            CreatedAt = now.AddHours(-6)
        });

        // Ticket 2: Resolved High Priority Ticket
        var ticket2 = new Ticket
        {
            TicketCode = $"TKT-{now.Year}-000002",
            CustomerId = customers[1].Id,
            Subject = "SSO Authentication Timeout on Mobile App",
            Description = "Users attempting to authenticate via Okta are experiencing 30-second timeouts.",
            Priority = TicketPriority.High,
            Status = TicketStatus.Resolved,
            AssignedToUserId = agent1.Id,
            CreatedByUserId = admin.Id,
            CreatedAt = now.AddDays(-2),
            DueAt = now.AddDays(-1),
            ResolvedAt = now.AddHours(-3),
            UpdatedAt = now.AddHours(-3)
        };
        ticket2.StatusHistory.Add(new TicketStatusHistory
        {
            FromStatus = TicketStatus.InProgress,
            ToStatus = TicketStatus.Resolved,
            ChangedByUserId = agent1.Id,
            ChangedAt = now.AddHours(-3),
            Notes = "Updated callback redirect URIs and renewed IdP signing certificates."
        });

        // Ticket 3: New Medium Ticket (Awaiting assignment)
        var ticket3 = new Ticket
        {
            TicketCode = $"TKT-{now.Year}-000003",
            CustomerId = customers[2].Id,
            Subject = "Request for Monthly Billing Statement Breakdown",
            Description = "Customer requires an itemized export of active license usage for the month of August.",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedByUserId = admin.Id,
            CreatedAt = now.AddHours(-5),
            DueAt = now.AddHours(43),
            UpdatedAt = now.AddHours(-5)
        };

        // Ticket 4: Closed Ticket
        var ticket4 = new Ticket
        {
            TicketCode = $"TKT-{now.Year}-000004",
            CustomerId = customers[3].Id,
            Subject = "Password reset link not received",
            Description = "Customer was unable to receive password reset notification in their spam filter.",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Closed,
            AssignedToUserId = agent2?.Id ?? agent1.Id,
            CreatedByUserId = manager.Id,
            CreatedAt = now.AddDays(-5),
            DueAt = now.AddDays(-2),
            ResolvedAt = now.AddDays(-3),
            ClosedAt = now.AddDays(-2),
            UpdatedAt = now.AddDays(-2)
        };

        _db.Tickets.AddRange(ticket1, ticket2, ticket3, ticket4);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded demo tickets, status transitions, and internal notes.");
    }
}
