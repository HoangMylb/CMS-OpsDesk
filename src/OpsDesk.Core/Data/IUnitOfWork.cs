using OpsDesk.Core.Data.Repositories;
using OpsDesk.Core.Entities;

namespace OpsDesk.Core.Data;

/// <summary>
/// Unit of Work pattern interface.
/// Coordinates data access across domain repositories and maintains transaction boundaries.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    ITicketRepository Tickets { get; }
    ICustomerRepository Customers { get; }
    IAuditLogRepository AuditLogs { get; }
    IRepository<TicketMessage> Messages { get; }
    IRepository<TicketStatusHistory> StatusHistories { get; }
    IRepository<TicketAssignmentHistory> AssignmentHistories { get; }
    IRepository<Department> Departments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
