using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Infrastructure.Services;
using Xunit;

namespace OpsDesk.Tests.Integration;

public class TicketAssignmentIntegrationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private UserManager<ApplicationUser> CreateTestUserManager(ApplicationDbContext context)
    {
        var userStore = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(context);
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var userValidators = new List<IUserValidator<ApplicationUser>> { new UserValidator<ApplicationUser>() };
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();

        return new UserManager<ApplicationUser>(
            userStore,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    [Fact]
    public async Task AssignTicketAsync_ToInactiveEmployee_ShouldFailWithBusinessRuleError()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var uow = new UnitOfWork(context);
        var slaService = new SlaService();
        var auditService = new AuditService(uow, NullLogger<AuditService>.Instance);
        var userManager = CreateTestUserManager(context);
        var ticketService = new TicketService(uow, slaService, auditService, userManager, NullLogger<TicketService>.Instance);

        // Seed inactive agent
        var inactiveAgent = new ApplicationUser
        {
            Id = "agent-inactive",
            UserName = "inactive@opsdesk.local",
            Email = "inactive@opsdesk.local",
            FullName = "Deactivated Agent",
            IsActive = false
        };
        context.Users.Add(inactiveAgent);

        // Seed test ticket
        var ticket = new Ticket
        {
            Id = 1,
            TicketCode = "TKT-2026-000001",
            CustomerId = 1,
            Subject = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedByUserId = "creator-1",
            DueAt = DateTime.UtcNow.AddDays(2)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Act
        var result = await ticketService.AssignTicketAsync(ticket.Id, inactiveAgent.Id, "manager-1");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("deactivated employee", result.FirstError);
    }
}
