using System.Text.Json;
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
}
