namespace CulinaryBlog.Application.Interfaces;

using System.Data;

public interface IUnitOfWork
{
    Task<IExecutionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<IExecutionTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
}

public interface IExecutionTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
