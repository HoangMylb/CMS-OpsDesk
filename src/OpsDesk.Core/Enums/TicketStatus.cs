namespace OpsDesk.Core.Enums;

/// <summary>
/// Represents the lifecycle state of a support ticket.
/// The valid transitions between statuses are enforced by TicketWorkflowService.
/// Do NOT change the integer values — they are persisted to the database.
/// </summary>
public enum TicketStatus
{
    New = 1,
    Assigned = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    Reopened = 6
}
