using System.Text;
using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(CulinaryBlogDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Categories.AnyAsync() || await context.Recipes.AnyAsync())
        {
            return;
        }

        var categoryNames = new[]
        {
            "Món Việt", "Món Hàn", "Món Nhật", "Món Trung", "Món Thái",
            "Món Âu", "Món Chay", "Bánh", "Lẩu", "BBQ",
            "Hải sản", "Salad", "Ăn sáng", "Ăn vặt", "Món cay",
            "Soup", "Nước uống", "Healthy", "Fast Food", "Tráng miệng"
        };

        var categories = categoryNames
            .Select((name, index) => new Category
            {
                Name = name,
                Slug = ToSlug(name) + (index == 0 ? string.Empty : $"-{index + 1}"),
                Description = $"Các món thuộc nhóm {name}.",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        var authors = new[]
        {
            new ApplicationUser { FullName = "Nguyễn Văn A", UserName = "chef_a", Email = "chef.a@example.com", IsEmailVerified = true, AvatarUrl = "https://example.com/avatars/a.png" },
            new ApplicationUser { FullName = "Trần Thị B", UserName = "chef_b", Email = "chef.b@example.com", IsEmailVerified = true, AvatarUrl = "https://example.com/avatars/b.png" },
            new ApplicationUser { FullName = "Lê Hoàng C", UserName = "chef_c", Email = "chef.c@example.com", IsEmailVerified = true, AvatarUrl = "https://example.com/avatars/c.png" },
            new ApplicationUser { FullName = "Phạm Minh D", UserName = "chef_d", Email = "chef.d@example.com", IsEmailVerified = true, AvatarUrl = "https://example.com/avatars/d.png" },
            new ApplicationUser { FullName = "Hoàng Thảo E", UserName = "chef_e", Email = "chef.e@example.com", IsEmailVerified = true, AvatarUrl = "https://example.com/avatars/e.png" }
        };

        await context.ApplicationUsers.AddRangeAsync(authors);
        await context.SaveChangesAsync();

        var ingredientPool = new[]
        {
            "Gạo", "Mì", "Thịt bò", "Thịt gà", "Tôm", "Cá", "Trứng", "Tỏi", "Hành tím", "Hành lá",
            "Ớt", "Gừng", "Dầu ăn", "Nước mắm", "Đường", "Muối", "Bột ngọt", "Bột canh", "Bạc hà", "Rau thơm",
            "Bắp cải", "Cà rốt", "Cà chua", "Bí ngòi", "Măng", "Nấm", "Đậu phụ", "Bánh mì", "Sữa tươi", "Phô mai",
            "Hành tây", "Củ cải", "Khoai tây", "Ớt chuông", "Ngò gai", "Nấm hương", "Sốt mayo", "Sốt cà chua", "Dưa leo", "Bắp non"
        };

        var recipeBaseNames = new[]
        {
            "Phở bò", "Bún chả", "Cơm tấm", "Bánh mì kẹp", "Gà rán", "Salad rau củ", "Mì xào hải sản",
            "Súp cua", "Bánh xèo", "Bánh canh", "Lẩu thái", "Mì ramen", "Bò nướng", "Gà nướng mật ong",
            "Cá kho tiêu", "Bún riêu", "Cháo gà", "Nộm hoa chuối", "Gỏi cuốn", "Mì Ý sốt bò", "Bún đậu mắm tôm",
            "Bánh tráng trộn", "Mì udon", "Canh chua", "Cá sốt chua ngọt", "Bò kho", "Đậu hũ sốt cà", "Cà ri gà",
            "Bò bít tết", "Chả giò", "Rau muống xào", "Nem nướng", "Món chay đặc biệt", "Sườn xào chua ngọt",
            "Kimbap", "Bánh flan", "Dưa hấu tẩm", "Mì cay Hàn", "Bánh gạo", "Bia chua", "Sữa chua đá", "Mì spagheti",
            "Bánh cuốn", "Món lẩu nấm", "Bò lúc lắc", "Sườn nướng BBQ", "Ốc xào tỏi", "Rau xào tỏi", "Bánh tôm",
            "Cá hồi nướng", "Món soup hải sản", "Tôm hùm xào", "Bún bò Huế", "Cà ri tôm", "Tom yum", "Món nướng kiểu Nhật",
            "Món hầm rau", "Đậu hũ xào", "Canh bí đỏ", "Mì cay xào", "Súp yến", "Gà hấp", "Nui hải sản", "Lợn rừng nướng",
            "Bánh cuốn nhân thịt", "Cơm chiên hải sản", "Gà kho gừng", "Thịt kho tàu", "Bún cá", "Bánh bao", "Bánh kem",
            "Mì vịt", "Tôm rang bơ", "Cá chiên giòn", "Sườn rim mắm", "Món ăn vặt đặc sản", "Món ăn sáng gà", "Bánh trứng nướng",
            "Tàu hũ chiên", "Món canh măng", "Rau câu", "Nước ép cam", "Món ngon gia đình", "Món nấu chậm", "Món ăn lành mạnh"
        };

        var stepTemplates = new[]
        {
            "Chuẩn bị nguyên liệu sạch sẽ và cắt nhỏ theo khẩu phần.",
            "Ướp gia vị với muối, đường, dầu ăn và các loại thảo mộc cần thiết.",
            "Phi thơm tỏi và hành cùng với dầu nóng để tạo hương thơm nền.",
            "Cho nguyên liệu chính vào xào hoặc nấu cho chín đều và thấm gia vị.",
            "Điều chỉnh vị và thưởng thức khi món đã chín tới độ ngon nhất."
        };

        var recipes = new List<Recipe>();

        for (var i = 1; i <= 100; i++)
        {
            var category = categories[(i - 1) % categories.Count];
            var author = authors[(i - 1) % authors.Length];
            var title = recipeBaseNames[(i - 1) % recipeBaseNames.Length];
            var recipe = new Recipe
            {
                Title = i == 1 ? title : $"{title} {i}",
                Slug = ToSlug(i == 1 ? title : $"{title} {i}"),
                Summary = $"Món {title.ToLower()} thơm ngon, dễ làm và phù hợp cho gia đình.",
                Content = $"Món {title.ToLower()} là sự kết hợp giữa nguyên liệu tươi ngon, gia vị đậm đà và cách chế biến đơn giản. Đây là món phù hợp cho bữa cơm gia đình, tiệc nhỏ hoặc ăn sáng.",
                FeaturedImageUrl = $"https://images.example.com/recipes/{i}.jpg",
                PrepTimeMinutes = 10 + (i % 15),
                CookTimeMinutes = 20 + (i % 35),
                Servings = 2 + (i % 4),
                IsPublished = true,
                IsArchived = false,
                CategoryId = category.Id,
                AuthorId = author.Id,
                CreatedAt = DateTime.UtcNow,
                Ingredients = new List<RecipeIngredient>(),
                Steps = new List<RecipeStep>(),
                Images = new List<RecipeImage>()
            };

            for (var j = 0; j < 10; j++)
            {
                var ingredientName = ingredientPool[(i * 3 + j) % ingredientPool.Length];
                recipe.Ingredients.Add(new RecipeIngredient
                {
                    Name = ingredientName,
                    Quantity = (1 + ((i + j) % 5)).ToString(),
                    Unit = j % 3 == 0 ? "g" : j % 3 == 1 ? "ml" : "cái",
                    Notes = j % 2 == 0 ? "Rửa sạch" : "Cắt nhỏ",
                    CreatedAt = DateTime.UtcNow
                });
            }

            for (var stepIndex = 0; stepIndex < 5; stepIndex++)
            {
                recipe.Steps.Add(new RecipeStep
                {
                    StepNumber = stepIndex + 1,
                    Description = stepTemplates[stepIndex % stepTemplates.Length] + $" Bước thực hiện số {stepIndex + 1} cho món {title}.",
                    ImageUrl = $"https://images.example.com/steps/{i}-{stepIndex + 1}.jpg",
                    CreatedAt = DateTime.UtcNow
                });
            }

            recipe.Images.Add(new RecipeImage
            {
                ImageUrl = recipe.FeaturedImageUrl,
                IsPrimary = true,
                AltText = recipe.Title,
                CreatedAt = DateTime.UtcNow
            });

            recipes.Add(recipe);
        }

        await context.Recipes.AddRangeAsync(recipes);
        await context.SaveChangesAsync();
    }

    private static string ToSlug(string input)
    {
        var normalized = input.Trim();
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
            {
                builder.Append('-');
            }
        }

        var result = builder.ToString();
        while (result.Contains("--"))
        {
            result = result.Replace("--", "-");
        }

        return result.Trim('-');
    }
}
