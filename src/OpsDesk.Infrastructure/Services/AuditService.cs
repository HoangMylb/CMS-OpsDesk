using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
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

            await _unitOfWork.AuditLogs.AddAsync(log);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit log errors must not break the primary business workflow
            _logger.LogError(ex, "Error writing AuditLog: Action={Action}, Entity={Entity}/{Id}",
                action, entityName, entityId);
        }
    }

    public async Task<(List<AuditLogItemDto> Items, int TotalCount)> GetPagedLogsAsync(AuditLogFilterParams filter)
    {
        return await _unitOfWork.AuditLogs.GetPagedLogsAsync(filter);
    }
}
