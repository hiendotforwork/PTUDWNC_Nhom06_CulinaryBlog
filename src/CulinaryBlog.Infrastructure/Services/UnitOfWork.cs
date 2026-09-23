namespace CulinaryBlog.Infrastructure.Services;

using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IExecutionTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return new NoOpExecutionTransaction();
        }

        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfExecutionTransaction(transaction);
    }

    private sealed class EfExecutionTransaction : IExecutionTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfExecutionTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }

    private sealed class NoOpExecutionTransaction : IExecutionTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
