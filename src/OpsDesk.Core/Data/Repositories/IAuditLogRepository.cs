using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Core.Data.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(List<AuditLogItemDto> Items, int TotalCount)> GetPagedLogsAsync(
        AuditLogFilterParams filter,
        CancellationToken cancellationToken = default);
}
