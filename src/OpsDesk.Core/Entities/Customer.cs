namespace OpsDesk.Core.Entities;

/// <summary>
/// An external contact (company or individual) on behalf of whom tickets are raised.
/// Customers never log in — this system is internal only.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Unique email — enforced at DB level and validated in CustomerService.</summary>
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Company { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Ticket> Tickets { get; set; } = [];
}
