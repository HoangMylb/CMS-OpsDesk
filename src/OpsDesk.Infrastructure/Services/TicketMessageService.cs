using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Services;

public class TicketMessageService : ITicketMessageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<TicketMessageService> _logger;

    public TicketMessageService(
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<TicketMessageService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<TicketMessageDto>> GetMessagesAsync(int ticketId, string currentUserId, bool canViewInternal)
    {
        var query = _unitOfWork.Messages.Query(asNoTracking: true)
            .Where(m => m.TicketId == ticketId);

        // Security rule: hide internal notes if unauthorized (BR-10)
        if (!canViewInternal)
        {
            query = query.Where(m => !m.IsInternal);
        }

        return await query
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TicketMessageDto(
                m.Id,
                m.AuthorUserId,
                m.Author.FullName,
                m.Author.Email ?? string.Empty,
                m.Content,
                m.IsInternal,
                m.CreatedAt,
                m.AuthorUserId == currentUserId
            ))
            .ToListAsync();
    }

    public async Task<ServiceResult<int>> AddMessageAsync(AddMessageRequest request, string authorUserId)
    {
        // Reject empty messages (BR-09, UC-TKT-18)
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ServiceResult<int>.Failure("Message content cannot be empty or whitespace only.");
        }

        var ticketExists = await _unitOfWork.Tickets.AnyAsync(t => t.Id == request.TicketId);
        if (!ticketExists)
        {
            return ServiceResult<int>.Failure("Ticket not found.");
        }

        var message = new TicketMessage
        {
            TicketId = request.TicketId,
            AuthorUserId = authorUserId,
            Content = request.Content.Trim(),
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            await _unitOfWork.Messages.AddAsync(message);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync(
                authorUserId,
                request.IsInternal ? "InternalNoteAdded" : "TicketMessageAdded",
                "TicketMessage",
                message.Id.ToString(),
                newValues: new { message.TicketId, message.IsInternal });
        });

        _logger.LogInformation("Message/Note {MessageId} added to Ticket {TicketId} by user {UserId}",
            message.Id, request.TicketId, authorUserId);

        return ServiceResult<int>.Success(message.Id);
    }
}
