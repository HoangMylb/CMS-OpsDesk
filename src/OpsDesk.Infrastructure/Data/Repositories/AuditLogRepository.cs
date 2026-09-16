using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Data.Repositories;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Data.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(List<AuditLogItemDto> Items, int TotalCount)> GetPagedLogsAsync(
        AuditLogFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(l => l.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(l => l.Action.Contains(filter.Action.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(l => l.EntityName == filter.EntityName.Trim());
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= filter.ToDate.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new AuditLogItemDto(
                l.Id,
                l.UserId,
                l.User != null ? l.User.FullName : "System",
                l.Action,
                l.EntityName,
                l.EntityId,
                l.OldValues,
                l.NewValues,
                l.Timestamp,
                l.IpAddress
            ))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
