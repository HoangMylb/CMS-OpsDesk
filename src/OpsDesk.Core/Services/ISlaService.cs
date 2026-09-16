using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public interface ISlaService
{
    /// <summary>
    /// Calculates resolution deadline (DueAt) based on creation time and ticket priority:
    /// - Critical: 4 hours
    /// - High: 24 hours
    /// - Medium: 48 hours
    /// - Low: 72 hours
    /// </summary>
    DateTime CalculateDueAt(DateTime createdAt, TicketPriority priority);

    /// <summary>
    /// Checks whether a ticket is overdue:
    /// Overdue when Current Time > DueAt and status is not Resolved or Closed.
    /// </summary>
    bool IsOverdue(DateTime dueAt, TicketStatus status);

    /// <summary>
    /// Gets SLA duration as TimeSpan corresponding to priority.
    /// </summary>
    TimeSpan GetSlaDuration(TicketPriority priority);
}
