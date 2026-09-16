namespace OpsDesk.Core.Data;

/// <summary>
/// Unit of Work pattern — đóng gói ranh giới giao dịch (transaction boundary)
/// Giúp kiểm soát transaction cực ngắn (short-lived transactions), đảm bảo
/// tính nhất quán toàn vẹn dữ liệu và dễ dàng kiểm thử.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Lưu mọi thay đổi trong DbContext vào Database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi một hành động bên trong một Transaction ngắn có rollback tự động khi gặp lỗi.
    /// </summary>
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi và trả về kết quả bên trong một Transaction ngắn có rollback tự động.
    /// </summary>
    Task<T> ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
