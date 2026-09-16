using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(
        ApplicationDbContext db,
        ICacheService cache,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cache = cache;
        _userManager = userManager;
    }

    public async Task<DashboardViewModelData> GetDashboardDataAsync(string currentUserId, bool isSystemWide)
    {
        var cacheKey = $"dashboard_{currentUserId}_{(isSystemWide ? "sys" : "personal")}";

        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var now = DateTime.UtcNow;
            var todayUtc = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            var query = _db.Tickets.AsNoTracking();

            if (!isSystemWide)
            {
                query = query.Where(t => t.AssignedToUserId == currentUserId);
            }

            // 1. KPI Counts
            var totalTickets = await query.CountAsync();
            var openTickets = await query.CountAsync(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
            var inProgressTickets = await query.CountAsync(t => t.Status == TicketStatus.InProgress);
            var overdueTickets = await query.CountAsync(t => t.DueAt < now && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
            var resolvedTodayCount = await query.CountAsync(t => t.ResolvedAt >= todayUtc);

            var kpi = new DashboardKpiSummary(
                totalTickets,
                openTickets,
                inProgressTickets,
                overdueTickets,
                resolvedTodayCount
            );

            // 2. Thống kê theo Mức độ ưu tiên
            var prioCounts = await query
                .Where(t => t.Status != TicketStatus.Closed)
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToListAsync();

            var priorityDistribution = Enum.GetValues<TicketPriority>()
                .Select(p => new PriorityDistributionItem(
                    p,
                    GetPriorityDisplayName(p),
                    prioCounts.FirstOrDefault(x => x.Priority == p)?.Count ?? 0
                ))
                .ToList();

            // 3. Thống kê theo Trạng thái
            var statusCounts = await query
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusDistribution = Enum.GetValues<TicketStatus>()
                .Select(s => new StatusDistributionItem(
                    s,
                    GetStatusDisplayName(s),
                    statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0
                ))
                .ToList();

            // 4. Phân bổ công việc giữa các Agent (chỉ tính cho Admin/Manager xem toàn hệ thống)
            var agentWorkloads = new List<AgentWorkloadItem>();
            if (isSystemWide)
            {
                var activeAgents = await _userManager.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive)
                    .Select(u => new { u.Id, u.FullName })
                    .ToListAsync();

                var agentTicketStats = await _db.Tickets
                    .AsNoTracking()
                    .Where(t => t.AssignedToUserId != null && t.Status != TicketStatus.Closed)
                    .GroupBy(t => t.AssignedToUserId!)
                    .Select(g => new
                    {
                        AgentId = g.Key,
                        AssignedCount = g.Count(),
                        InProgressCount = g.Count(t => t.Status == TicketStatus.InProgress),
                        OverdueCount = g.Count(t => t.DueAt < now && t.Status != TicketStatus.Resolved)
                    })
                    .ToListAsync();

                agentWorkloads = activeAgents.Select(a =>
                {
                    var stat = agentTicketStats.FirstOrDefault(s => s.AgentId == a.Id);
                    return new AgentWorkloadItem(
                        a.Id,
                        a.FullName,
                        stat?.AssignedCount ?? 0,
                        stat?.InProgressCount ?? 0,
                        stat?.OverdueCount ?? 0
                    );
                })
                .OrderByDescending(w => w.AssignedCount)
                .ToList();
            }

            // 5. Danh sách Top 5 Ticket quá hạn cần ưu tiên giải quyết
            var topOverdue = await query
                .Where(t => t.DueAt < now && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
                .OrderBy(t => t.DueAt)
                .Take(5)
                .Select(t => new TicketListItem(
                    t.Id,
                    t.TicketCode,
                    t.Subject,
                    t.CustomerId,
                    t.Customer.Name,
                    t.Priority,
                    t.Status,
                    t.AssignedToUserId,
                    t.AssignedTo != null ? t.AssignedTo.FullName : null,
                    t.CreatedAt,
                    t.DueAt,
                    true
                ))
                .ToListAsync();

            return new DashboardViewModelData(
                kpi,
                priorityDistribution,
                statusDistribution,
                agentWorkloads,
                topOverdue,
                isSystemWide
            );
        }, TimeSpan.FromMinutes(2)); // Cache trong 2 phút
    }

    private static string GetPriorityDisplayName(TicketPriority p) => p switch
    {
        TicketPriority.Critical => "Khẩn cấp (4h)",
        TicketPriority.High => "Cao (24h)",
        TicketPriority.Medium => "Trung bình (48h)",
        TicketPriority.Low => "Thấp (72h)",
        _ => p.ToString()
    };

    private static string GetStatusDisplayName(TicketStatus s) => s switch
    {
        TicketStatus.New => "Mới",
        TicketStatus.Assigned => "Đã phân công",
        TicketStatus.InProgress => "Đang xử lý",
        TicketStatus.Resolved => "Đã giải quyết",
        TicketStatus.Closed => "Đã đóng",
        TicketStatus.Reopened => "Mở lại",
        _ => s.ToString()
    };
}
