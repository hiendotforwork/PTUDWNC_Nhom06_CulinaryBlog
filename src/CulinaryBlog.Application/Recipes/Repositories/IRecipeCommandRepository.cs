// Tệp này khai báo cổng dữ liệu cho các thao tác ghi của FR-RCP.
// Nó tách khỏi IRecipeRepository của FR-SRCH để không thay đổi phần tìm kiếm của thành viên khác.

namespace CulinaryBlog.Application.Recipes.Repositories;

using CulinaryBlog.Domain.Entities;

// Interface gom các thao tác đọc phục vụ ghi và theo dõi entity của FR-RCP.
// Input: định danh hoặc entity công thức. Output: dữ liệu nghiệp vụ hoặc entity được theo dõi.
public interface IRecipeCommandRepository
{
    // Kiểm tra slug đã tồn tại. Input: slug. Output: true nếu trùng.
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    // Kiểm tra danh mục tồn tại. Input: categoryId. Output: true nếu tồn tại.
    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    // Lấy công thức phục vụ cập nhật. Input: id và tùy chọn include. Output: Recipe hoặc null.
    Task<Recipe?> GetRecipeAsync(Guid id, bool includeChildren = false, bool ignoreQueryFilters = false, CancellationToken cancellationToken = default);
    // Lấy phiên bản mới nhất. Input: id. Output: RowVersion hoặc null.
    Task<byte[]?> GetRecipeVersionAsync(Guid id, CancellationToken cancellationToken = default);
    // Kiểm tra công thức có nguyên liệu. Input: recipeId. Output: bool.
    Task<bool> HasIngredientsAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Kiểm tra công thức có bước làm. Input: recipeId. Output: bool.
    Task<bool> HasStepsAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Đưa công thức mới vào vùng theo dõi. Input: Recipe. Output: không có.
    void AddRecipe(Recipe recipe);

    // Tính thứ tự nguyên liệu tiếp theo. Input: recipeId. Output: số thứ tự.
    Task<int> GetNextIngredientOrderAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Lấy nguyên liệu thuộc công thức. Input: recipeId, ingredientId. Output: entity hoặc null.
    Task<RecipeIngredient?> GetIngredientAsync(Guid recipeId, Guid ingredientId, CancellationToken cancellationToken = default);
    // Đưa nguyên liệu mới vào vùng theo dõi. Input: RecipeIngredient. Output: không có.
    void AddIngredient(RecipeIngredient ingredient);

    // Tính số bước tiếp theo. Input: recipeId. Output: số bước.
    Task<int> GetNextStepNumberAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Lấy bước thuộc công thức. Input: recipeId, stepId. Output: entity hoặc null.
    Task<RecipeStep?> GetStepAsync(Guid recipeId, Guid stepId, CancellationToken cancellationToken = default);
    // Đưa bước mới vào vùng theo dõi. Input: RecipeStep. Output: không có.
    void AddStep(RecipeStep step);

    // Lấy danh sách metadata ảnh. Input: recipeId. Output: danh sách ảnh theo thứ tự.
    Task<IReadOnlyList<RecipeImage>> GetImagesAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Lấy metadata một ảnh. Input: recipeId, imageId. Output: entity hoặc null.
    Task<RecipeImage?> GetImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default);
    // Tính thứ tự ảnh tiếp theo. Input: recipeId. Output: số thứ tự.
    Task<int> GetNextImageOrderAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Kiểm tra đã có ảnh đại diện. Input: recipeId. Output: bool.
    Task<bool> HasPrimaryImageAsync(Guid recipeId, CancellationToken cancellationToken = default);
    // Đưa metadata ảnh mới vào vùng theo dõi. Input: RecipeImage. Output: không có.
    void AddImage(RecipeImage image);

    // Đặt RowVersion gốc để EF kiểm tra đồng thời. Input: entity, rowVersion. Output: không có.
    void SetOriginalVersion(BaseEntity entity, byte[] rowVersion);
}
