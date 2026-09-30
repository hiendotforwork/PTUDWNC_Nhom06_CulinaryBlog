namespace CulinaryBlog.Application.Interfaces;

public interface IFileDeletionQueue
{
    void Enqueue(string url);
}
