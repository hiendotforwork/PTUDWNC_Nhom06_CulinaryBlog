// Tệp này khai báo Unit of Work riêng cho các thao tác ghi FR-RCP.

namespace CulinaryBlog.Application.Recipes.Repositories;

// Interface điều phối lưu thay đổi và transaction của FR-RCP.
// Input: thay đổi đang được DbContext theo dõi. Output: số bản ghi hoặc transaction.
public interface IRecipeUnitOfWork
{
    // Lưu toàn bộ thay đổi. Input: cancellationToken. Output: số bản ghi bị tác động.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Mở transaction cho thao tác nhiều bước. Input: cancellationToken. Output: IRecipeTransaction.
    Task<IRecipeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

// Interface bọc transaction để Application không phụ thuộc EF Core.
// Input: lệnh commit/rollback. Output: Task hoàn tất.
public interface IRecipeTransaction : IAsyncDisposable
{
    // Xác nhận transaction. Input: cancellationToken. Output: Task.
    Task CommitAsync(CancellationToken cancellationToken = default);
    // Hoàn tác transaction. Input: cancellationToken. Output: Task.
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
