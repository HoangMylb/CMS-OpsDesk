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
    /// Lấy danh sách tin nhắn của ticket sử dụng AsNoTracking và DTO projection.
    /// Tự động lọc bỏ các ghi chú nội bộ (IsInternal = true) nếu người dùng không có quyền xem.
    /// </summary>
    Task<List<TicketMessageDto>> GetMessagesAsync(int ticketId, string currentUserId, bool canViewInternal);

    /// <summary>
    /// Thêm tin nhắn hoặc ghi chú nội bộ mới. Chặn tin nhắn rỗng (BR-09).
    /// </summary>
    Task<ServiceResult<int>> AddMessageAsync(AddMessageRequest request, string authorUserId);
}
