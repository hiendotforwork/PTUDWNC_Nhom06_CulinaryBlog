namespace CulinaryBlog.Integration.Tests;

using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class TestAuthenticationService : IAuthenticationService
{
    private readonly IAuthenticationService _inner;
    public static ClaimsPrincipal? ExternalPrincipal { get; set; }

    public TestAuthenticationService(IAuthenticationService inner)
    {
        _inner = inner;
    }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        if (scheme == IdentityConstants.ExternalScheme && ExternalPrincipal != null)
        {
            var ticket = new AuthenticationTicket(ExternalPrincipal, IdentityConstants.ExternalScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return _inner.AuthenticateAsync(context, scheme);
    }

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        _inner.ChallengeAsync(context, scheme, properties);

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        _inner.ForbidAsync(context, scheme, properties);

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
        _inner.SignInAsync(context, scheme, principal, properties);

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
        _inner.SignOutAsync(context, scheme, properties);
}

public class GoogleOAuthCallbackTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GoogleOAuthCallbackTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateTestFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.First(s => s.ServiceType == typeof(IAuthenticationService));
                services.Remove(descriptor);
                services.AddTransient<IAuthenticationService>(sp =>
                {
                    var inner = (IAuthenticationService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
                    return new TestAuthenticationService(inner);
                });
            });
        });
    }

    [Fact]
    public async Task GoogleCallback_WhenEmailVerifiedIsTrue_RedirectsWithTokens_AndSetsEmailConfirmed()
    {
        // Arrange
        var testFactory = CreateTestFactory();
        using var client = testFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"google_verified_{Guid.NewGuid():N}@culinaryblog.vn";
        TestAuthenticationService.ExternalPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "google-verified-sub-1"),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Google Verified User"),
            new Claim("email_verified", "true")
        }, IdentityConstants.ExternalScheme));

        // Act
        var response = await client.GetAsync("/api/v1/auth/google-callback");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/auth/google-callback#");
        location.Should().Contain("accessToken=");
        location.Should().Contain("refreshToken=");

        using var scope = testFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        user!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task GoogleCallback_WhenEmailVerifiedIsFalse_RedirectsToLoginWithError()
    {
        // Arrange
        var testFactory = CreateTestFactory();
        using var client = testFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"google_unverified_{Guid.NewGuid():N}@culinaryblog.vn";
        TestAuthenticationService.ExternalPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "google-unverified-sub-2"),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Google Unverified User"),
            new Claim("email_verified", "false")
        }, IdentityConstants.ExternalScheme));

        // Act
        var response = await client.GetAsync("/api/v1/auth/google-callback");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/login?error=");

        using var scope = testFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().BeNull();
    }

    [Fact]
    public async Task GoogleCallback_WhenEmailVerifiedClaimMissing_RedirectsToLoginWithError()
    {
        // Arrange
        var testFactory = CreateTestFactory();
        using var client = testFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"google_missing_{Guid.NewGuid():N}@culinaryblog.vn";
        TestAuthenticationService.ExternalPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "google-missing-sub-3"),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Google Missing Verified User")
        }, IdentityConstants.ExternalScheme));

        // Act
        var response = await client.GetAsync("/api/v1/auth/google-callback");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/login?error=");

        using var scope = testFactory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().BeNull();
    }
}
