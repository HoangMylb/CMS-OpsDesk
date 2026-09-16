namespace OpsDesk.Core.Entities;

/// <summary>
/// System-wide audit trail. Records important operations so we can answer:
///   "Who changed what, when, and on which record?"
///
/// Design decisions:
/// - OldValues / NewValues stored as JSON strings for flexibility without extra tables.
/// - Never edited or deleted from UI. The table has no soft-delete.
/// - Passwords and secrets must NEVER appear in OldValues / NewValues.
/// - EntityName + EntityId together identify what was affected.
///   e.g. EntityName="Ticket", EntityId="42"
/// </summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>The employee who performed the action. Nullable in case of system actions.</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Human-readable action name, e.g. "TicketAssigned", "EmployeeDeactivated".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>The domain entity type affected, e.g. "Ticket", "Customer", "ApplicationUser".</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>The primary key of the affected record as a string.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot of key field values before the change. May be null for create operations.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of key field values after the change. May be null for delete operations.</summary>
    public string? NewValues { get; set; }

    /// <summary>IP address of the request, for security investigations.</summary>
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
