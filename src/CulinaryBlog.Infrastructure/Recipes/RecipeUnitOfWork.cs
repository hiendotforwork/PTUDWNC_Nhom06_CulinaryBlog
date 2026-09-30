// Tệp này cài đặt Unit of Work riêng cho FR-RCP và quản lý transaction EF Core.

namespace CulinaryBlog.Infrastructure.Recipes;

using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

// Class lưu thay đổi và quản lý transaction cho các thao tác FR-RCP.
// Input: ApplicationDbContext. Output: kết quả lưu hoặc transaction phù hợp provider.
public sealed class RecipeUnitOfWork(ApplicationDbContext db) : IRecipeUnitOfWork
{
    // Chức năng: lưu các thay đổi đang theo dõi. Input: cancellationToken. Output: số bản ghi.
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);

    // Chức năng: mở transaction thật với database quan hệ hoặc transaction rỗng khi test InMemory.
    // Input: cancellationToken. Output: IRecipeTransaction.
    public async Task<IRecipeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational()) return new NoOpRecipeTransaction();
        return new EfRecipeTransaction(await db.Database.BeginTransactionAsync(cancellationToken));
    }

    private sealed class EfRecipeTransaction(IDbContextTransaction transaction) : IRecipeTransaction
    {
        // Xác nhận transaction EF. Input: cancellationToken. Output: Task.
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        // Hoàn tác transaction EF. Input: cancellationToken. Output: Task.
        public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
        // Giải phóng transaction EF. Input: không có. Output: ValueTask.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    private sealed class NoOpRecipeTransaction : IRecipeTransaction
    {
        // Hoàn tất commit giả trong InMemory test. Input: cancellationToken. Output: Task hoàn tất.
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        // Hoàn tất rollback giả trong InMemory test. Input: cancellationToken. Output: Task hoàn tất.
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        // Giải phóng transaction giả. Input: không có. Output: ValueTask hoàn tất.
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
