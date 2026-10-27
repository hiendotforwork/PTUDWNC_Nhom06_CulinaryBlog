namespace CulinaryBlog.Integration.Tests.Helpers;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class AuthTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<AuthResponse> RegisterConfirmedUserAsync(
        HttpClient client,
        IServiceProvider services,
        RegisterRequest request)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var pending = await registerResponse.Content.ReadFromJsonAsync<RegistrationPendingResponse>(JsonOptions);
        pending.Should().NotBeNull();
        pending!.Email.Should().Be(request.Email);
        pending.EmailConfirmationRequired.Should().BeTrue();

        string userId;
        string token;
        using (var scope = services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(request.Email);
            user.Should().NotBeNull();
            userId = user!.Id;
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        var confirmRequest = new ConfirmEmailRequest(userId, token);
        var confirmResponse = await client.PostAsJsonAsync("/api/v1/auth/email/confirm", confirmRequest);
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await confirmResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        authResponse.Should().NotBeNull();
        return authResponse!;
    }
}
