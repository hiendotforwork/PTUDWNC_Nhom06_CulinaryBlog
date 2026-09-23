using CulinaryBlog.Application.Common.Models;
using CulinaryBlog.Application.Recipes.Queries;
using CulinaryBlog.Application.Recipes.Validation;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class SearchRecipesQueryValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    public void RejectsMissingOrShortQuery(string? query)
    {
        var errors = SearchRecipesQueryValidator.Validate(query, 1, 12);

        Assert.True(errors.ContainsKey("q"));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public void RejectsInvalidPagination(int page, int pageSize)
    {
        var errors = SearchRecipesQueryValidator.Validate("pho", page, pageSize);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AcceptsValidSearchRequest()
    {
        var errors = SearchRecipesQueryValidator.Validate("pho bo", 2, 10);

        Assert.Empty(errors);
    }
}

public sealed class PagedResultTests
{
    [Fact]
    public void CalculatesPaginationMetadata()
    {
        var result = PagedResult<string>.Create(["one"], 25, 2, 10);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }
}

public sealed class GetRecipesQueryValidatorTests
{
    [Theory]
    [InlineData("createdAt")]
    [InlineData("-createdAt")]
    [InlineData("title")]
    [InlineData("-title")]
    [InlineData("cookTime")]
    [InlineData("-cookTime")]
    public void AcceptsSupportedSort(string sort)
    {
        var errors = GetRecipesQueryValidator.Validate(new GetRecipesQuery(Sort: sort));

        Assert.DoesNotContain("sort", errors.Keys);
    }

    [Fact]
    public void RejectsUnsupportedSort()
    {
        var errors = GetRecipesQueryValidator.Validate(new GetRecipesQuery(Sort: "views"));

        Assert.True(errors.ContainsKey("sort"));
    }
}