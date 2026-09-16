using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsDesk.Core.Data;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;
using OpsDesk.Infrastructure.Data;

namespace OpsDesk.Infrastructure.Services;

public class TicketMessageService : ITicketMessageService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ILogger<TicketMessageService> _logger;

    public TicketMessageService(
        ApplicationDbContext db,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ILogger<TicketMessageService> logger)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<TicketMessageDto>> GetMessagesAsync(int ticketId, string currentUserId, bool canViewInternal)
    {
        var query = _db.TicketMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId);

        // Bảo mật: Nếu người dùng không có quyền xem ghi chú nội bộ, lọc bỏ ở tầng Database (BR-10)
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
        // Kiểm tra nội dung rỗng (BR-09, UC-TKT-18)
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ServiceResult<int>.Failure("Nội dung tin nhắn hoặc ghi chú không được để trống.");
        }

        var ticketExists = await _db.Tickets.AnyAsync(t => t.Id == request.TicketId);
        if (!ticketExists)
        {
            return ServiceResult<int>.Failure("Không tìm thấy ticket.");
        }

        var message = new TicketMessage
        {
            TicketId = request.TicketId,
            AuthorUserId = authorUserId,
            Content = request.Content.Trim(),
            IsInternal = request.IsInternal,
            CreatedAt = DateTime.UtcNow
        };

        // Thực thi transaction cực ngắn qua Unit of Work
        await _unitOfWork.ExecuteTransactionAsync(async () =>
        {
            _db.TicketMessages.Add(message);
            await _db.SaveChangesAsync();

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
