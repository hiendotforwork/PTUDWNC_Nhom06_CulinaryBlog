using System.Security.Claims;
using CulinaryBlog.Application.Categories.Commands;
using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Queries;
using CulinaryBlog.Domain.Constants;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(
    ISender sender,
    IValidator<CreateCategoryCommand> createCategoryValidator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CategoryDetailDto>> GetBySlug(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return UnprocessableEntity(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [page < 1 ? "page" : "pageSize"] = [page < 1
                    ? "page phải lớn hơn hoặc bằng 1."
                    : "pageSize phải nằm trong khoảng từ 1 đến 100."]
            }));

        var result = await sender.Send(
            new GetCategoryBySlugQuery(
                slug,
                page,
                pageSize,
                User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand(request.Name, request.Description);
        await createCategoryValidator.ValidateAndThrowAsync(command, cancellationToken);
        var category = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, category);
    }
}

public sealed record CreateCategoryRequest(string Name, string? Description);
