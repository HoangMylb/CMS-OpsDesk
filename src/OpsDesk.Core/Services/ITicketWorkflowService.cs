using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public record StatusTransitionRequest(
    int TicketId,
    TicketStatus TargetStatus,
    string? Notes
);

public interface ITicketWorkflowService
{
    /// <summary>
    /// Kiểm tra xem bước chuyển trạng thái có hợp lệ theo bảng quy tắc máy trạng thái không.
    /// </summary>
    bool CanTransition(TicketStatus currentStatus, TicketStatus targetStatus);

    /// <summary>
    /// Lấy danh sách các trạng thái mục tiêu hợp lệ mà ticket hiện tại có thể chuyển tới.
    /// </summary>
    List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus);

    /// <summary>
    /// Thực hiện chuyển trạng thái ticket, cập nhật mốc thời gian ResolvedAt/ClosedAt,
    /// ghi nhận TicketStatusHistory và ghi AuditLog bằng Unit of Work transaction cực ngắn.
    /// </summary>
    Task<ServiceResult> TransitionAsync(StatusTransitionRequest request, string currentUserId);
}
