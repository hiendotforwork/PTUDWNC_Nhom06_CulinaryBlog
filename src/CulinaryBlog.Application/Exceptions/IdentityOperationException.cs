namespace CulinaryBlog.Application.Exceptions;

public class IdentityOperationException : Exception
{
    public IEnumerable<string> Errors { get; }

    public IdentityOperationException(string message, IEnumerable<string> errors) : base(message)
    {
        Errors = errors;
    }
}
