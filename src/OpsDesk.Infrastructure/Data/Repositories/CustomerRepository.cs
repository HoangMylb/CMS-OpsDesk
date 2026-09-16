using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Data.Repositories;
using OpsDesk.Core.Entities;
using OpsDesk.Core.Enums;
using OpsDesk.Core.Services;

namespace OpsDesk.Infrastructure.Data.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(List<CustomerListItem> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Name.Contains(term) ||
                c.Email.Contains(term) ||
                (c.Company != null && c.Company.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItem(
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Company,
                c.Tickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
                c.Tickets.Count,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Customer?> GetDetailWithTicketsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(c => c.Tickets)
                .ThenInclude(t => t.AssignedTo)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return await DbSet
            .AsNoTracking()
            .AnyAsync(c => c.Email.ToLower() == normalized && (!excludeId.HasValue || c.Id != excludeId.Value), cancellationToken);
    }
}
