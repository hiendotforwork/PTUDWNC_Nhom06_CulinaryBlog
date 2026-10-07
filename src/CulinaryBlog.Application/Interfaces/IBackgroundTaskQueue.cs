namespace CulinaryBlog.Application.Interfaces;

// Writes durable task intent inside the caller's database transaction.
public interface IBackgroundTaskQueue
{
    Task WelcomeAsync(string userId, CancellationToken ct);
    Task ResizeAsync(Guid imageId, CancellationToken ct);
}
