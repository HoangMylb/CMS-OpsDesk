using OpsDesk.Core.Enums;

namespace OpsDesk.Core.Services;

public interface ISlaService
{
    /// <summary>
    /// Tính thời hạn xử lý (DueAt) dựa trên thời điểm tạo và mức độ ưu tiên:
    /// - Critical: 4 giờ
    /// - High: 24 giờ
    /// - Medium: 48 giờ
    /// - Low: 72 giờ
    /// </summary>
    DateTime CalculateDueAt(DateTime createdAt, TicketPriority priority);

    /// <summary>
    /// Kiểm tra ticket đã quá hạn chưa:
    /// Quá hạn khi hiện tại > DueAt và ticket chưa ở trạng thái Resolved hoặc Closed.
    /// </summary>
    bool IsOverdue(DateTime dueAt, TicketStatus status);

    /// <summary>
    /// Lấy thời lượng SLA dạng TimeSpan tương ứng với mức độ ưu tiên.
    /// </summary>
    TimeSpan GetSlaDuration(TicketPriority priority);
}
