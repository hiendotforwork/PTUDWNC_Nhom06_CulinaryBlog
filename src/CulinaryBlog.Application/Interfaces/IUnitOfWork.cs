namespace CulinaryBlog.Application.Interfaces;

public interface IUnitOfWork
{
    Task<IExecutionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IExecutionTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
