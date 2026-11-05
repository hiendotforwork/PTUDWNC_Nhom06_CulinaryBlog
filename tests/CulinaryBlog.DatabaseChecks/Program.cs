using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Set the test database connection locally.");
// Never run these checks against the shared Supabase database.
var cs = new NpgsqlConnectionStringBuilder(connectionString);
if (cs.Host is not ("127.0.0.1" or "localhost") || cs.Database != "culinary_lab2")
    throw new InvalidOperationException("Checks are restricted to local culinary_lab2.");
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
await using var db = new ApplicationDbContext(options);
Console.WriteLine(await OfficialRecipeSeeder.VerifyAsync(db));
var legacyRecipeIds = await db.Recipes.IgnoreQueryFilters().Where(x => x.Slug.StartsWith("lab2-chuong-"))
    .Select(x => x.Id).ToArrayAsync();
var legacyCategoryIds = await db.Categories.IgnoreQueryFilters().Where(x => x.Slug.StartsWith("lab2-chuong-"))
    .Select(x => x.Id).ToArrayAsync();
if (await db.Recipes.AnyAsync(x => x.Slug.StartsWith("lab2-chuong-"))
    || await db.Categories.AnyAsync(x => x.Slug.StartsWith("lab2-chuong-"))
    || await db.RecipeIngredients.IgnoreQueryFilters().AnyAsync(x => legacyRecipeIds.Contains(x.RecipeId) && !x.IsDeleted)
    || await db.RecipeSteps.IgnoreQueryFilters().AnyAsync(x => legacyRecipeIds.Contains(x.RecipeId) && !x.IsDeleted)
    || await db.Categories.IgnoreQueryFilters().AnyAsync(x => legacyCategoryIds.Contains(x.Id) && !x.IsDeleted))
    throw new InvalidOperationException("Legacy Lab 2 recipes, categories, or children remain active.");
var sampleId = await db.Recipes.Where(x => x.AuthorId == OfficialRecipeSeeder.SeedAuthorId)
    .Where(recipe => db.RecipeSteps.Count(step => step.RecipeId == recipe.Id) >= 2)
    .Select(x => x.Id).FirstAsync();
var secondRecipeId = await db.Recipes.Where(x => x.AuthorId == OfficialRecipeSeeder.SeedAuthorId && x.Id != sampleId)
    .Select(x => x.Id).FirstAsync();

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    var sharedOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
    await using var first = new ApplicationDbContext(sharedOptions);
    await using var tx = await first.Database.BeginTransactionAsync();
    await using var second = new ApplicationDbContext(sharedOptions);
    await second.Database.UseTransactionAsync(tx.GetDbTransaction());
    var a = await first.Recipes.SingleAsync(x => x.Id == sampleId);
    var b = await second.Recipes.SingleAsync(x => x.Id == sampleId);
    a.Description = "Kiểm tra concurrency lần ghi thứ nhất";
    await first.SaveChangesAsync();
    b.Description = "Không được ghi đè từ phiên bản cũ";
    var rejected = false;
    try { await second.SaveChangesAsync(); }
    catch (DbUpdateConcurrencyException) { rejected = true; }
    if (!rejected) throw new Exception("Concurrency did not reject stale data.");
    Console.WriteLine("PASS: stale RowVersion rejected");
    a.IsDeleted = true;
    await first.SaveChangesAsync();
    if (await first.RecipeSteps.AnyAsync(x => x.RecipeId == sampleId)) throw new Exception("Deleted parent leaked steps.");
    if (!await first.RecipeSteps.IgnoreQueryFilters().AnyAsync(x => x.RecipeId == sampleId)) throw new Exception("Soft delete lost data.");
    Console.WriteLine("PASS: deleted parent hides children without physical deletion");
    await tx.RollbackAsync();
}

async Task ExpectConstraint(string name, string sql, string state, params NpgsqlParameter[] parameters)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var tx = await connection.BeginTransactionAsync();
    var rejected = false;
    try
    {
        await using var command = new NpgsqlCommand(sql, connection, tx);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }
    catch (PostgresException ex) when (ex.SqlState == state) { rejected = true; }
    await tx.RollbackAsync();
    if (!rejected) throw new Exception("Missing constraint: " + name);
    Console.WriteLine("PASS: " + name);
}

await ExpectConstraint("negative cooking time",
    "UPDATE culinary.\"Recipes\" SET \"CookTime\"=-1 WHERE \"Id\"=@id", "23514", new NpgsqlParameter("id", sampleId));
await ExpectConstraint("quantity without unit",
    "UPDATE culinary.\"RecipeIngredients\" SET \"Quantity\"=1, \"Unit\"=NULL WHERE \"RecipeId\"=@id", "23514", new NpgsqlParameter("id", sampleId));
await ExpectConstraint("duplicate slug",
    "UPDATE culinary.\"Recipes\" SET \"Slug\"=(SELECT \"Slug\" FROM culinary.\"Recipes\" WHERE \"Id\"=@sample) WHERE \"Id\"=@other",
    "23505", new NpgsqlParameter("sample", sampleId), new NpgsqlParameter("other", secondRecipeId));
await ExpectConstraint("duplicate step number",
    "UPDATE culinary.\"RecipeSteps\" SET \"StepNumber\"=1 WHERE \"StepNumber\"=2 AND \"RecipeId\"=@id",
    "23505", new NpgsqlParameter("id", sampleId));
await ExpectConstraint("second primary image", """
    INSERT INTO culinary."RecipeImages" ("Id", "RecipeId", "OriginalUrl", "IsPrimary", "OrderIndex", "CreatedAt", "IsDeleted", "RowVersion")
    VALUES (gen_random_uuid(), @id, 'https://example.invalid/test-image', true, 1, now(), false, decode(repeat('00',16),'hex'));
    """, "23505", new NpgsqlParameter("id", sampleId));
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    var matches = (bool)(await new NpgsqlCommand("SELECT to_tsvector('culinary.vietnamese','Phở bò') @@ plainto_tsquery('culinary.vietnamese','pho bo')", connection).ExecuteScalarAsync())!;
    if (!matches) throw new Exception("Unaccent search failed.");
    var indexed = (long)(await new NpgsqlCommand("SELECT count(*) FROM culinary.\"Recipes\" WHERE \"SearchVector\" IS NOT NULL", connection).ExecuteScalarAsync())!;
    if (indexed < 100) throw new Exception("Search trigger did not index the official dataset.");
    Console.WriteLine("PASS: Vietnamese unaccent search and insert trigger");
}
Console.WriteLine("All database checks passed; test mutations rolled back.");
