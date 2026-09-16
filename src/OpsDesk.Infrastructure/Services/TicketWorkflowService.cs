using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class TicketWorkflowService : ITicketWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<TicketWorkflowService> _logger;

    // Bảng định nghĩa chuyển đổi trạng thái hợp lệ
    private static readonly Dictionary<TicketStatus, List<TicketStatus>> AllowedTransitions = new()
    {
        { TicketStatus.New, [TicketStatus.Assigned] },
        { TicketStatus.Assigned, [TicketStatus.InProgress] },
        { TicketStatus.InProgress, [TicketStatus.Resolved] },
        { TicketStatus.Resolved, [TicketStatus.Closed, TicketStatus.Reopened] },
        { TicketStatus.Closed, [TicketStatus.Reopened] },
        { TicketStatus.Reopened, [TicketStatus.InProgress] }
    };

    public TicketWorkflowService(
        ApplicationDbContext db,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<TicketWorkflowService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public bool CanTransition(TicketStatus currentStatus, TicketStatus targetStatus)
    {
        if (currentStatus == targetStatus) return false;
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(targetStatus);
    }

    public List<TicketStatus> GetAllowedTransitions(TicketStatus currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var list) ? list : [];
    }

    public async Task<ServiceResult> TransitionAsync(StatusTransitionRequest request, string currentUserId)
    {
        var ticket = await _db.Tickets.FindAsync(request.TicketId);
        if (ticket is null)
            return ServiceResult.Failure("Không tìm thấy ticket.");

        var currentStatus = ticket.Status;
        var targetStatus = request.TargetStatus;

        if (!CanTransition(currentStatus, targetStatus))
        {
            return ServiceResult.Failure($"Không thể chuyển trạng thái từ '{currentStatus}' sang '{targetStatus}'. Bước nhảy này không hợp lệ.");
        }

        var now = DateTime.UtcNow;

        // Thực thi transaction cực ngắn qua Unit of Work
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            // 1. Cập nhật các mốc thời gian đặc biệt
            if (targetStatus == TicketStatus.Resolved)
            {
                ticket.ResolvedAt = now;
            }
            else if (targetStatus == TicketStatus.Closed)
            {
                ticket.ClosedAt = now;
            }
            else if (targetStatus == TicketStatus.Reopened)
            {
                ticket.ClosedAt = null; // Mở lại thì hủy mốc đóng
            }

            ticket.Status = targetStatus;
            ticket.UpdatedAt = now;

            // 2. Thêm bản ghi bất biến vào TicketStatusHistory
            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = currentStatus,
                ToStatus = targetStatus,
                ChangedByUserId = currentUserId,
                ChangedAt = now,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };
            _db.TicketStatusHistories.Add(history);

            // 3. Ghi nhận Audit Log
            await _auditService.LogAsync(
                currentUserId,
                "TicketStatusChanged",
                "Ticket",
                ticket.Id.ToString(),
                oldValues: new { Status = currentStatus },
                newValues: new { Status = targetStatus, Notes = request.Notes });
        });

        _logger.LogInformation("Ticket {TicketCode} transitioned from {From} to {To} by {UserId}",
            ticket.TicketCode, currentStatus, targetStatus, currentUserId);

        return ServiceResult.Success();
    }
}
