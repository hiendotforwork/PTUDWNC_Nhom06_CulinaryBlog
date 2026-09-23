namespace CulinaryBlog.Application.Exceptions;

public class AuthException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public AuthException(string errorCode, string message, int statusCode = 401)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public static AuthException InvalidCredentials() =>
        new("AUTH_INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.", 401);

    public static AuthException AccountLocked(DateTime unlockAt) =>
        new("AUTH_ACCOUNT_LOCKED",
            $"Tài khoản đã bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {unlockAt:HH:mm:ss}.",
            423);

    public static AuthException UserNotFound() =>
        new("AUTH_USER_NOT_FOUND", "Tài khoản không tồn tại.", 404);
}
