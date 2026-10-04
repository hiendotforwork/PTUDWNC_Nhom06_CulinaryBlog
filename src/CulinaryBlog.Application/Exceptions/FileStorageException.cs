namespace CulinaryBlog.Application.Exceptions;

public sealed class InvalidImageException(string message) : Exception(message);
public sealed class FileStorageException(string message, Exception innerException) : Exception(message, innerException);
