namespace CulinaryBlog.API.Controllers;

using CulinaryBlog.Application.Commands.Auth.Login;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.Commands.Auth.Logout;
using CulinaryBlog.Application.Commands.Auth.RefreshToken;
using CulinaryBlog.Application.Commands.Auth.ConfirmEmail;
using CulinaryBlog.Application.Commands.Auth.ResendEmailConfirmation;
using CulinaryBlog.Application.Commands.Auth.ChangePassword;
using CulinaryBlog.Application.Commands.Auth.ForgotPassword;
using CulinaryBlog.Application.Commands.Auth.ResetPassword;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Commands.Auth.GoogleLogin;
using CulinaryBlog.Application.Commands.Auth.UpdateProfile;
using CulinaryBlog.Application.Queries.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterCommand> _registerValidator;
    private readonly IValidator<LoginCommand> _loginValidator;
    private readonly IValidator<RefreshTokenCommand>? _refreshValidator;
    private readonly IValidator<UpdateProfileCommand>? _updateProfileValidator;
    private readonly IValidator<ConfirmEmailCommand>? _confirmEmailValidator;
    private readonly IValidator<ResendEmailConfirmationCommand>? _resendEmailValidator;
    private readonly IValidator<ChangePasswordCommand>? _changePasswordValidator;
    private readonly IValidator<ForgotPasswordCommand>? _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordCommand>? _resetPasswordValidator;
    private readonly IConfiguration _configuration;

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator,
        IConfiguration configuration,
        IValidator<RefreshTokenCommand>? refreshValidator = null,
        IValidator<UpdateProfileCommand>? updateProfileValidator = null,
        IValidator<ConfirmEmailCommand>? confirmEmailValidator = null,
        IValidator<ResendEmailConfirmationCommand>? resendEmailValidator = null,
        IValidator<ChangePasswordCommand>? changePasswordValidator = null,
        IValidator<ForgotPasswordCommand>? forgotPasswordValidator = null,
        IValidator<ResetPasswordCommand>? resetPasswordValidator = null)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _configuration = configuration;
        _refreshValidator = refreshValidator;
        _updateProfileValidator = updateProfileValidator;
        _confirmEmailValidator = confirmEmailValidator;
        _resendEmailValidator = resendEmailValidator;
        _changePasswordValidator = changePasswordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    [HttpPost("register")]
    [EnableRateLimiting("RegisterRateLimit")]
    [ProducesResponseType(typeof(RegistrationPendingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.UserName,
            request.DisplayName,
            request.Password
        );

        var validationResult = await _registerValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginRateLimit")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var validationResult = await _loginValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("RefreshRateLimit")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        if (_refreshValidator is not null)
        {
            var validationResult = await _refreshValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("email/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);
        if (_confirmEmailValidator is not null)
        {
            var validationResult = await _confirmEmailValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("email/status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetEmailConfirmationStatus([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);
        if (_confirmEmailValidator is not null)
        {
            var validationResult = await _confirmEmailValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        var confirmed = await _mediator.Send(new GetEmailConfirmationStatusQuery(request.UserId, request.Token), cancellationToken);
        return Ok(new { confirmed });
    }

    [HttpPost("email/resend")]
    [EnableRateLimiting("ResendEmailRateLimit")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequest request, CancellationToken cancellationToken)
    {
        var command = new ResendEmailConfirmationCommand(request.Email);
        if (_resendEmailValidator is not null)
        {
            var validationResult = await _resendEmailValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        await _mediator.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpGet("google-signin")]
    public IActionResult GoogleSignIn([FromQuery] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, "Google");
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded)
            {
                authenticateResult = await HttpContext.AuthenticateAsync("Google");
            }

            var frontendBase = _configuration["FrontendUrl"] ?? "http://localhost:3000";

            if (!authenticateResult.Succeeded)
            {
                var error = authenticateResult.Failure?.Message ?? "Google authentication failed";
                return Redirect($"{frontendBase}/login?error={Uri.EscapeDataString(error)}");
            }

            var claims = authenticateResult.Principal?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var displayName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                              ?? claims?.FirstOrDefault(c => c.Type == "name")?.Value
                              ?? "Google User";
            var avatarUrl = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;
            var providerKey = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            var emailVerifiedClaim = claims?.FirstOrDefault(c => c.Type == "email_verified")?.Value;
            var emailVerified = string.Equals(emailVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey) || !emailVerified)
            {
                return Redirect($"{frontendBase}/login?error={Uri.EscapeDataString("Không thể lấy thông tin xác thực từ Google hoặc email chưa được xác thực.")}");
            }

            var command = new GoogleLoginCommand(
                Provider: "Google",
                ProviderKey: providerKey,
                Email: email,
                DisplayName: displayName,
                AvatarUrl: avatarUrl,
                EmailVerified: emailVerified
            );

            var authResponse = await _mediator.Send(command, cancellationToken);

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var redirectUrl = $"{frontendBase}/auth/google-callback#" +
                $"accessToken={Uri.EscapeDataString(authResponse.AccessToken)}" +
                $"&refreshToken={Uri.EscapeDataString(authResponse.RefreshToken)}" +
                $"&expiresAt={Uri.EscapeDataString(authResponse.ExpiresAt.ToString("O"))}" +
                $"&userId={Uri.EscapeDataString(authResponse.User.Id)}" +
                $"&displayName={Uri.EscapeDataString(authResponse.User.DisplayName)}" +
                $"&email={Uri.EscapeDataString(authResponse.User.Email)}" +
                $"&avatarUrl={Uri.EscapeDataString(authResponse.User.AvatarUrl ?? "")}" +
                $"&roles={Uri.EscapeDataString(string.Join(",", authResponse.User.Roles))}";

            if (!string.IsNullOrEmpty(returnUrl))
            {
                redirectUrl += $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            return Redirect(redirectUrl);
        }
        catch (Exception)
        {
            var frontendBase = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            return Redirect($"{frontendBase}/login?error={Uri.EscapeDataString("Đăng nhập Google thất bại. Vui lòng thử lại.")}");
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new LogoutCommand(request?.RefreshToken), cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirst("sub")?.Value;

        var userId = !string.IsNullOrWhiteSpace(nameIdentifier) ? nameIdentifier.Trim()
            : !string.IsNullOrWhiteSpace(sub) ? sub.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var profile = await _mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirst("sub")?.Value;

        var userId = !string.IsNullOrWhiteSpace(nameIdentifier) ? nameIdentifier.Trim()
            : !string.IsNullOrWhiteSpace(sub) ? sub.Trim()
            : null;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var command = new UpdateProfileCommand(
            userId,
            request.DisplayName,
            request.Bio
        );

        if (_updateProfileValidator != null)
        {
            var validationResult = await _updateProfileValidator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        var profile = await _mediator.Send(command, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("password/change")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var nameIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirst("sub")?.Value;
        var userId = !string.IsNullOrWhiteSpace(nameIdentifier) ? nameIdentifier.Trim()
            : !string.IsNullOrWhiteSpace(sub) ? sub.Trim()
            : null;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
        if (_changePasswordValidator is not null)
        {
            var validation = await _changePasswordValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);
        }

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetRateLimit")]
    [ProducesResponseType(typeof(PasswordResetRequestAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        if (_forgotPasswordValidator is not null)
        {
            var validation = await _forgotPasswordValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);
        }

        await _mediator.Send(command, cancellationToken);
        return Accepted(new PasswordResetRequestAcceptedResponse(
            "Nếu email này thuộc tài khoản hợp lệ, hướng dẫn đặt lại mật khẩu sẽ được gửi."));
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword);
        if (_resetPasswordValidator is not null)
        {
            var validation = await _resetPasswordValidator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid) throw new ValidationException(validation.Errors);
        }

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
