// Tệp này cài đặt repository cho các thao tác ghi FR-RCP bằng ApplicationDbContext.

namespace CulinaryBlog.Infrastructure.Recipes;

using CulinaryBlog.Application.Recipes.Repositories;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

// Class hiện thực truy cập dữ liệu ghi cho FR-RCP bằng EF Core.
// Input: ApplicationDbContext. Output: entity/dữ liệu cho controller và thay đổi được theo dõi.
public sealed class RecipeCommandRepository(ApplicationDbContext db) : IRecipeCommandRepository
{
    // Kiểm tra slug trùng, kể cả bản ghi đã xóa mềm. Input: slug. Output: bool.
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        db.Recipes.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug, ct);

    // Kiểm tra danh mục còn hiệu lực. Input: categoryId. Output: bool.
    public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct = default) =>
        db.Categories.AnyAsync(x => x.Id == categoryId, ct);

    // Lấy công thức và tùy chọn tải thành phần con. Input: id/cờ tải. Output: Recipe hoặc null.
    public async Task<Recipe?> GetRecipeAsync(Guid id, bool includeChildren = false, bool ignoreQueryFilters = false, CancellationToken ct = default)
    {
        IQueryable<Recipe> query = ignoreQueryFilters ? db.Recipes.IgnoreQueryFilters() : db.Recipes;
        if (includeChildren) query = query.Include(x => x.Ingredients).Include(x => x.Steps).Include(x => x.Images);
        return await query.SingleOrDefaultAsync(x => x.Id == id && (!ignoreQueryFilters || !x.IsDeleted), ct);
    }

    // Lấy RowVersion hiện tại. Input: id. Output: byte[] hoặc null.
    public Task<byte[]?> GetRecipeVersionAsync(Guid id, CancellationToken ct = default) =>
        db.Recipes.AsNoTracking().Where(x => x.Id == id).Select(x => x.RowVersion).SingleOrDefaultAsync(ct);

    // Kiểm tra nguyên liệu tồn tại. Input: recipeId. Output: bool.
    public Task<bool> HasIngredientsAsync(Guid recipeId, CancellationToken ct = default) => db.RecipeIngredients.AnyAsync(x => x.RecipeId == recipeId, ct);
    // Kiểm tra bước làm tồn tại. Input: recipeId. Output: bool.
    public Task<bool> HasStepsAsync(Guid recipeId, CancellationToken ct = default) => db.RecipeSteps.AnyAsync(x => x.RecipeId == recipeId, ct);
    // Theo dõi công thức mới. Input: Recipe. Output: không có.
    public void AddRecipe(Recipe recipe) => db.Recipes.Add(recipe);

    // Tính thứ tự nguyên liệu kế tiếp. Input: recipeId. Output: int.
    public async Task<int> GetNextIngredientOrderAsync(Guid recipeId, CancellationToken ct = default) =>
        (await db.RecipeIngredients.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.OrderIndex).MaxAsync(ct) ?? -1) + 1;
    // Lấy nguyên liệu theo công thức. Input: recipeId/ingredientId. Output: entity hoặc null.
    public Task<RecipeIngredient?> GetIngredientAsync(Guid recipeId, Guid ingredientId, CancellationToken ct = default) =>
        db.RecipeIngredients.SingleOrDefaultAsync(x => x.Id == ingredientId && x.RecipeId == recipeId, ct);
    // Theo dõi nguyên liệu mới. Input: RecipeIngredient. Output: không có.
    public void AddIngredient(RecipeIngredient ingredient) => db.RecipeIngredients.Add(ingredient);

    // Tính số bước kế tiếp. Input: recipeId. Output: int.
    public async Task<int> GetNextStepNumberAsync(Guid recipeId, CancellationToken ct = default) =>
        (await db.RecipeSteps.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.StepNumber).MaxAsync(ct) ?? 0) + 1;
    // Lấy bước theo công thức. Input: recipeId/stepId. Output: entity hoặc null.
    public Task<RecipeStep?> GetStepAsync(Guid recipeId, Guid stepId, CancellationToken ct = default) =>
        db.RecipeSteps.SingleOrDefaultAsync(x => x.Id == stepId && x.RecipeId == recipeId, ct);
    // Theo dõi bước mới. Input: RecipeStep. Output: không có.
    public void AddStep(RecipeStep step) => db.RecipeSteps.Add(step);

    // Lấy metadata ảnh theo thứ tự. Input: recipeId. Output: danh sách ảnh.
    public async Task<IReadOnlyList<RecipeImage>> GetImagesAsync(Guid recipeId, CancellationToken ct = default) =>
        await db.RecipeImages.Where(x => x.RecipeId == recipeId).OrderBy(x => x.OrderIndex).ToListAsync(ct);
    // Lấy metadata một ảnh. Input: recipeId/imageId. Output: entity hoặc null.
    public Task<RecipeImage?> GetImageAsync(Guid recipeId, Guid imageId, CancellationToken ct = default) =>
        db.RecipeImages.SingleOrDefaultAsync(x => x.Id == imageId && x.RecipeId == recipeId, ct);
    // Tính thứ tự ảnh kế tiếp. Input: recipeId. Output: int.
    public async Task<int> GetNextImageOrderAsync(Guid recipeId, CancellationToken ct = default) =>
        (await db.RecipeImages.Where(x => x.RecipeId == recipeId).Select(x => (int?)x.OrderIndex).MaxAsync(ct) ?? -1) + 1;
    // Kiểm tra ảnh đại diện. Input: recipeId. Output: bool.
    public Task<bool> HasPrimaryImageAsync(Guid recipeId, CancellationToken ct = default) =>
        db.RecipeImages.AnyAsync(x => x.RecipeId == recipeId && x.IsPrimary, ct);
    // Theo dõi metadata ảnh mới. Input: RecipeImage. Output: không có.
    public void AddImage(RecipeImage image) => db.RecipeImages.Add(image);

    // Gán RowVersion gốc cho kiểm soát đồng thời. Input: entity/rowVersion. Output: không có.
    public void SetOriginalVersion(BaseEntity entity, byte[] rowVersion) => db.Entry(entity).Property(x => x.RowVersion).OriginalValue = rowVersion;
}
