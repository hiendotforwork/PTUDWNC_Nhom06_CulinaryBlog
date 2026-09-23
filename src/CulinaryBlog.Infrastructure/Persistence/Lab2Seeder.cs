// Tệp này tạo và kiểm tra dữ liệu mẫu Lab 2 cho database Recipe.
// Chức năng: sinh khóa ổn định (Key), tạo dữ liệu (SeedAsync) và kiểm tra số lượng (VerifyAsync).

using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed record Lab2Counts(int Categories, int Recipes, int Ingredients, int Steps, int MinimumIngredients, int MinimumSteps);

// Class tiện ích tạo 20 danh mục, 100 công thức và các thành phần con theo yêu cầu Lab 2.
// Input: ApplicationDbContext. Output: dữ liệu mẫu hoặc kết quả đếm xác minh.
public static class Lab2Seeder
{
    private const string Prefix = "lab2-chuong-";
    // Chức năng: sinh Guid ổn định từ chuỗi để seed có thể chạy lặp lại.
    // Input: value. Output: Guid được băm từ prefix và value.
    private static Guid Key(string value) => new(MD5.HashData(Encoding.UTF8.GetBytes(Prefix + value)));

    // Chức năng: thêm dữ liệu mẫu nếu chưa tồn tại.
    // Input: ApplicationDbContext. Output: Task hoàn tất sau khi lưu dữ liệu.
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(2312588)");
        const string authorId = "lab2-chuong-demo-author";
        if (!await db.Users.AnyAsync(x => x.Id == authorId))
            db.Users.Add(new CulinaryBlog.Domain.Entities.ApplicationUser { Id = authorId, UserName = "lab2.chuong", NormalizedUserName = "LAB2.CHUONG", DisplayName = "Tác giả dữ liệu Lab 2", Email = "lab2.chuong@example.invalid", NormalizedEmail = "LAB2.CHUONG@EXAMPLE.INVALID", IsActive = false });
        string[] categoryNames = ["Món khai vị", "Món chính", "Món canh", "Món xào", "Món kho", "Món nướng", "Món hấp", "Món chiên", "Món luộc", "Món chay", "Món ăn sáng", "Món ăn nhẹ", "Món tráng miệng", "Bánh", "Đồ uống", "Món gỏi", "Món súp", "Món lẩu", "Món cuốn", "Món cơm"];
        var categories = await db.Categories.IgnoreQueryFilters().Where(x => x.Slug.StartsWith(Prefix)).ToDictionaryAsync(x => x.Id);
        for (var i = 0; i < categoryNames.Length; i++)
        {
            var id = Key("category-" + i);
            if (!categories.ContainsKey(id))
            {
                var category = new Category { Id = id, Name = "Lab 2 - " + categoryNames[i], Slug = Prefix + "category-" + i, OrderIndex = i, Description = "Dữ liệu giả lập phục vụ kiểm thử Lab 2." };
                db.Categories.Add(category);
                categories.Add(id, category);
            }
        }
        var existing = await db.Recipes.IgnoreQueryFilters().Where(x => x.Slug.StartsWith(Prefix)).Select(x => x.Id).ToHashSetAsync();
        string[] ingredientNames = ["Gạo", "Nấm", "Cà rốt", "Hành tím", "Tỏi", "Dầu ăn", "Muối", "Tiêu", "Nước", "Hành lá"];
        for (var i = 0; i < 100; i++)
        {
            var id = Key("recipe-" + i);
            if (existing.Contains(id)) continue;
            var random = new Random(2312588 + i);
            var recipe = new Recipe {
                Id = id, Title = $"Công thức thử nghiệm Lab 2 số {i + 1:000}", Slug = Prefix + $"recipe-{i + 1:000}",
                Description = "Dữ liệu ngẫu nhiên phục vụ kiểm thử phần mềm, không phải hướng dẫn nấu ăn đã kiểm chứng.",
                CategoryId = Key("category-" + i % 20), AuthorId = authorId,
                PrepTime = random.Next(1, 31), CookTime = random.Next(0, 91), Servings = random.Next(1, 9),
                Difficulty = (RecipeDifficulty)random.Next(1, 5), Status = (RecipeStatus)(i % 3),
                PublishedAt = i % 3 == 1 ? DateTimeOffset.UtcNow : null,
                Nutrition = new() { Calories = random.Next(100, 801), Protein = random.Next(0, 61), Carbohydrates = random.Next(0, 101), Fat = random.Next(0, 41) }
            };
            for (var j = 0; j < 10; j++)
                recipe.Ingredients.Add(new() { Id = Key($"ingredient-{i}-{j}"), Name = ingredientNames[j], Quantity = j == 6 ? null : random.Next(1, 501), Unit = j == 6 ? null : "g", OrderIndex = j, Notes = j == 6 ? "Vừa đủ" : "Dữ liệu kiểm thử" });
            for (var j = 1; j <= 5; j++)
                recipe.Steps.Add(new() { Id = Key($"step-{i}-{j}"), StepNumber = j, Title = $"Bước {j}", Description = $"Nội dung bước thử nghiệm {j} của công thức {i + 1}.", TimerMinutes = random.Next(0, 16) });
            db.Recipes.Add(recipe);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    // Chức năng: đếm và kiểm tra dữ liệu mẫu theo yêu cầu tối thiểu.
    // Input: ApplicationDbContext. Output: Lab2Counts chứa các số lượng thực tế.
    public static async Task<Lab2Counts> VerifyAsync(ApplicationDbContext db)
    {
        var recipes = db.Recipes.Where(x => x.Slug.StartsWith(Prefix));
        var counts = new Lab2Counts(
            await db.Categories.CountAsync(x => x.Slug.StartsWith(Prefix)),
            await recipes.CountAsync(),
            await db.RecipeIngredients.CountAsync(x => x.Recipe.Slug.StartsWith(Prefix)),
            await db.RecipeSteps.CountAsync(x => x.Recipe.Slug.StartsWith(Prefix)),
            await recipes.Select(x => x.Ingredients.Count(y => !y.IsDeleted)).DefaultIfEmpty().MinAsync(),
            await recipes.Select(x => x.Steps.Count(y => !y.IsDeleted)).DefaultIfEmpty().MinAsync());
        if (counts.Categories < 20 || counts.Recipes < 100 || counts.MinimumIngredients < 10 || counts.MinimumSteps < 5)
            throw new InvalidOperationException($"Lab 2 requirements not met: {counts}");
        return counts;
    }
}
