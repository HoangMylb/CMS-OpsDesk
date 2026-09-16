using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Data.Repositories;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Data.Repositories;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(List<TicketListItem> Items, int TotalCount)> GetPagedAsync(
        TicketFilterParams filter,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (!canViewAll)
        {
            query = query.Where(t => t.AssignedToUserId == currentUserId);
        }
        else if (!string.IsNullOrEmpty(filter.AssignedToUserId))
        {
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t =>
                t.TicketCode.Contains(term) ||
                t.Subject.Contains(term) ||
                t.Customer.Name.Contains(term) ||
                t.Customer.Email.Contains(term));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        var now = DateTime.UtcNow;
        if (filter.OverdueOnly == true)
        {
            query = query.Where(t =>
                t.DueAt < now &&
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Closed);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TicketListItem(
                t.Id,
                t.TicketCode,
                t.Subject,
                t.CustomerId,
                t.Customer.Name,
                t.Priority,
                t.Status,
                t.AssignedToUserId,
                t.AssignedTo != null ? t.AssignedTo.FullName : null,
                t.CreatedAt,
                t.DueAt,
                now > t.DueAt && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed
            ))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Ticket?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.StatusHistory)
                .ThenInclude(h => h.ChangedBy)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.ChangedBy)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.PreviousAssignee)
            .Include(t => t.AssignmentHistory)
                .ThenInclude(h => h.NewAssignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<string?> GetLatestTicketCodeByYearPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t => t.TicketCode.StartsWith(prefix))
            .OrderByDescending(t => t.TicketCode)
            .Select(t => t.TicketCode)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
