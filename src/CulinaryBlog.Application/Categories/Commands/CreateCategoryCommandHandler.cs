using System.Globalization;
using System.Text;
using CulinaryBlog.Application.Categories.DTOs;
using CulinaryBlog.Application.Categories.Queries;
using CulinaryBlog.Application.Categories.Repositories;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Categories.Commands;

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categories,
    IMemoryCache cache)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await categories.NameExistsAsync(name, cancellationToken: cancellationToken))
            throw new CategoryConflictException("CATEGORY_NAME_EXISTS", "Tên danh mục đã tồn tại.");

        var slugBase = Slugify(name);
        var slug = slugBase;
        var suffix = 2;
        while (await categories.SlugExistsAsync(slug, cancellationToken))
            slug = $"{slugBase}-{suffix++}";

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        categories.Add(category);
        await categories.SaveChangesAsync(cancellationToken);
        cache.Remove(CategoryCache.Key);

        return new CategoryDto(category.Id, category.Name, category.Slug, category.Description, 0);
    }

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        var separatorPending = false;

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            var normalizedCharacter = character switch
            {
                '\u0111' => 'd',
                '\u0110' => 'D',
                _ => character
            };

            if (char.IsLetterOrDigit(normalizedCharacter))
            {
                if (separatorPending && result.Length > 0)
                    result.Append('-');
                result.Append(char.ToLowerInvariant(normalizedCharacter));
                separatorPending = false;
            }
            else
            {
                separatorPending = true;
            }
        }

        return result.Length == 0 ? "category" : result.ToString();
    }
}
