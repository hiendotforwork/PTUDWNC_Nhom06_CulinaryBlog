namespace CulinaryBlog.Integration.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.DTOs.Auth;
using FluentAssertions;
using Xunit;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
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
    public async Task Register_WithValidData_Returns201CreatedWithAuthResponse()
    {
        // Arrange
        var request = CreateUniqueRegisterRequest("valid");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.RefreshToken.Should().NotBeNullOrWhiteSpace();
        content.User.Should().NotBeNull();
        content.User.Email.Should().Be(request.Email);
        content.User.UserName.Should().Be(request.UserName);
        content.User.DisplayName.Should().Be(request.DisplayName);
        content.User.Roles.Should().Contain("Author");
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
    public async Task Register_ResponseAccessToken_HasValidJwtStructureAndClaims()
    {
        // Arrange
        var request = CreateUniqueRegisterRequest("jwt_check");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
        authResponse.Should().NotBeNull();

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(authResponse!.AccessToken).Should().BeTrue();

        var jwt = handler.ReadJwtToken(authResponse.AccessToken);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(request.Email);
        jwt.Claims.First(c => c.Type == "name").Value.Should().Be(request.DisplayName);
        jwt.Claims.Where(c => c.Type == "roles" || c.Type == "role").Select(c => c.Value).Should().Contain("Author");
    }
}
