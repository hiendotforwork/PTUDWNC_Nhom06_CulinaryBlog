using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;

namespace CulinaryBlog.API.Services;

public static class ObservabilityTelemetry
{
    public const string Name = "CulinaryBlog";
    public static readonly ActivitySource Activities = new(Name);
    public static readonly Meter Meter = new(Name);
    public static readonly Counter<long> RecipesCreated = Meter.CreateCounter<long>("culinary.recipe.created");
    public static readonly Counter<long> RecipesPublished = Meter.CreateCounter<long>("culinary.recipe.published");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("culinary.http.requests");
    public static readonly Counter<long> Errors = Meter.CreateCounter<long>("culinary.http.errors");
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("culinary.http.duration", "ms");
}

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        using var activity = ObservabilityTelemetry.Activities.StartActivity("mediatr." + name);
        var watch = Stopwatch.StartNew();
        // Log request type only: never serialize passwords, tokens or payloads.
        logger.LogInformation("Handling {RequestType}", name);
        try { return await next(); }
        catch (Exception ex) { activity?.SetStatus(ActivityStatusCode.Error); logger.LogError(ex, "{RequestType} failed", name); throw; }
        finally { logger.Log(watch.ElapsedMilliseconds > 500 ? LogLevel.Warning : LogLevel.Information,
            "Handled {RequestType} in {ElapsedMs} ms", name, watch.Elapsed.TotalMilliseconds); }
    }
}
