// Tệp này định nghĩa các lỗi nghiệp vụ riêng của module quản lý công thức.
// Các lớp chỉ mô tả lỗi FR-RCP; middleware dùng chung sẽ ánh xạ chúng sang Problem Details.

namespace CulinaryBlog.Domain.Exceptions;

// Class gốc chứa mã lỗi chung cho exception công thức.
// Input: errorCode, message. Output: exception có ErrorCode để tầng API ánh xạ.
public abstract class RecipeDomainException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

// Lỗi dùng khi không tìm thấy công thức. Input: recipeId. Output: lỗi RECIPE_NOT_FOUND.
public sealed class RecipeNotFoundException(Guid recipeId)
    : RecipeDomainException("RECIPE_NOT_FOUND", $"Không tìm thấy công thức {recipeId}.")
{
    public Guid RecipeId { get; } = recipeId;
}

// Lỗi dùng khi người dùng không có quyền. Input: recipeId. Output: lỗi RECIPE_ACCESS_DENIED.
public sealed class RecipeAccessDeniedException(Guid recipeId)
    : RecipeDomainException("RECIPE_ACCESS_DENIED", "Bạn không có quyền thay đổi công thức này.")
{
    public Guid RecipeId { get; } = recipeId;
}

// Lỗi dùng khi trạng thái không cho phép thao tác. Input: mã và thông báo. Output: lỗi trạng thái.
public sealed class InvalidRecipeStateException(string errorCode, string message)
    : RecipeDomainException(errorCode, message);

// Lỗi dùng khi RowVersion xung đột. Input: recipeId. Output: lỗi RECIPE_CONCURRENCY_CONFLICT.
public sealed class RecipeConcurrencyException(Guid recipeId)
    : RecipeDomainException("RECIPE_CONCURRENCY_CONFLICT", "Công thức đã được thay đổi bởi yêu cầu khác.")
{
    public Guid RecipeId { get; } = recipeId;
}

// Lỗi dùng khi dữ liệu đầu vào không hợp lệ. Input: lỗi theo trường. Output: lỗi validation công thức.
public sealed class RecipeValidationException(
    IReadOnlyDictionary<string, string[]> errors,
    string message = "Dữ liệu công thức không hợp lệ.")
    : RecipeDomainException("RECIPE_VALIDATION_ERROR", message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
