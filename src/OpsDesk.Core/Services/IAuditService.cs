namespace OpsDesk.Core.Services;

/// <summary>
/// Ghi lại các thao tác quan trọng vào AuditLog.
/// Mọi service cần audit đều phụ thuộc vào interface này,
/// không phụ thuộc trực tiếp vào DbContext.
/// </summary>
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
}
