using CulinaryBlog.API.Services;
using CulinaryBlog.Domain.Constants;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/v1/admin/jobs")]
public sealed class AdminJobsController(IBackgroundJobClient jobs) : ControllerBase
{
    [HttpPost("sitemap")]
    public IActionResult GenerateSitemap()
    {
        var jobId = jobs.Enqueue<SitemapGenerationJob>(job => job.RunAsync(CancellationToken.None));
        return Accepted(new { jobId });
    }
}
