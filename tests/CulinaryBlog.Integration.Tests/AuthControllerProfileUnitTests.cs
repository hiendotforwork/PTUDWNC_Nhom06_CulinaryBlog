namespace CulinaryBlog.Integration.Tests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CulinaryBlog.API.Controllers;
using CulinaryBlog.Application.Commands.Auth.Login;
using CulinaryBlog.Application.Commands.Auth.RefreshToken;
using CulinaryBlog.Application.Commands.Auth.Register;
using CulinaryBlog.Application.Commands.Auth.UpdateProfile;
using CulinaryBlog.Application.DTOs.Auth;
using CulinaryBlog.Application.Queries.Auth;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class AuthControllerProfileUnitTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IValidator<RegisterCommand>> _registerValidatorMock = new();
    private readonly Mock<IValidator<LoginCommand>> _loginValidatorMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly AuthController _controller;

    public AuthControllerProfileUnitTests()
    {
        _controller = new AuthController(
            _mediatorMock.Object,
            _registerValidatorMock.Object,
            _loginValidatorMock.Object,
            _configMock.Object
        );
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task GetCurrentUser_WithoutUsableUserIdClaims_Returns401WithoutCallingMediator(string? nameIdentifier, string? sub)
    {
        var claims = new List<Claim>();
        if (nameIdentifier != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }
        if (sub != null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentUser_WithWhitespaceNameIdentifierAndValidSub_DispatchesQueryWithSub()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "   "),
            new(JwtRegisteredClaimNames.Sub, "user-sub-123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var expectedDto = new UserProfileDto(
            "user-sub-123",
            "Sub User",
            "sub@example.com",
            "subuser",
            null,
            null,
            new[] { "Author" },
            true,
            DateTime.UtcNow
        );

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetCurrentUserQuery>(q => q.UserId == "user-sub-123"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<GetCurrentUserQuery>(q => q.UserId == "user-sub-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task PatchMeWithoutUsableUserIdReturnsUnauthorizedWithoutSendingCommand(string? nameIdentifier, string? sub)
    {
        var claims = new List<Claim>();
        if (nameIdentifier != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }
        if (sub != null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var result = await _controller.UpdateProfile(new UpdateProfileRequest { DisplayName = "New Name" }, CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateProfileCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchMeUsesClaimUserIdAndReturnsUpdatedProfile()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-claim-123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var expectedDto = new UserProfileDto(
            "user-claim-123",
            "Updated Name",
            "user@example.com",
            "testuser",
            null,
            "Updated Bio",
            new[] { "Author" },
            true,
            DateTime.UtcNow
        );

        _mediatorMock
            .Setup(m => m.Send(It.Is<UpdateProfileCommand>(c => c.UserId == "user-claim-123" && c.DisplayName == "Updated Name" && c.Bio == "Updated Bio"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var request = new UpdateProfileRequest
        {
            DisplayName = "Updated Name",
            Bio = "Updated Bio"
        };

        var result = await _controller.UpdateProfile(request, CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(expectedDto);
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateProfileCommand>(c => c.UserId == "user-claim-123" && c.DisplayName == "Updated Name" && c.Bio == "Updated Bio"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
