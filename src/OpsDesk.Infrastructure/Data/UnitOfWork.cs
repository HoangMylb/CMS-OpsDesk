using Microsoft.EntityFrameworkCore;
using OpsDesk.Core.Data;
using OpsDesk.Core.Data.Repositories;
using OpsDesk.Core.Entities;
using OpsDesk.Infrastructure.Data.Repositories;

namespace OpsDesk.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private bool _disposed;

    private ITicketRepository? _tickets;
    private ICustomerRepository? _customers;
    private IAuditLogRepository? _auditLogs;
    private IRepository<TicketMessage>? _messages;
    private IRepository<TicketStatusHistory>? _statusHistories;
    private IRepository<TicketAssignmentHistory>? _assignmentHistories;
    private IRepository<Department>? _departments;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ITicketRepository Tickets => _tickets ??= new TicketRepository(_context);
    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
    public IAuditLogRepository AuditLogs => _auditLogs ??= new AuditLogRepository(_context);
    public IRepository<TicketMessage> Messages => _messages ??= new Repository<TicketMessage>(_context);
    public IRepository<TicketStatusHistory> StatusHistories => _statusHistories ??= new Repository<TicketStatusHistory>(_context);
    public IRepository<TicketAssignmentHistory> AssignmentHistories => _assignmentHistories ??= new Repository<TicketAssignmentHistory>(_context);
    public IRepository<Department> Departments => _departments ??= new Repository<Department>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
