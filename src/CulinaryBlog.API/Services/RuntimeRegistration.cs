using System.Text.Json;
using Hangfire;
using Hangfire.Dashboard;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

namespace CulinaryBlog.API.Services;

public sealed class AdminDashboardAuthorization : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) =>
        context.GetHttpContext().User.Identity?.IsAuthenticated == true && context.GetHttpContext().User.IsInRole("Admin");
}

public static class RuntimeRegistration
{
    public static void AddRuntimeModules(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, log) =>
        {
            log.MinimumLevel.Is(context.HostingEnvironment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Npgsql", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter());
            if (!context.HostingEnvironment.IsEnvironment("Testing"))
                log.WriteTo.File(new RenderedCompactJsonFormatter(), "logs/culinary-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);
            var seq = context.Configuration["Observability:SeqUrl"];
            if (context.HostingEnvironment.IsDevelopment() && !string.IsNullOrWhiteSpace(seq)) log.WriteTo.Seq(seq);
        });
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        builder.Services.AddScoped<WelcomeEmailJob>();
        builder.Services.AddScoped<ImageResizeJob>();
        builder.Services.AddScoped<SitemapGenerationJob>();
        builder.Services.AddScoped<IOriginalImageReader, OriginalImageReader>();
        if (builder.Environment.IsEnvironment("Testing"))
        {
            builder.Services.AddScoped<IBackgroundTaskQueue, NoBackgroundTaskQueue>();
        }
        else
        {
            builder.Services.AddScoped<IBackgroundTaskQueue, DatabaseBackgroundTaskQueue>();
            builder.Services.AddHostedService<BackgroundTaskDispatcher>();
        }
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(builder.Configuration["Redis:Connection"] ?? "localhost:6379");
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 2000;
            options.AsyncTimeout = 2000;
            return ConnectionMultiplexer.Connect(options);
        });
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"], timeout: TimeSpan.FromSeconds(3))
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"], timeout: TimeSpan.FromSeconds(3))
            .AddCheck<StorageHealthCheck>("storage", tags: ["storage"], timeout: TimeSpan.FromSeconds(3));
        var telemetry = builder.Services.AddOpenTelemetry().ConfigureResource(r => r.AddService("CulinaryBlog.API"));
        var endpoint = builder.Configuration["Observability:OtlpEndpoint"];
        telemetry.WithTracing(trace =>
        {
            trace.AddSource(ObservabilityTelemetry.Name, "Npgsql").AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (!string.IsNullOrWhiteSpace(endpoint)) trace.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(endpoint.TrimEnd('/') + "/v1/traces");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }).WithMetrics(metrics =>
        {
            metrics.AddMeter(ObservabilityTelemetry.Name).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation();
            if (!string.IsNullOrWhiteSpace(endpoint)) metrics.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(endpoint.TrimEnd('/') + "/v1/metrics");
                o.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        });
    }

    public static void MapRuntimeModules(this WebApplication app)
    {
        static Task Write(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
        {
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(new {
                status = report.Status.ToString(),
                components = report.Entries.ToDictionary(x => x.Key, x => new { status = x.Value.Status.ToString(), description = x.Value.Description })
            }));
        }
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = Write });
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = Write });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready"), ResponseWriter = Write });
        app.MapGet("/robots.txt", (IConfiguration configuration) =>
            Results.Text("User-agent: *\nAllow: /\nSitemap: " +
                (configuration["Site:PublicApiUrl"] ?? "http://localhost:5058").TrimEnd('/') + "/sitemap.xml\n", "text/plain"));
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions {
                Authorization = [new AdminDashboardAuthorization()],
                DashboardTitle = "Culinary Blog Jobs"
            });
            var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
            recurring.AddOrUpdate<SitemapGenerationJob>("culinary-sitemap",
                job => job.RunAsync(CancellationToken.None), "0 2 * * *", new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        }
    }
}
