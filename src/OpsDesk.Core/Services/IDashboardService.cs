using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public record DashboardKpiSummary(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int OverdueTickets,
    int ResolvedTodayCount
);

public record PriorityDistributionItem(
    TicketPriority Priority,
    string PriorityName,
    int Count
);

public record StatusDistributionItem(
    TicketStatus Status,
    string StatusName,
    int Count
);

public record AgentWorkloadItem(
    string AgentId,
    string AgentName,
    int AssignedCount,
    int InProgressCount,
    int OverdueCount
);

public record DashboardViewModelData(
    DashboardKpiSummary Kpi,
    List<PriorityDistributionItem> PriorityDistribution,
    List<StatusDistributionItem> StatusDistribution,
    List<AgentWorkloadItem> AgentWorkloads,
    List<TicketListItem> TopOverdueTickets,
    bool IsAdminOrManager
);

public interface IDashboardService
{
    /// <summary>
    /// Retrieves aggregated dashboard metrics optimized with AsNoTracking, LINQ GroupBy, and memory caching.
    /// If user is an agent (with ViewAssigned permission only), restricts metrics to that agent's tickets.
    /// </summary>
    Task<DashboardViewModelData> GetDashboardDataAsync(string currentUserId, bool isSystemWide);
}
