using System.Diagnostics;
using System.Security.Claims;
using CulinaryBlog.API.Services;
using Serilog.Context;

namespace CulinaryBlog.API.Middleware;

public sealed class CorrelationLoggingMiddleware(RequestDelegate next, ILogger<CorrelationLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
        var id = supplied.Length is > 0 and <= 64 && supplied.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? supplied : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = id;
        context.Response.Headers["X-Correlation-ID"] = id;
        using var correlation = LogContext.PushProperty("CorrelationId", id);
        using var trace = LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString());
        var watch = Stopwatch.StartNew();
        try { await next(context); }
        finally
        {
            // Endpoint patterns keep metric cardinality bounded; omit query strings.
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            var tags = new TagList { { "http.request.method", context.Request.Method },
                { "http.route", route }, { "http.response.status_code", context.Response.StatusCode } };
            ObservabilityTelemetry.Requests.Add(1, tags);
            ObservabilityTelemetry.Duration.Record(watch.Elapsed.TotalMilliseconds, tags);
            if (context.Response.StatusCode >= 500) ObservabilityTelemetry.Errors.Add(1, tags);
            logger.Log(context.Response.StatusCode >= 500 ? LogLevel.Error : watch.ElapsedMilliseconds > 500 ? LogLevel.Warning : LogLevel.Information,
                "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMs} ms; UserId {UserId}",
                context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, watch.Elapsed.TotalMilliseconds,
                context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}
