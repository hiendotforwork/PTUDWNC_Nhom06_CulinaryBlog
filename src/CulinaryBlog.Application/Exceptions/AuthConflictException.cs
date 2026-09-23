namespace CulinaryBlog.Application.Exceptions;

public class AuthConflictException : Exception
{
    public string ErrorCode { get; }

    public AuthConflictException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public static AuthConflictException EmailExists() =>
        new("AUTH_EMAIL_EXISTS", "Email đã được đăng ký trong hệ thống.");

    public static AuthConflictException UserNameExists() =>
        new("AUTH_USERNAME_EXISTS", "Tên đăng nhập đã được sử dụng.");
}
