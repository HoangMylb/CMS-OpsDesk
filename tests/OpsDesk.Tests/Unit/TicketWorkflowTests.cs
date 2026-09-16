using OpsDesk.Core.Enums;
using OpsDesk.Infrastructure.Services;
using Xunit;

namespace OpsDesk.Tests.Unit;

public class TicketWorkflowTests
{
    private readonly TicketWorkflowService _workflowService;

    public TicketWorkflowTests()
    {
        // Testing State Machine logic without DB dependencies
        _workflowService = new TicketWorkflowService(null!, null!, null!);
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Reopened, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened, true)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress, true)]
    public void CanTransition_ValidTransitions_ShouldReturnTrue(TicketStatus from, TicketStatus to, bool expected)
    {
        var result = _workflowService.CanTransition(from, to);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.New, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.New, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress, false)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress, false)]
    public void CanTransition_InvalidTransitions_ShouldReturnFalse(TicketStatus from, TicketStatus to, bool expected)
    {
        var result = _workflowService.CanTransition(from, to);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanTransition_SameStatus_ShouldReturnFalse()
    {
        Assert.False(_workflowService.CanTransition(TicketStatus.New, TicketStatus.New));
        Assert.False(_workflowService.CanTransition(TicketStatus.InProgress, TicketStatus.InProgress));
    }
}
