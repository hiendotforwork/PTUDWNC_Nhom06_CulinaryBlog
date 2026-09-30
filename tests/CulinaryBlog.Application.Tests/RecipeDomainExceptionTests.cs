// Tệp này kiểm thử các exception nghiệp vụ thuộc FR-RCP.
// Chức năng: bảo đảm mã lỗi và dữ liệu ngữ cảnh được giữ đúng để middleware có thể ánh xạ.

namespace CulinaryBlog.Application.Tests;

using CulinaryBlog.Domain.Exceptions;
using FluentAssertions;
using Xunit;

public sealed class RecipeDomainExceptionTests
{
    [Fact]
    // Chức năng: kiểm tra lỗi không tìm thấy công thức.
    // Input: recipeId. Output: mã RECIPE_NOT_FOUND và đúng recipeId.
    public void RecipeNotFoundException_PreservesErrorContext()
    {
        var recipeId = Guid.NewGuid();

        var exception = new RecipeNotFoundException(recipeId);

        exception.ErrorCode.Should().Be("RECIPE_NOT_FOUND");
        exception.RecipeId.Should().Be(recipeId);
    }

    [Fact]
    // Chức năng: kiểm tra lỗi validation lưu danh sách lỗi theo trường.
    // Input: dictionary lỗi. Output: mã lỗi và dictionary ban đầu.
    public void RecipeValidationException_PreservesFieldErrors()
    {
        IReadOnlyDictionary<string, string[]> errors = new Dictionary<string, string[]>
        {
            ["title"] = ["Tiêu đề không hợp lệ."]
        };

        var exception = new RecipeValidationException(errors);

        exception.ErrorCode.Should().Be("RECIPE_VALIDATION_ERROR");
        exception.Errors.Should().BeSameAs(errors);
    }
}
