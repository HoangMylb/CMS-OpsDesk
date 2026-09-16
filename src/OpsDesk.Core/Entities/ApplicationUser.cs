using Microsoft.AspNetCore.Identity;

namespace OpsDesk.Core.Entities;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with OpsDesk-specific fields.
/// This IS the employee record — we do not create a separate Employee table.
/// Doing so would mean maintaining two tables for one concept.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation: tickets this user created
    public ICollection<Ticket> CreatedTickets { get; set; } = [];

    // Navigation: tickets assigned to this user
    public ICollection<Ticket> AssignedTickets { get; set; } = [];

    // Navigation: messages authored by this user
    public ICollection<TicketMessage> Messages { get; set; } = [];

    // Navigation: audit log entries for actions performed by this user
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
