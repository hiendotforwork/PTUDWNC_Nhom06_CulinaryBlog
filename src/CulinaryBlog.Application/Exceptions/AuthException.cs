namespace CulinaryBlog.Application.Exceptions;

public class AuthException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    public IDictionary<string, object?> Extensions { get; }

    public AuthException(
        string errorCode,
        string message,
        int statusCode = 401,
        IDictionary<string, object?>? extensions = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }

    public static AuthException InvalidCredentials() =>
        new("AUTH_INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.", 401);

    public static AuthException AccountLocked(DateTime unlockAt)
    {
        var retryAfter = Math.Max(0, (int)(unlockAt - DateTime.UtcNow).TotalSeconds);
        return new("AUTH_ACCOUNT_LOCKED",
            $"Tài khoản đã bị khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {unlockAt:HH:mm:ss}.",
            423,
            new Dictionary<string, object?>
            {
                ["unlockAt"] = unlockAt.ToString("o"),
                ["retryAfterSeconds"] = retryAfter
            });
    }

    public static AuthException UserNotFound() =>
        new("AUTH_USER_NOT_FOUND", "Tài khoản không tồn tại.", 404);
}
