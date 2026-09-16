using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Infrastructure.Data;
using Xunit;

namespace OpsDesk.Tests.Integration;

public class TicketConcurrencyTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Ticket_WhenConcurrentlyUpdated_OriginalRowVersionMismatchThrowsException()
    {
        // Arrange
        var context1 = CreateInMemoryDbContext();
        var ticket = new Ticket
        {
            Id = 100,
            TicketCode = "TKT-2026-000100",
            CustomerId = 1,
            Subject = "Original Subject",
            Description = "Original Description",
            Priority = TicketPriority.Low,
            Status = TicketStatus.New,
            CreatedByUserId = "user-1",
            DueAt = DateTime.UtcNow.AddDays(3),
            RowVersion = [1, 2, 3, 4]
        };
        context1.Tickets.Add(ticket);
        await context1.SaveChangesAsync();

        // Simulate stale row version from another client session
        var staleRowVersion = new byte[] { 9, 9, 9, 9 };

        // Act & Assert
        // Setting an explicit original value that mismatches triggers concurrency conflict
        context1.Entry(ticket).Property(t => t.RowVersion).OriginalValue = staleRowVersion;
        ticket.Subject = "Attempted Update with stale RowVersion";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await context1.SaveChangesAsync();
        });
    }
}
