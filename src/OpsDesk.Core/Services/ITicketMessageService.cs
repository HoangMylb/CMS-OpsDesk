namespace OpsDesk.Core.Services;

public record TicketMessageDto(
    int Id,
    string AuthorId,
    string AuthorName,
    string AuthorEmail,
    string Content,
    bool IsInternal,
    DateTime CreatedAt,
    bool IsCurrentUserAuthor
);

public record AddMessageRequest(
    int TicketId,
    string Content,
    bool IsInternal
);

public interface ITicketMessageService
{
    /// <summary>
    /// Retrieves ticket message list using AsNoTracking and DTO projection.
    /// Automatically filters out internal notes (IsInternal = true) if user lacks permission.
    /// </summary>
    Task<List<TicketMessageDto>> GetMessagesAsync(int ticketId, string currentUserId, bool canViewInternal);

    /// <summary>
    /// Adds a new ticket reply or internal note. Rejects blank messages (BR-09).
    /// </summary>
    Task<ServiceResult<int>> AddMessageAsync(AddMessageRequest request, string authorUserId);
}
