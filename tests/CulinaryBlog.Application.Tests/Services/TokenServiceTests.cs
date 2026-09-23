namespace CulinaryBlog.Application.Tests.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

public class TokenServiceTests
{
    private const string TestSecret = "CulinaryBlogSuperSecretKeyForJwtAuthenticationTestPurposeOnly12345!";
    private const string TestIssuer = "CulinaryBlogTest";
    private const string TestAudience = "CulinaryBlogClientTest";
    private const int TestExpiryMinutes = 15;

    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:Secret", TestSecret},
            {"Jwt:Issuer", TestIssuer},
            {"Jwt:Audience", TestAudience},
            {"Jwt:ExpiryMinutes", TestExpiryMinutes.ToString()}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeCorrectClaimsAndExpiry()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "testuser",
            DisplayName = "Test User"
        };
        var roles = new List<string> { "Author" };

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        tokenString.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        token.Issuer.Should().Be(TestIssuer);
        token.Audiences.Should().Contain(TestAudience);
        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id);
        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(user.Email);
        token.Claims.First(c => c.Type == "name").Value.Should().Be(user.DisplayName);
        token.Claims.Where(c => c.Type == "role" || c.Type == "roles" || c.Type == ClaimTypes.Role).Select(c => c.Value).Should().Contain("Author");

        // Expiration check: should be ~15 minutes from now
        token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));
        token.ValidTo.Should().BeBefore(DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnCryptographicallySecureString()
    {
        // Act
        var refreshToken1 = _tokenService.GenerateRefreshToken();
        var refreshToken2 = _tokenService.GenerateRefreshToken();

        // Assert
        refreshToken1.Should().NotBeNullOrWhiteSpace();
        refreshToken2.Should().NotBeNullOrWhiteSpace();
        refreshToken1.Should().NotBe(refreshToken2);
        // Minimum 64 hex characters (32 bytes) or base64 equivalent
        refreshToken1.Length.Should().BeGreaterOrEqualTo(40);
    }

    [Fact]
    public void GenerateAccessToken_ShouldBeVerifiableWithValidationParameters()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test2@example.com",
            UserName = "testuser2",
            DisplayName = "Test User 2"
        };
        var roles = new List<string> { "Author" };

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecret);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = TestIssuer,
            ValidateAudience = true,
            ValidAudience = TestAudience,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(tokenString, validationParameters, out var validatedToken);
        principal.Should().NotBeNull();
        validatedToken.Should().NotBeNull();
    }
}
