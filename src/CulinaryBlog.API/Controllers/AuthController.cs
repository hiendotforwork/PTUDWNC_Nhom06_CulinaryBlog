namespace CulinaryBlog.API.Controllers;

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
    private readonly IValidator<RegisterCommand> _validator;

    public AuthController(IMediator mediator, IValidator<RegisterCommand> validator)
    {
        _mediator = mediator;
        _validator = validator;
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

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
