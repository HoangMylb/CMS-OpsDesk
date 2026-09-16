namespace OpsDesk.Core.Services;

public record AuditLogItemDto(
    int Id,
    string? UserId,
    string? UserName,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    DateTime Timestamp,
    string? IPAddress
);

public class AuditLogFilterParams
{
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public interface IAuditService
{
    Task LogAsync(
        string? userId,
        string action,
        string entityName,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null);

    Task<(List<AuditLogItemDto> Items, int TotalCount)> GetPagedLogsAsync(AuditLogFilterParams filter);
}
