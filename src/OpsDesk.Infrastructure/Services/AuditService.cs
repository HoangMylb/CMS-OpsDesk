using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string? userId,
        string action,
        string entityName,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues is not null
                    ? JsonSerializer.Serialize(oldValues)
                    : null,
                NewValues = newValues is not null
                    ? JsonSerializer.Serialize(newValues)
                    : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Lỗi audit log không nên làm hỏng luồng nghiệp vụ chính
            _logger.LogError(ex, "Lỗi khi ghi AuditLog: Action={Action}, Entity={Entity}/{Id}",
                action, entityName, entityId);
        }
    }

    public async Task<(List<AuditLogItemDto> Items, int TotalCount)> GetPagedLogsAsync(AuditLogFilterParams filter)
    {
        var query = _db.AuditLogs.AsNoTracking();

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

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => new AuditLogItemDto(
                l.Id,
                l.UserId,
                l.User != null ? l.User.FullName : "Hệ thống",
                l.Action,
                l.EntityName,
                l.EntityId,
                l.OldValues,
                l.NewValues,
                l.Timestamp,
                l.IpAddress
            ))
            .ToListAsync();

        return (items, total);
    }
}
