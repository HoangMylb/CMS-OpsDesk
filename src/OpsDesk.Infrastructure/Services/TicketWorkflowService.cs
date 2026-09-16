using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class TicketWorkflowService : ITicketWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<TicketWorkflowService> _logger;

    // State machine allowed transitions
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
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<TicketWorkflowService> logger)
    {
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
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(request.TicketId);
        if (ticket is null)
            return ServiceResult.Failure("Ticket not found.");

        var currentStatus = ticket.Status;
        var targetStatus = request.TargetStatus;

        if (!CanTransition(currentStatus, targetStatus))
        {
            return ServiceResult.Failure($"Invalid transition from '{currentStatus}' to '{targetStatus}'.");
        }

        var now = DateTime.UtcNow;

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
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
                ticket.ClosedAt = null;
            }

            ticket.Status = targetStatus;
            ticket.UpdatedAt = now;

            var history = new TicketStatusHistory
            {
                TicketId = ticket.Id,
                FromStatus = currentStatus,
                ToStatus = targetStatus,
                ChangedByUserId = currentUserId,
                ChangedAt = now,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };
            await _unitOfWork.StatusHistories.AddAsync(history);
            await _unitOfWork.SaveChangesAsync();

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
