using OpsDesk.Core.Enums;
using OpsDesk.Infrastructure.Services;
using Xunit;

namespace OpsDesk.Tests.Unit;

public class SlaServiceTests
{
    private readonly SlaService _slaService = new();

    [Theory]
    [InlineData(TicketPriority.Critical, 4)]
    [InlineData(TicketPriority.High, 24)]
    [InlineData(TicketPriority.Medium, 48)]
    [InlineData(TicketPriority.Low, 72)]
    public void CalculateDueAt_ShouldAddCorrectHoursBasedOnPriority(TicketPriority priority, int expectedHours)
    {
        // Arrange
        var createdAt = new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var dueAt = _slaService.CalculateDueAt(createdAt, priority);

        // Assert
        Assert.Equal(createdAt.AddHours(expectedHours), dueAt);
    }

    [Fact]
    public void IsOverdue_WhenPastDueAtAndOpen_ShouldReturnTrue()
    {
        // Arrange
        var pastDueAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.True(_slaService.IsOverdue(pastDueAt, TicketStatus.New));
        Assert.True(_slaService.IsOverdue(pastDueAt, TicketStatus.Assigned));
        Assert.True(_slaService.IsOverdue(pastDueAt, TicketStatus.InProgress));
        Assert.True(_slaService.IsOverdue(pastDueAt, TicketStatus.Reopened));
    }

    [Fact]
    public void IsOverdue_WhenPastDueAtButResolvedOrClosed_ShouldReturnFalse()
    {
        // Arrange
        var pastDueAt = DateTime.UtcNow.AddHours(-1);

        // Act & Assert
        Assert.False(_slaService.IsOverdue(pastDueAt, TicketStatus.Resolved));
        Assert.False(_slaService.IsOverdue(pastDueAt, TicketStatus.Closed));
    }

    [Fact]
    public void IsOverdue_WhenNotPastDueAt_ShouldReturnFalse()
    {
        // Arrange
        var futureDueAt = DateTime.UtcNow.AddHours(5);

        // Act & Assert
        Assert.False(_slaService.IsOverdue(futureDueAt, TicketStatus.New));
    }
}
