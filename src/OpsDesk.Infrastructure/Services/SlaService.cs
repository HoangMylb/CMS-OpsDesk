using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class SlaService : ISlaService
{
    public DateTime CalculateDueAt(DateTime createdAt, TicketPriority priority)
    {
        var duration = GetSlaDuration(priority);
        return createdAt.Add(duration);
    }

    public bool IsOverdue(DateTime dueAt, TicketStatus status)
    {
        if (status is TicketStatus.Resolved or TicketStatus.Closed)
            return false;

        return DateTime.UtcNow > dueAt;
    }

    public TimeSpan GetSlaDuration(TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => TimeSpan.FromHours(4),
        TicketPriority.High     => TimeSpan.FromHours(24),
        TicketPriority.Medium   => TimeSpan.FromHours(48),
        TicketPriority.Low      => TimeSpan.FromHours(72),
        _                       => TimeSpan.FromHours(48)
    };
}
