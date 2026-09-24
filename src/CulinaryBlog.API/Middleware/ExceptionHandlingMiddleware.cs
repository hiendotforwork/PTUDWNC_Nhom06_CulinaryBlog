namespace CulinaryBlog.API.Middleware;

using System.Net;
using System.Text.Json;
using CulinaryBlog.Application.Exceptions;
using FluentValidation;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate _next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (ex is ValidationException or AuthConflictException or IdentityOperationException or AuthException)
            {
                _logger.LogWarning("Handled request exception: {Message}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, errorCode, title, detail, errors, customExtensions) = exception switch
        {
            AuthException authEx => (
                authEx.StatusCode,
                authEx.ErrorCode,
                authEx.StatusCode == 423 ? "Account locked" : "Authentication failed",
                authEx.Message,
                null as IEnumerable<ValidationErrorDetail>,
                authEx.Extensions
            ),
            AuthConflictException conflictEx => (
                (int)HttpStatusCode.Conflict,
                conflictEx.ErrorCode,
                "Conflict",
                conflictEx.Message,
                null as IEnumerable<ValidationErrorDetail>,
                null as IDictionary<string, object?>
            ),
            ValidationException validationEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "VALIDATION_ERROR",
                "Validation failed",
                "Dữ liệu không hợp lệ.",
                validationEx.Errors.Select(e => new ValidationErrorDetail(e.PropertyName, e.ErrorMessage)),
                null as IDictionary<string, object?>
            ),
            IdentityOperationException identityEx => (
                (int)HttpStatusCode.BadRequest,
                "IDENTITY_ERROR",
                "Identity operation failed",
                identityEx.Message,
                identityEx.Errors.Select(e => new ValidationErrorDetail("Identity", e)),
                null as IDictionary<string, object?>
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "Internal server error",
                "Đã có lỗi hệ thống xảy ra. Vui lòng thử lại sau.",
                null as IEnumerable<ValidationErrorDetail>,
                null as IDictionary<string, object?>
            )
        };

        context.Response.StatusCode = statusCode;

        var extensionsDict = new Dictionary<string, object?>
        {
            ["code"] = errorCode,
            ["traceId"] = context.TraceIdentifier,
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };

        if (errors != null)
        {
            extensionsDict["errors"] = errors;
        }

        if (customExtensions != null)
        {
            foreach (var kvp in customExtensions)
            {
                extensionsDict[kvp.Key] = kvp.Value;
            }
        }

        var response = new ProblemDetailsResponse(
            Type: $"https://culinaryblog.com/errors/auth/{errorCode}",
            Title: title,
            Status: statusCode,
            Detail: detail,
            Instance: context.Request.Path,
            StatusCode: statusCode,
            ErrorCode: errorCode,
            Message: detail,
            Errors: errors,
            Extensions: extensionsDict
        );

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    public record ValidationErrorDetail(string Field, string Message);

    public record ProblemDetailsResponse(
        string Type,
        string Title,
        int Status,
        string Detail,
        string Instance,
        int StatusCode,
        string ErrorCode,
        string Message,
        IEnumerable<ValidationErrorDetail>? Errors,
        Dictionary<string, object?> Extensions
    );
}
