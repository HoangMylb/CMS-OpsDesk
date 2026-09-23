using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;
using OpsDesk.Infrastructure.Services;

namespace OpsDesk.Tests.Integration;

public class TicketWorkflowIntegrationTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task TransitionAsync_ResolvingTicket_PersistsTimestampHistoryAndAuditTrail()
    {
        await using var context = CreateDbContext();
        var ticket = new Ticket
        {
            TicketCode = "TKT-2026-000001",
            CustomerId = 1,
            Subject = "Cannot sign in",
            Description = "The customer cannot sign in after a password reset.",
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress,
            CreatedByUserId = "agent-1",
            DueAt = DateTime.UtcNow.AddDays(1)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await using var unitOfWork = new UnitOfWork(context);
        var auditService = new AuditService(unitOfWork, NullLogger<AuditService>.Instance);
        var workflow = new TicketWorkflowService(unitOfWork, auditService, NullLogger<TicketWorkflowService>.Instance);

        var result = await workflow.TransitionAsync(
            new StatusTransitionRequest(ticket.Id, TicketStatus.Resolved, "Customer confirmed access."),
            "agent-1");

        Assert.True(result.Succeeded);

        var persistedTicket = await context.Tickets.SingleAsync();
        Assert.Equal(TicketStatus.Resolved, persistedTicket.Status);
        Assert.NotNull(persistedTicket.ResolvedAt);

        var history = await context.TicketStatusHistories.SingleAsync();
        Assert.Equal(TicketStatus.InProgress, history.FromStatus);
        Assert.Equal(TicketStatus.Resolved, history.ToStatus);
        Assert.Equal("Customer confirmed access.", history.Notes);

        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal("TicketStatusChanged", auditLog.Action);
        Assert.Equal(ticket.Id.ToString(), auditLog.EntityId);
    }

    [Fact]
    public async Task TransitionAsync_InvalidTransition_DoesNotWriteHistoryOrAuditTrail()
    {
        await using var context = CreateDbContext();
        var ticket = new Ticket
        {
            TicketCode = "TKT-2026-000002",
            CustomerId = 1,
            Subject = "New ticket",
            Description = "A new ticket cannot be closed directly.",
            Priority = TicketPriority.Medium,
            Status = TicketStatus.New,
            CreatedByUserId = "agent-1",
            DueAt = DateTime.UtcNow.AddDays(2)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        await using var unitOfWork = new UnitOfWork(context);
        var auditService = new AuditService(unitOfWork, NullLogger<AuditService>.Instance);
        var workflow = new TicketWorkflowService(unitOfWork, auditService, NullLogger<TicketWorkflowService>.Instance);

        var result = await workflow.TransitionAsync(
            new StatusTransitionRequest(ticket.Id, TicketStatus.Closed, null),
            "agent-1");

        Assert.False(result.Succeeded);
        Assert.Equal(TicketStatus.New, (await context.Tickets.SingleAsync()).Status);
        Assert.Empty(context.TicketStatusHistories);
        Assert.Empty(context.AuditLogs);
    }
}
