using CulinaryBlog.Infrastructure.Data;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Application.Files;
using CulinaryBlog.Application.Interfaces;
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
    || await db.Categories.IgnoreQueryFilters().AnyAsync(category => legacyCategoryIds.Contains(category.Id) && !category.IsDeleted
        && !db.Recipes.IgnoreQueryFilters().Any(recipe => !recipe.IsDeleted
            && !recipe.Slug.StartsWith("lab2-chuong-") && recipe.CategoryId == category.Id)))
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
var dataDirectory = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(dataDirectory, "recipes.json"))
    || !File.Exists(Path.Combine(dataDirectory, "images", "manifest.json")))
    dataDirectory = Directory.GetParent(dataDirectory)?.FullName
        ?? throw new InvalidOperationException("Could not locate recipes.json and images/manifest.json for rollback verification.");
var dataset = await OfficialRecipeDataLoader.LoadAsync(dataDirectory);
var previousImages = await db.RecipeImages.Where(x => x.Recipe.AuthorId == OfficialRecipeSeeder.SeedAuthorId)
    .Select(x => x.OriginalUrl).ToArrayAsync();
var storage = new RollbackCheckStorage();
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("""
        CREATE OR REPLACE FUNCTION culinary.fail_official_sync_test() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            RAISE EXCEPTION 'database-check sync failure';
            RETURN NEW;
        END $$;
        CREATE TRIGGER official_sync_failure_test BEFORE UPDATE ON culinary."RecipeImages"
        FOR EACH ROW EXECUTE FUNCTION culinary.fail_official_sync_test();
        """, connection);
    await command.ExecuteNonQueryAsync();
}
var syncFailed = false;
try
{
    await OfficialRecipeSeeder.SyncAsync(db, storage, dataset);
}
catch (DbUpdateException)
{
    syncFailed = true;
}
finally
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var command = new NpgsqlCommand("DROP TRIGGER IF EXISTS official_sync_failure_test ON culinary.\"RecipeImages\"; DROP FUNCTION IF EXISTS culinary.fail_official_sync_test();", connection);
    await command.ExecuteNonQueryAsync();
}
if (!syncFailed) throw new InvalidOperationException("Database trigger did not force the official sync to fail.");
await using (var verificationDb = new ApplicationDbContext(options))
{
    var currentImages = await verificationDb.RecipeImages.Where(x => x.Recipe.AuthorId == OfficialRecipeSeeder.SeedAuthorId)
        .Select(x => x.OriginalUrl).ToArrayAsync();
    if (!previousImages.Order().SequenceEqual(currentImages.Order()))
        throw new InvalidOperationException("Failed sync changed existing official image references.");
}
if (!storage.UploadedUrls.Order().SequenceEqual(storage.DeletedUrls.Order())
    || storage.DeletedUrls.Intersect(previousImages).Any())
    throw new InvalidOperationException("Failed sync did not clean new uploads while preserving old image references.");
Console.WriteLine("PASS: PostgreSQL sync rollback preserves rows and old images, and cleans new uploads");
Console.WriteLine("All database checks passed; test mutations rolled back.");

sealed class RollbackCheckStorage : IFileStorageService
{
    public List<string> UploadedUrls { get; } = [];
    public List<string> DeletedUrls { get; } = [];

    public async Task<StoredFile> UploadAsync(Stream stream, string contentType, string extension, string folder, CancellationToken cancellationToken)
    {
        await stream.CopyToAsync(Stream.Null, cancellationToken);
        var url = $"https://minio.local/rollback-check/{Guid.NewGuid():N}{extension}";
        UploadedUrls.Add(url);
        return new StoredFile(url);
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        DeletedUrls.Add(url);
        return Task.CompletedTask;
    }

    public bool IsOwnedUrl(string url) => url.StartsWith("https://minio.local/", StringComparison.Ordinal);
}
