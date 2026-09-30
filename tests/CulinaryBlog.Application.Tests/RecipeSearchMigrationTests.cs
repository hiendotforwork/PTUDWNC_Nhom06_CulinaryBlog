using CulinaryBlog.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace CulinaryBlog.Application.Tests;

public sealed class RecipeSearchMigrationTests
{
    [Fact]
    public void DockerSearchMigrationLoadsBundledSql()
    {
        var operation = Assert.IsType<SqlOperation>(Assert.Single(new RestoreRecipeSearchTrigger().UpOperations));
        Assert.Contains("CREATE TRIGGER recipe_search", operation.Sql);
        Assert.Contains("ON culinary.\"Recipes\"", operation.Sql);
        Assert.Contains("culinary.vietnamese", operation.Sql);
    }
}