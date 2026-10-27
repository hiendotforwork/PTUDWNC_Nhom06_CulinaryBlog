namespace CulinaryBlog.Integration.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Integration.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static RegisterRequest CreateUniqueRegisterRequest(string prefix = "user")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return new RegisterRequest(
            Email: $"{prefix}_{suffix}@culinaryblog.vn",
            UserName: $"{prefix}_{suffix}",
            DisplayName: $"Chef {prefix} {suffix}",
            Password: "Password123!"
        );
    }

    [Fact]
    public async Task Register_WithValidData_Returns201CreatedWithRegistrationPendingResponse()
    {
        // Arrange
        var request = CreateUniqueRegisterRequest("valid");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<RegistrationPendingResponse>(_jsonOptions);
        content.Should().NotBeNull();
        content!.Email.Should().Be(request.Email);
        content.EmailConfirmationRequired.Should().BeTrue();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("accessToken");
        body.Should().NotContain("refreshToken");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns422UnprocessableEntity()
    {
        // Arrange
        var request = CreateUniqueRegisterRequest("invalid_email") with { Email = "not-a-valid-email" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("VALIDATION_ERROR");
        body.Should().Contain("Email");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409ConflictWithEmailExistsCode()
    {
        // Arrange
        var initialRequest = CreateUniqueRegisterRequest("dup_email");
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", initialRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateRequest = CreateUniqueRegisterRequest("diff_user") with { Email = initialRequest.Email };

        // Act
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicateRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadAsStringAsync();
        body.Should().Contain("AUTH_EMAIL_EXISTS");
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_Returns409ConflictWithUsernameExistsCode()
    {
        // Arrange
        var initialRequest = CreateUniqueRegisterRequest("dup_uname");
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", initialRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateRequest = CreateUniqueRegisterRequest("diff_email") with { UserName = initialRequest.UserName };

        // Act
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", duplicateRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondResponse.Content.ReadAsStringAsync();
        body.Should().Contain("AUTH_USERNAME_EXISTS");
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns422UnprocessableEntity()
    {
        // Arrange - missing uppercase, digit, and special char
        var request = CreateUniqueRegisterRequest("weak_pwd") with { Password = "password" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("VALIDATION_ERROR");
        body.Should().Contain("Password");
    }

    [Fact]
    public async Task ConfirmEmail_ResponseAccessToken_HasValidJwtStructureAndClaims()
    {
        // Arrange
        var request = CreateUniqueRegisterRequest("jwt_check");
        var authResponse = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, request);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(authResponse.AccessToken).Should().BeTrue();

        var jwt = handler.ReadJwtToken(authResponse.AccessToken);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(request.Email);
        jwt.Claims.First(c => c.Type == "name").Value.Should().Be(request.DisplayName);
        jwt.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).Should().Contain("Author");
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200OKWithTokens()
    {
        // Arrange - register and confirm user first
        var registerRequest = CreateUniqueRegisterRequest("login_valid");
        await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);

        var loginRequest = new LoginRequest(registerRequest.Email, registerRequest.Password);

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.User.Email.Should().Be(registerRequest.Email);
    }

    [Fact]
    public async Task Login_WhenUnconfirmed_Returns403EmailNotConfirmed()
    {
        // Arrange - register user without confirming
        var registerRequest = CreateUniqueRegisterRequest("login_unconf");
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRequest = new LoginRequest(registerRequest.Email, registerRequest.Password);

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await loginResponse.Content.ReadAsStringAsync();
        body.Should().Contain("EMAIL_NOT_CONFIRMED");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange - register user first
        var registerRequest = CreateUniqueRegisterRequest("login_wrongpwd");
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRequest = new LoginRequest(registerRequest.Email, "WrongPassword123!");

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await loginResponse.Content.ReadAsStringAsync();
        body.Should().Contain("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401Unauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest("nonexistent_user_9999@culinaryblog.vn", "Password123!");

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await loginResponse.Content.ReadAsStringAsync();
        body.Should().Contain("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_Returns422UnprocessableEntity()
    {
        // Arrange
        var loginRequest = new LoginRequest("", "");

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await loginResponse.Content.ReadAsStringAsync();
        body.Should().Contain("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Login_WhenLockedOutAfter5Attempts_Returns423Locked()
    {
        // Arrange - register user first
        var registerRequest = CreateUniqueRegisterRequest("lockout_test");
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var wrongLoginRequest = new LoginRequest(registerRequest.Email, "WrongPassword123!");

        // Act: Fail 4 times (first 4 return 401 Unauthorized)
        for (var i = 0; i < 4; i++)
        {
            var res = await _client.PostAsJsonAsync("/api/v1/auth/login", wrongLoginRequest);
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 5th attempt reaches max failed attempts (5) and triggers 423 Locked
        var fifthResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", wrongLoginRequest);
        fifthResponse.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body5 = await fifthResponse.Content.ReadAsStringAsync();
        body5.Should().Contain("AUTH_ACCOUNT_LOCKED");
        body5.Should().Contain("unlockAt");
        body5.Should().Contain("retryAfterSeconds");

        // Subsequent attempt while locked also returns 423 Locked
        var sixthResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", wrongLoginRequest);
        sixthResponse.StatusCode.Should().Be(HttpStatusCode.Locked);
        var body6 = await sixthResponse.Content.ReadAsStringAsync();
        body6.Should().Contain("AUTH_ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task GoogleSignIn_ReturnsChallengeOrRedirect()
    {
        // Arrange
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/auth/google-signin");

        // Assert - should return 302 redirect to Google OAuth
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("accounts.google.com");
    }

    [Fact]
    public async Task GoogleCallback_WithoutAuthentication_RedirectsToLoginWithError()
    {
        // Arrange
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/api/v1/auth/google-callback");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/login");
    }

    [Fact]
    public async Task Logout_WithActiveRefreshToken_Returns204AndRevokesToken()
    {
        // Arrange
        var registerRequest = CreateUniqueRegisterRequest("logout_active");
        var authContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);
        var rawRefreshToken = authContent.RefreshToken;
        var tokenHash = RefreshToken.HashToken(rawRefreshToken);

        var logoutRequest = new LogoutRequest(rawRefreshToken);

        // Act - POST without Authorization header
        var clientWithoutAuth = _factory.CreateClient();
        clientWithoutAuth.DefaultRequestHeaders.Authorization = null;
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedToken = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        persistedToken.Should().NotBeNull();
        persistedToken!.RevokedAt.Should().NotBeNull();
        persistedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithUnknownRefreshToken_Returns204()
    {
        // Arrange
        var logoutRequest = new LogoutRequest("non-existent-random-token-123");

        // Act
        var clientWithoutAuth = _factory.CreateClient();
        clientWithoutAuth.DefaultRequestHeaders.Authorization = null;
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/v1/auth/logout", logoutRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task Logout_WithExpiredOrRevokedRefreshToken_Returns204()
    {
        // Arrange - register user to attach tokens to
        var registerRequest = CreateUniqueRegisterRequest("logout_exp_rev");
        var authContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);
        var userId = authContent.User.Id;

        var expiredRawToken = "expired-raw-token-" + Guid.NewGuid().ToString("N");
        var expiredTokenHash = RefreshToken.HashToken(expiredRawToken);
        var expiredToken = RefreshToken.Create(userId, expiredRawToken, "127.0.0.1", -2);

        var revokedRawToken = "revoked-raw-token-" + Guid.NewGuid().ToString("N");
        var revokedTokenHash = RefreshToken.HashToken(revokedRawToken);
        var revokedToken = RefreshToken.Create(userId, revokedRawToken, "127.0.0.1", 7);
        var originalRevokedAt = DateTime.UtcNow.AddHours(-1);
        revokedToken.RevokedAt = originalRevokedAt;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RefreshTokens.AddRange(expiredToken, revokedToken);
            await db.SaveChangesAsync();
        }

        var clientWithoutAuth = _factory.CreateClient();
        clientWithoutAuth.DefaultRequestHeaders.Authorization = null;

        // Act 1: expired token
        var expiredResponse = await clientWithoutAuth.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(expiredRawToken));

        // Act 2: revoked token
        var revokedResponse = await clientWithoutAuth.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(revokedRawToken));

        // Assert
        expiredResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        revokedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbExpired = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == expiredTokenHash);
            dbExpired.Should().NotBeNull();
            dbExpired!.RevokedAt.Should().BeNull(); // Was not modified to revoked

            var dbRevoked = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == revokedTokenHash);
            dbRevoked.Should().NotBeNull();
            dbRevoked!.RevokedAt.Should().BeCloseTo(originalRevokedAt, TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public async Task Logout_WithoutBody_Returns204()
    {
        var response = await _factory.CreateClient().PostAsync("/api/v1/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"refreshToken\":null}")]
    [InlineData("{\"refreshToken\":\"\"}")]
    [InlineData("{\"refreshToken\":\"   \"}")]
    public async Task Logout_WithMissingNullOrBlankRefreshToken_Returns204(string payload)
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/v1/auth/logout",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("malformed")]
    public async Task Logout_WithInvalidAuthorizationHeader_Returns204AndRevokesToken(string authorizationKind)
    {
        var auth = await AuthTestHelpers.RegisterConfirmedUserAsync(
            _client,
            _factory.Services,
            CreateUniqueRegisterRequest("logout_invalid_auth"));
        var rawRefreshToken = auth.RefreshToken;
        var tokenHash = RefreshToken.HashToken(rawRefreshToken);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authorizationKind == "expired" ? CreateExpiredJwt() : "malformed");

        var response = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(rawRefreshToken));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await db.RefreshTokens.FirstAsync(token => token.TokenHash == tokenHash)).RevokedAt.Should().NotBeNull();
    }

    private static string CreateExpiredJwt()
    {
        var token = new JwtSecurityToken(
            issuer: "CulinaryBlog",
            audience: "CulinaryBlogApp",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "expired-user")],
            notBefore: DateTime.UtcNow.AddHours(-2),
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("CulinaryBlogDefaultSuperSecretKeyForDevelopmentAndTestingPurposesOnly32Chars!")),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        // Arrange - register and confirm user to get initial tokens
        var registerRequest = CreateUniqueRegisterRequest("refresh_valid");
        var regContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);
        var originalRefreshToken = regContent.RefreshToken;

        // Act
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(originalRefreshToken));

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(originalRefreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest("invalid-token-12345"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_TokenRotation_OldTokenInvalidated()
    {
        // Arrange - register and confirm user to get initial tokens
        var registerRequest = CreateUniqueRegisterRequest("refresh_reuse");
        var regContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);
        var originalRefreshToken = regContent.RefreshToken;

        // First refresh
        var firstRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(originalRefreshToken));
        firstRefresh.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to use original token again (should fail - reuse attack)
        var secondRefresh = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(originalRefreshToken));

        // Assert
        secondRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string CreateValidJwt(params Claim[] claims)
    {
        var token = new JwtSecurityToken(
            issuer: "CulinaryBlog",
            audience: "CulinaryBlogApp",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("CulinaryBlogDefaultSuperSecretKeyForDevelopmentAndTestingPurposesOnly32Chars!")),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsProfile()
    {
        var registerRequest = CreateUniqueRegisterRequest("me_valid");
        var regContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", regContent.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await response.Content.ReadAsStringAsync();
        rawJson.Should().NotContain("passwordHash").And.NotContain("securityStamp");

        var profile = JsonSerializer.Deserialize<UserProfileDto>(rawJson, _jsonOptions);
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(regContent.User.Id);
        profile.DisplayName.Should().Be(regContent.User.DisplayName);
        profile.Email.Should().Be(regContent.User.Email);
        profile.UserName.Should().Be(regContent.User.UserName);
        profile.AvatarUrl.Should().BeNull();
        profile.Bio.Should().BeNull();
        profile.Roles.Should().Contain("Author");
        profile.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutUserIdClaims_Returns401()
    {
        var token = CreateValidJwt(new Claim("unrelated_claim", "value"));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserDoesNotExist_Returns401()
    {
        var registerRequest = CreateUniqueRegisterRequest("me_deleted");
        var regContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(registerRequest.Email);
            user.Should().NotBeNull();
            var deleteResult = await userManager.DeleteAsync(user!);
            deleteResult.Succeeded.Should().BeTrue();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", regContent.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchCurrentUser_WhenUserDoesNotExist_Returns401()
    {
        var registerRequest = CreateUniqueRegisterRequest("patch_deleted");
        var regContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(registerRequest.Email);
            user.Should().NotBeNull();
            var deleteResult = await userManager.DeleteAsync(user!);
            deleteResult.Succeeded.Should().BeTrue();
        }

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/v1/auth/me")
        {
            Content = JsonContent.Create(new { displayName = "New Name", bio = "New Bio" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", regContent.AccessToken);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidToken_Returns200WithAuthResponse()
    {
        string userId;
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = $"confirm_valid_{Guid.NewGuid():N}@culinaryblog.vn",
                UserName = $"confirm_user_{Guid.NewGuid():N}",
                DisplayName = "Confirm Chef",
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            var createResult = await userManager.CreateAsync(user, "Password123!");
            createResult.Succeeded.Should().BeTrue();
            userId = user.Id;
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        var confirmRequest = new ConfirmEmailRequest(userId, token);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/email/confirm", confirmRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();
        authResponse.RefreshToken.Should().NotBeNullOrWhiteSpace();
        authResponse.User.Id.Should().Be(userId);

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var updatedUser = await userManager.FindByIdAsync(userId);
            updatedUser!.EmailConfirmed.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidToken_Returns400BadRequest()
    {
        string userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = $"confirm_inv_{Guid.NewGuid():N}@culinaryblog.vn",
                UserName = $"confirm_inv_{Guid.NewGuid():N}",
                DisplayName = "Confirm Invalid",
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            await userManager.CreateAsync(user, "Password123!");
            userId = user.Id;
        }

        var confirmRequest = new ConfirmEmailRequest(userId, "completely-invalid-token");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/email/confirm", confirmRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_Replay_Returns409Conflict()
    {
        string userId;
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = $"confirm_replay_{Guid.NewGuid():N}@culinaryblog.vn",
                UserName = $"confirm_rep_{Guid.NewGuid():N}",
                DisplayName = "Confirm Replay",
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            await userManager.CreateAsync(user, "Password123!");
            userId = user.Id;
            token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        var confirmRequest = new ConfirmEmailRequest(userId, token);
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/auth/email/confirm", confirmRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/auth/email/confirm", confirmRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ConfirmEmail_GetMethod_Returns405MethodNotAllowed()
    {
        var response = await _client.GetAsync("/api/v1/auth/email/confirm");
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task ResendEmailConfirmation_AlwaysReturns202Accepted_NoEmailEnumeration()
    {
        var existingEmail = $"resend_exist_{Guid.NewGuid():N}@culinaryblog.vn";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = existingEmail,
                UserName = $"resend_user_{Guid.NewGuid():N}",
                DisplayName = "Resend Chef",
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            await userManager.CreateAsync(user, "Password123!");
        }

        // 1. Existing pending user
        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", new ResendEmailConfirmationRequest(existingEmail));
        response1.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 2. Non-existent user
        var nonExistentEmail = $"resend_none_{Guid.NewGuid():N}@culinaryblog.vn";
        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", new ResendEmailConfirmationRequest(nonExistentEmail));
        response2.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ResendEmailConfirmation_WithInvalidEmail_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", new ResendEmailConfirmationRequest("invalid-email"));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResendEmailConfirmation_GetMethod_Returns405MethodNotAllowed()
    {
        var response = await _client.GetAsync("/api/v1/auth/email/resend");
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task ProtectedEndpoints_WhenUserEmailNotConfirmed_Returns401Unauthorized()
    {
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString("N"),
                Email = $"unconf_jwt_{Guid.NewGuid():N}@culinaryblog.vn",
                UserName = $"unconf_jwt_{Guid.NewGuid():N}",
                DisplayName = "Unconfirmed Chef",
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString("N")
            };
            var createResult = await userManager.CreateAsync(user, "Password123!");
            createResult.Succeeded.Should().BeTrue();
            token = tokenService.GenerateAccessToken(user, new List<string> { "Author" });
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var publicResponse = await _client.GetAsync("/api/v1/categories");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resendResponse = await _client.PostAsJsonAsync("/api/v1/auth/email/resend", new ResendEmailConfirmationRequest("test@culinaryblog.vn"));
        resendResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ProtectedEndpoints_WhenUserEmailConfirmedBecomesFalse_RevokesAccessImmediately()
    {
        var registerRequest = CreateUniqueRegisterRequest("revoke_conf");
        var authContent = await AuthTestHelpers.RegisterConfirmedUserAsync(_client, _factory.Services, registerRequest);

        using (var initialReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me"))
        {
            initialReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authContent.AccessToken);
            var initRes = await _client.SendAsync(initialReq);
            initRes.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(authContent.User.Id);
            user!.EmailConfirmed = false;
            await userManager.UpdateAsync(user);
        }

        using var secondReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        secondReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authContent.AccessToken);
        var secondRes = await _client.SendAsync(secondReq);

        secondRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

