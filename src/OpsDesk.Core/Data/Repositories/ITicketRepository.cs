using OpsDesk.Core.Entities;
using OpsDesk.Core.Services;

namespace OpsDesk.Core.Data.Repositories;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<(List<TicketListItem> Items, int TotalCount)> GetPagedAsync(
        TicketFilterParams filter,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken = default);

    Task<Ticket?> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<string?> GetLatestTicketCodeByYearPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
