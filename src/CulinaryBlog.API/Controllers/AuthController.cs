namespace CulinaryBlog.API.Controllers;

using CulinaryBlog.Application.Commands.Auth.Login;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Commands.Auth.GoogleLogin;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
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
    private readonly IConfiguration _configuration;

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [EnableRateLimiting("RegisterRateLimit")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
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

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
            {
                return Redirect($"{frontendBase}/login?error={Uri.EscapeDataString("Không thể lấy thông tin từ Google.")}");
            }

            var command = new GoogleLoginCommand(
                Provider: "Google",
                ProviderKey: providerKey,
                Email: email,
                DisplayName: displayName,
                AvatarUrl: avatarUrl
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
}
