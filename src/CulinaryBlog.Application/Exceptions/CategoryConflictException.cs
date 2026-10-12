namespace CulinaryBlog.Application.Exceptions;

public sealed class CategoryConflictException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}
