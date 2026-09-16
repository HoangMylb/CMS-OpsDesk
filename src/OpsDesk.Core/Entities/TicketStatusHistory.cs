using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Entities;

/// <summary>
/// Immutable record of every status transition on a ticket.
/// Created automatically by TicketWorkflowService whenever a status changes.
/// Never edited or deleted from the UI — this is historical fact.
/// </summary>
public class TicketStatusHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string ChangedByUserId { get; set; } = string.Empty;
    public ApplicationUser ChangedBy { get; set; } = null!;

    public TicketStatus FromStatus { get; set; }

    public TicketStatus ToStatus { get; set; }

    /// <summary>Optional note explaining why the transition occurred (e.g. "Reopened — issue recurred").</summary>
    public string? Notes { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
