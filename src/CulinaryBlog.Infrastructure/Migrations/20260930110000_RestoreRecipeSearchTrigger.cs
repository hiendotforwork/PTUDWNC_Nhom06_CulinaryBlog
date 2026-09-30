using CulinaryBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

// The new Docker baseline creates the vector/index but not the search trigger.
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260930110000_RestoreRecipeSearchTrigger")]
public sealed class RestoreRecipeSearchTrigger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        using var stream = typeof(RestoreRecipeSearchTrigger).Assembly.GetManifestResourceStream(
            "CulinaryBlog.Infrastructure.Persistence.Sql.20260922_RecipeSearchVector.sql")
            ?? throw new InvalidOperationException("Recipe search SQL resource is missing.");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS recipe_search ON culinary."Recipes";
            DROP FUNCTION IF EXISTS culinary.update_recipe_search();
            DROP TEXT SEARCH CONFIGURATION IF EXISTS culinary.vietnamese;
            """);
    }
}
