# FR-AUTH Test Plan

> **Tài liệu nguồn:** FR-AUTH_BaoCao.md v1.0.0  
> **Phạm vi:** Test cases cho module Authentication  
> **Phiên bản:** 1.0.0  
> **Ngày tạo:** 04/06/2026

---

## 1. Tổng quan Test Plan

### 1.1. Mục đích

Tài liệu này định nghĩa chi tiết test cases cho module FR-AUTH, bao gồm:
- Unit Tests
- Integration Tests
- E2E Tests
- Security Tests
- Performance Tests

### 1.2. Test Strategy

| Test Type | Coverage Target | Tool |
|-----------|----------------|------|
| Unit Tests | 80% + code coverage | xUnit, Moq, FluentAssertions |
| Integration Tests | Key workflows | xUnit, WebApplicationFactory |
| E2E Tests | Critical user flows | Playwright |
| Security Tests | Authentication flows | Manual + OWASP ZAP |
| Performance Tests | API response times | k6 |

### 1.3. Test Pyramid

```
                    ┌─────────────┐
                    │    E2E     │  10%
                    │   Tests    │  Critical user flows
                   ├─────────────┤
                   │ Integration │  30%
                   │   Tests     │  API workflows
                  ├─────────────┤
                  │   Unit      │  60%
                  │   Tests     │  Individual components
                 └─────────────┘
```

---

## 2. Unit Tests

### 2.1. Registration Tests

#### Test Class: RegisterCommandHandlerTests

```csharp
public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserManager> _userManager;
    private readonly Mock<IJwtService> _jwtService;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepo;
    private readonly RegisterCommandHandler _handler;

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthResponseWithTokens()
    {
        // Arrange
        var command = new RegisterCommand
        {
            DisplayName = "Nguyễn Văn A",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Password123!"
        };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser)null);
        
        _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Author"))
            .ReturnsAsync(IdentityResult.Success);

        _jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()))
            .Returns("access-token");
        
        _jwtService.Setup(x => x.GenerateRefreshToken(It.IsAny<string>()))
            .Returns(new RefreshToken { Token = "refresh-token" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.User.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_EmailAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var command = new RegisterCommand
        {
            DisplayName = "Nguyễn Văn A",
            Email = "existing@example.com",
            UserName = "testuser",
            Password = "Password123!"
        };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(new ApplicationUser { Email = command.Email });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_REG_001");
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsValidationError()
    {
        // Arrange
        var command = new RegisterCommand
        {
            DisplayName = "Nguyễn Văn A",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "weak"
        };

        _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Password too weak" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("VALIDATION_ERROR");
    }
}
```

#### Test Class: RegisterCommandValidatorTests

```csharp
public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator;

    [Theory]
    [InlineData("", false, "DisplayName is required")]
    [InlineData("A", false, "DisplayName too short")]
    [InlineData("Nguyễn Văn A", true, "Valid display name")]
    [InlineData("AB", true, "Minimum valid display name")]
    [InlineData("Nguyễn Văn A".PadRight(101, 'X'), false, "DisplayName too long")]
    public void ValidateDisplayName(string displayName, bool isValid, string reason)
    {
        // Arrange
        var command = CreateValidCommand();
        command.DisplayName = displayName;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
        else
            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData("invalid", false)]
    [InlineData("noemail", false)]
    [InlineData("@nodomain.com", false)]
    [InlineData("valid@email.com", true)]
    [InlineData("user.name+tag@domain.co.uk", true)]
    public void ValidateEmail(string email, bool isValid)
    {
        // Arrange
        var command = CreateValidCommand();
        command.Email = email;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        else
            result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("abc", false, "Too short")]
    [InlineData("ab", false, "Too short")]
    [InlineData("validuser", true, "Valid")]
    [InlineData("user_123", true, "Valid with underscore")]
    [InlineData("User123", true, "Valid with numbers")]
    [InlineData("user@example", false, "Contains special char")]
    [InlineData("user name", false, "Contains space")]
    public void ValidateUserName(string userName, bool isValid, string reason)
    {
        // Arrange
        var command = CreateValidCommand();
        command.UserName = userName;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.UserName);
        else
            result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Theory]
    [InlineData("Short1!", false, "Too short")]
    [InlineData("NoSpecial1", false, "No special char")]
    [InlineData("Nolower1!", false, "No lowercase")]
    [InlineData("NOLOWER1!", false, "No lowercase")]
    [InlineData("NoDigit!", false, "No digit")]
    [InlineData("ValidPass1!", true, "Valid password")]
    [InlineData("Password123!", true, "Valid password")]
    public void ValidatePassword(string password, bool isValid, string reason)
    {
        // Arrange
        var command = CreateValidCommand();
        command.Password = password;

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Password);
        else
            result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    private RegisterCommand CreateValidCommand()
    {
        return new RegisterCommand
        {
            DisplayName = "Nguyễn Văn A",
            Email = "test@example.com",
            UserName = "testuser",
            Password = "Password123!"
        };
    }
}
```

---

### 2.2. Login Tests

#### Test Class: LoginCommandHandlerTests

```csharp
public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = command.Email,
            UserName = "testuser",
            EmailConfirmed = true
        };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);
        
        _userManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);
        
        _userManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new[] { "Author" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "WrongPassword!"
        };

        var user = new ApplicationUser { Email = command.Email };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_LOGIN_001");
    }

    [Fact]
    public async Task Handle_AccountLocked_ReturnsLocked()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser { Email = command.Email };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);
        
        _userManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(true);
        
        _userManager.Setup(x => x.GetLockoutEndDateAsync(user))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(10));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_LOGIN_002");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ResetsFailedAttempts()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "Password123!"
        };

        var user = new ApplicationUser 
        { 
            Email = command.Email,
            AccessFailedCount = 3
        };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);
        
        _userManager.Setup(x => x.IsLockedOutAsync(user))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManager.Verify(x => x.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCredentials_IncrementsFailedAttempts()
    {
        // Arrange
        var command = new LoginCommand
        {
            Email = "user@example.com",
            Password = "WrongPassword!"
        };

        var user = new ApplicationUser { Email = command.Email };

        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userManager.Verify(x => x.AccessFailedAsync(user), Times.Once);
    }
}
```

---

### 2.3. Token Tests

#### Test Class: RefreshTokenHandlerTests

```csharp
public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var userId = "user-123";
        var oldTokenHash = "old-token-hash";
        var command = new RefreshTokenCommand
        {
            RefreshToken = "valid-refresh-token"
        };

        var user = new ApplicationUser { Id = userId };
        var oldToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = oldTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = null
        };

        _refreshTokenRepo.Setup(x => x.GetByTokenAsync(command.RefreshToken))
            .ReturnsAsync(oldToken);
        
        _userManager.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        
        _userManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new[] { "Author" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(oldToken.Token);
        
        // Verify old token was revoked
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "expired-token"
        };

        var expiredToken = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = null
        };

        _refreshTokenRepo.Setup(x => x.GetByTokenAsync(command.RefreshToken))
            .ReturnsAsync(expiredToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_TOKEN_001");
    }

    [Fact]
    public async Task Handle_RevokedToken_DetectsReuseAttack()
    {
        // Arrange
        var command = new RefreshTokenCommand
        {
            RefreshToken = "reused-token"
        };

        var revokedToken = new RefreshToken
        {
            UserId = "user-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            ReplacedByTokenHash = "new-token-hash"
        };

        _refreshTokenRepo.Setup(x => x.GetByTokenAsync(command.RefreshToken))
            .ReturnsAsync(revokedToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_TOKEN_002");
        
        // Verify security alert was logged
        _logger.Verify(x => x.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TOKEN_REUSE")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

#### Test Class: JwtServiceTests

```csharp
public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _settings;

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwt()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-123",
            Email = "user@example.com",
            DisplayName = "Test User"
        };
        var roles = new[] { "Author" };

        // Act
        var token = _jwtService.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "display_name" && c.Value == user.DisplayName);
    }

    [Fact]
    public void GenerateAccessToken_IncludesCorrectExpiration()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123" };

        // Act
        var token = _jwtService.GenerateAccessToken(user, new[] { "Author" });

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var token = _jwtService.GenerateAccessToken(
            new ApplicationUser { Id = "user-123" }, 
            new[] { "Author" });

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user-123");
    }

    [Fact]
    public void ValidateToken_TamperedToken_ThrowsException()
    {
        // Arrange
        var token = _jwtService.GenerateAccessToken(
            new ApplicationUser { Id = "user-123" }, 
            new[] { "Author" });
        
        var tamperedToken = token + "tampered";

        // Act & Assert
        var action = () => _jwtService.ValidateToken(tamperedToken);
        action.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }
}
```

---

### 2.4. Google OAuth Tests

#### Test Class: GoogleLoginCommandHandlerTests

```csharp
public class GoogleLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewGoogleUser_CreatesAccountAndReturnsTokens()
    {
        // Arrange
        var command = new GoogleLoginCommand
        {
            IdToken = "valid-google-token",
            Email = "google@example.com",
            Name = "Google User",
            AvatarUrl = "https://google.com/avatar.jpg"
        };

        var googleUser = new GoogleUserInfo
        {
            Id = "google-123",
            Email = command.Email,
            Name = command.Name,
            Picture = command.AvatarUrl
        };

        _googleAuthService.Setup(x => x.VerifyIdTokenAsync(command.IdToken))
            .ReturnsAsync(googleUser);
        
        _userManager.Setup(x => x.FindByLoginAsync("Google", googleUser.Id))
            .ReturnsAsync((ApplicationUser)null);
        
        _userManager.Setup(x => x.FindByEmailAsync(command.Email))
            .ReturnsAsync((ApplicationUser)null);
        
        _userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Success);
        
        _userManager.Setup(x => x.AddLoginAsync(
            It.IsAny<ApplicationUser>(), 
            It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be(command.Email);
        result.Value.User.DisplayName.Should().Be(command.Name);
    }

    [Fact]
    public async Task Handle_ExistingGoogleUser_ReturnsTokens()
    {
        // Arrange
        var command = new GoogleLoginCommand
        {
            IdToken = "valid-google-token",
            Email = "existing@example.com"
        };

        var existingUser = new ApplicationUser
        {
            Id = "user-123",
            Email = command.Email
        };

        _googleAuthService.Setup(x => x.VerifyIdTokenAsync(command.IdToken))
            .ReturnsAsync(new GoogleUserInfo { Email = command.Email });
        
        _userManager.Setup(x => x.FindByLoginAsync("Google", It.IsAny<string>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Id.Should().Be(existingUser.Id);
        
        // Verify no new user was created
        _userManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var command = new GoogleLoginCommand
        {
            IdToken = "invalid-token"
        };

        _googleAuthService.Setup(x => x.VerifyIdTokenAsync(command.IdToken))
            .ThrowsAsync(new UnauthorizedException("AUTH_GOOGLE_001"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AUTH_GOOGLE_001");
    }
}
```

---

### 2.5. Profile Tests

#### Test Class: UpdateProfileCommandHandlerTests

```csharp
public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidUpdate_UpdatesAndReturnsProfile()
    {
        // Arrange
        var userId = "user-123";
        var command = new UpdateProfileCommand
        {
            DisplayName = "New Name",
            AvatarUrl = "https://example.com/avatar.jpg",
            Bio = "New bio"
        };

        var user = new ApplicationUser
        {
            Id = userId,
            DisplayName = "Old Name",
            AvatarUrl = "https://old.com/avatar.jpg"
        };

        _currentUser.Setup(x => x.GetUserId()).Returns(userId);
        _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be(command.DisplayName);
        result.Value.AvatarUrl.Should().Be(command.AvatarUrl);
        result.Value.Bio.Should().Be(command.Bio);
        
        _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var userId = "user-123";
        var command = new UpdateProfileCommand
        {
            DisplayName = "New Name"
            // AvatarUrl and Bio not provided
        };

        var user = new ApplicationUser
        {
            Id = userId,
            DisplayName = "Old Name",
            AvatarUrl = "https://old.com/avatar.jpg",
            Bio = "Old bio"
        };

        _currentUser.Setup(x => x.GetUserId()).Returns(userId);
        _userManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.DisplayName.Should().Be(command.DisplayName);
        result.Value.AvatarUrl.Should().Be(user.AvatarUrl); // Unchanged
        result.Value.Bio.Should().Be(user.Bio); // Unchanged
    }

    [Theory]
    [InlineData("X", false)]
    [InlineData("", false)]
    [InlineData("A".PadRight(101), false)]
    public void Validate_InvalidDisplayName_ReturnsValidationError(string displayName, bool isValid)
    {
        // Arrange
        var command = new UpdateProfileCommand { DisplayName = displayName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (!isValid)
            result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData("not-a-url", false)]
    [InlineData("ftp://invalid.com", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("https://example.com/avatar.jpg", true)]
    public void Validate_InvalidAvatarUrl_ReturnsValidationError(string avatarUrl, bool isValid)
    {
        // Arrange
        var command = new UpdateProfileCommand { AvatarUrl = avatarUrl };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (!isValid)
            result.ShouldHaveValidationErrorFor(x => x.AvatarUrl);
    }

    [Fact]
    public void Validate_BioTooLong_ReturnsValidationError()
    {
        // Arrange
        var command = new UpdateProfileCommand 
        { 
            Bio = new string('X', 501) 
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Bio);
    }
}
```

---

## 3. Integration Tests

### 3.1. API Integration Tests

#### Test Class: AuthApiIntegrationTests

```csharp
[Collection("AuthApi")]
public class AuthApiIntegrationTests : IClassFixture<WebApplicationFactory>
{
    private readonly WebApplicationFactory _factory;
    private readonly HttpClient _client;

    [Fact]
    public async Task Register_ValidRequest_Returns201WithTokens()
    {
        // Arrange
        var request = new
        {
            displayName = "Integration Test User",
            email = $"test-{Guid.NewGuid()}@example.com",
            userName = $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
            password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
        content.RefreshToken.Should().NotBeNullOrEmpty();
        content.User.Email.Should().Be(request.email);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange - First register
        var email = $"login-test-{Guid.NewGuid()}@example.com";
        await RegisterTestUser(email);
        
        // Act - Then login
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = email,
            password = "TestPassword123!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        content.Should().NotBeNull();
        content!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var email = $"login-test-{Guid.NewGuid()}@example.com";
        await RegisterTestUser(email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = email,
            password = "WrongPassword!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Extensions["code"].ToString().Should().Be("AUTH_INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task GetProfile_WithValidToken_Returns200()
    {
        // Arrange
        var token = await GetValidToken();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var loginResponse = await LoginTestUser();
        var refreshToken = loginResponse.RefreshToken;

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        content.Should().NotBeNull();
        content!.RefreshToken.Should().NotBe(refreshToken); // Token should be rotated
    }

    [Fact]
    public async Task RefreshToken_UsedToken_Returns401()
    {
        // Arrange - Login and refresh
        var loginResponse = await LoginTestUser();
        var refreshToken = loginResponse.RefreshToken;
        
        // First refresh
        await _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        
        // Act - Try to use the same token again (reuse attack simulation)
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new
        {
            refreshToken = refreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"].ToString().Should().Be("AUTH_TOKEN_002");
    }

    [Fact]
    public async Task Logout_ValidRequest_Returns204()
    {
        // Arrange
        var loginResponse = await LoginTestUser();
        var token = loginResponse.AccessToken;
        var refreshToken = loginResponse.RefreshToken;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new { refreshToken });
        
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify token is now invalid
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var profileResponse = await _client.SendAsync(profileRequest);
        profileResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

### 3.2. Database Integration Tests

```csharp
public class RefreshTokenRepositoryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task GetByTokenAsync_ExistingToken_ReturnsToken()
    {
        // Arrange
        var userId = "user-123";
        var token = new RefreshToken
        {
            UserId = userId,
            TokenHash = "test-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        
        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTokenAsync(token.Token);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetActiveTokensForUser_ReturnsOnlyValidTokens()
    {
        // Arrange
        var userId = "user-123";
        
        var activeToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = "active-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        
        var revokedToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = "revoked-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };
        
        var expiredToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = "expired-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        await _dbContext.RefreshTokens.AddRangeAsync(activeToken, revokedToken, expiredToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveTokensForUserAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].TokenHash.Should().Be("active-hash");
    }
}
```

---

## 4. E2E Tests (Playwright)

### 4.1. Playwright Test Setup

```typescript
// e2e/auth.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/auth/login');
  });

  test('complete registration and login flow', async ({ page }) => {
    // 1. Navigate to register
    await page.click('text=Create an account');
    await expect(page).toHaveURL('/auth/register');

    // 2. Fill registration form
    await page.fill('[name="displayName"]', 'E2E Test User');
    await page.fill('[name="email"]', `e2e-${Date.now()}@test.com`);
    await page.fill('[name="userName"]', `e2euser${Date.now()}`.substring(0, 20);
    await page.fill('[name="password"]', 'TestPass123!');
    await page.fill('[name="confirmPassword"]', 'TestPass123!');

    // 3. Submit and verify redirect
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/dashboard');

    // 4. Verify user is logged in
    await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
  });

  test('login with valid credentials', async ({ page }) => {
    // Register first
    const testEmail = `e2e-login-${Date.now()}@test.com`;
    await registerTestUser(testEmail);

    // 1. Navigate to login
    await page.goto('/auth/login');

    // 2. Fill login form
    await page.fill('[name="email"]', testEmail);
    await page.fill('[name="password"]', 'TestPass123!');

    // 3. Submit
    await page.click('button[type="submit"]');

    // 4. Verify redirect and logged in state
    await expect(page).toHaveURL('/dashboard');
    await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
  });

  test('login shows error for invalid credentials', async ({ page }) => {
    // 1. Fill invalid credentials
    await page.fill('[name="email"]', 'nonexistent@test.com');
    await page.fill('[name="password"]', 'wrongpassword');

    // 2. Submit
    await page.click('button[type="submit"]');

    // 3. Verify error message
    await expect(page.locator('[data-testid="auth-error"]')).toContainText(
      'Email or password is incorrect'
    );
  });

  test('password validation shows inline errors', async ({ page }) => {
    await page.goto('/auth/register');

    // Enter weak password
    await page.fill('[name="password"]', 'weak');

    // Trigger blur to validate
    await page.locator('[name="password"]').blur();

    // Verify validation messages
    await expect(page.locator('[data-testid="password-error"]')).toBeVisible();
  });

  test('logout clears session', async ({ page }) => {
    // Login first
    const testEmail = `e2e-logout-${Date.now()}@test.com`;
    await loginTestUser(testEmail);

    // Navigate to dashboard
    await page.goto('/dashboard');

    // Click logout
    await page.click('[data-testid="logout-button"]');

    // Verify logged out state
    await expect(page).toHaveURL('/auth/login');
    await expect(page.locator('text=Login')).toBeVisible();
  });
});
```

---

## 5. Security Tests

### 5.1. Security Test Cases

| Test ID | Description | Expected Result |
|---------|------------|-----------------|
| SEC-001 | Brute force attack - 6 failed logins | Account locked after 5 attempts |
| SEC-002 | SQL injection in email field | Sanitized, returns validation error |
| SEC-003 | XSS in display name | Sanitized, no script execution |
| SEC-004 | Token reuse attack | Detected, returns 401, alerts logged |
| SEC-005 | CSRF on logout | Protected by anti-forgery token |
| SEC-006 | JWT tampering | Rejected, returns 401 |
| SEC-007 | Rate limiting exceeded | Returns 429 with Retry-After |
| SEC-008 | Invalid JWT signature | Rejected, returns 401 |

### 5.2. Brute Force Test

```csharp
[Fact]
public async Task Login_FiveFailedAttempts_LocksAccount()
{
    // Arrange
    var email = "lockout-test@example.com";
    var correctPassword = "CorrectPass123!";
    var wrongPassword = "WrongPass123!";
    
    await RegisterTestUser(email, correctPassword);

    // Act - 5 failed login attempts
    for (int i = 0; i < 5; i++)
    {
        await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = email,
            password = wrongPassword
        });
    }

    // Assert - 6th attempt should be locked
    var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
    {
        email = email,
        password = correctPassword
    });

    response.StatusCode.Should().Be(HttpStatusCode.Locked);
    
    var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
    problem!.Extensions["code"].ToString().Should().Be("AUTH_LOGIN_002");
}
```

---

## 6. Performance Tests

### 6.1. k6 Load Test

```javascript
// k6/auth-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },   // Ramp up
    { duration: '1m', target: 20 },   // Steady state
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% under 500ms
    http_req_failed: ['rate<0.01'],     // Less than 1% failures
  },
};

export default function () {
  const BASE_URL = 'https://api.culinaryblog.com';
  
  // Test registration
  const registerRes = http.post(`${BASE_URL}/api/v1/auth/register`, 
    JSON.stringify({
      displayName: `LoadTest${__VU}`,
      email: `loadtest${__VU}-${__ITER}@test.com`,
      userName: `loadtest${__VU}${__ITER}`,
      password: 'LoadTest123!'
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );
  
  check(registerRes, {
    'register status 201': (r) => r.status === 201,
    'register has token': (r) => JSON.parse(r.body).accessToken !== undefined,
  });

  sleep(1);
}
```

---

## 7. Test Coverage Matrix

| Component | Unit Tests | Integration Tests | E2E Tests |
|-----------|-----------|-----------------|-----------|
| Register Validator | ✅ 20 | - | ✅ 3 |
| Register Handler | ✅ 15 | ✅ 5 | - |
| Login Validator | ✅ 10 | - | ✅ 2 |
| Login Handler | ✅ 12 | ✅ 8 | ✅ 4 |
| JWT Service | ✅ 10 | ✅ 3 | - |
| Refresh Token | ✅ 8 | ✅ 5 | ✅ 2 |
| Google OAuth | ✅ 8 | ✅ 4 | ✅ 2 |
| Profile Update | ✅ 12 | ✅ 4 | ✅ 3 |
| Rate Limiting | ✅ 5 | ✅ 3 | ✅ 2 |
| Security Headers | - | ✅ 5 | ✅ 2 |

---

## 8. Test Data Management

### 8.1. Test Data Fixtures

```csharp
public class AuthTestData
{
    public static ApplicationUser CreateTestUser(string id = null)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid().ToString(),
            DisplayName = "Test User",
            Email = $"test-{Guid.NewGuid()}@example.com",
            UserName = $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static RegisterCommand CreateValidRegisterCommand()
    {
        return new RegisterCommand
        {
            DisplayName = "Test User",
            Email = $"test-{Guid.NewGuid()}@example.com",
            UserName = $"testuser_{Guid.NewGuid():N}".Substring(0, 20),
            Password = "TestPass123!"
        };
    }
}
```

---

## 9. Appendix

### 9.1. Test Naming Convention

```
[MethodName]_[Scenario]_[ExpectedResult]

Examples:
- Handle_ValidCommand_ReturnsAuthResponse
- Validate_EmailInvalid_ReturnsError
- Login_WrongPassword_ReturnsUnauthorized
```

### 9.2. Test Timeout Configuration

```csharp
// xunit.runner.json
{
  "methodDisplay": "method",
  "methodBodyDisplay": "displayName",
  "maxParallelThreads": 8,
  "queue": [
    { "name": "unit-tests", "count": 8, "timeoutSeconds": 60 },
    { "name": "integration-tests", "count": 4, "timeoutSeconds": 120 },
    { "name": "e2e-tests", "count": 2, "timeoutSeconds": 300 }
  ]
}
```

---

**Document Status:** Complete  
**Last Updated:** 04/06/2026  
**Reviewed by:** ________________  
**Date:** ________________
