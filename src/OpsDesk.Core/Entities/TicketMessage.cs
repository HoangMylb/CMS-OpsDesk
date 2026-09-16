namespace OpsDesk.Core.Entities;

/// <summary>
/// A single message or internal note on a ticket.
///
/// IsInternal = false → normal conversation entry visible to all who can read the ticket.
/// IsInternal = true  → private employee note; only shown to authorized employees.
///
/// Once created, messages are never edited or deleted — they form an immutable conversation record.
/// </summary>
public class TicketMessage
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public string AuthorUserId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// When true, this note is internal to employees only.
    /// The application layer must enforce visibility — do not rely on the UI alone.
    /// </summary>
    public bool IsInternal { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
