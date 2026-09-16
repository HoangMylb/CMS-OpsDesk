namespace OpsDesk.Core.Entities;

/// <summary>
/// Immutable record of every assignment or reassignment on a ticket.
/// Created automatically by TicketWorkflowService whenever AssignedToUserId changes.
/// PreviousAssigneeId is null when a ticket is assigned for the first time.
/// </summary>
public class TicketAssignmentHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    /// <summary>Null when the ticket was unassigned before this change.</summary>
    public string? PreviousAssigneeId { get; set; }
    public ApplicationUser? PreviousAssignee { get; set; }

    public string NewAssigneeId { get; set; } = string.Empty;
    public ApplicationUser NewAssignee { get; set; } = null!;

    public string ChangedByUserId { get; set; } = string.Empty;
    public ApplicationUser ChangedBy { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
