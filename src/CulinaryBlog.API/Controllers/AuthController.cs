namespace CulinaryBlog.API.Controllers;

using CulinaryBlog.Application.Commands.Auth.Login;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.DTOs.Auth;
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

    public AuthController(
        IMediator mediator,
        IValidator<RegisterCommand> registerValidator,
        IValidator<LoginCommand> loginValidator)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
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
}
