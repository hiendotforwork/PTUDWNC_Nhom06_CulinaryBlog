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
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            AuthException authEx => new ErrorResponse(
                authEx.StatusCode,
                authEx.ErrorCode,
                authEx.Message,
                null
            ),
            AuthConflictException conflictEx => new ErrorResponse(
                (int)HttpStatusCode.Conflict,
                conflictEx.ErrorCode,
                conflictEx.Message,
                null
            ),
            ValidationException validationEx => new ErrorResponse(
                (int)HttpStatusCode.UnprocessableEntity,
                "VALIDATION_ERROR",
                "Dữ liệu không hợp lệ.",
                validationEx.Errors.Select(e => new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))
            ),
            IdentityOperationException identityEx => new ErrorResponse(
                (int)HttpStatusCode.BadRequest,
                "IDENTITY_ERROR",
                identityEx.Message,
                identityEx.Errors.Select(e => new ValidationErrorDetail("Identity", e))
            ),
            _ => new ErrorResponse(
                (int)HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "Đã có lỗi hệ thống xảy ra. Vui lòng thử lại sau.",
                null
            )
        };

        context.Response.StatusCode = response.StatusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    public record ValidationErrorDetail(string Field, string Message);

    public record ErrorResponse(
        int StatusCode,
        string ErrorCode,
        string Message,
        IEnumerable<ValidationErrorDetail>? Errors
    );
}
