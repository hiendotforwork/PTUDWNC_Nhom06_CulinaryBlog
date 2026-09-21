using Microsoft.EntityFrameworkCore.Storage;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Set the test database connection locally.");
// Never run these checks against the shared Supabase database.
var cs = new NpgsqlConnectionStringBuilder(connectionString);
if (cs.Host is not ("127.0.0.1" or "localhost") || cs.Database != "culinary_lab2")
    throw new InvalidOperationException("Checks are restricted to local culinary_lab2.");
var options = new DbContextOptionsBuilder<CulinaryBlogDbContext>().UseNpgsql(connectionString).Options;
await using var db = new CulinaryBlogDbContext(options);
Console.WriteLine(await Lab2Seeder.VerifyAsync(db));
var sampleId = await db.Recipes.Where(x => x.Slug.StartsWith("lab2-chuong-")).Select(x => x.Id).FirstAsync();

await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    var sharedOptions = new DbContextOptionsBuilder<CulinaryBlogDbContext>().UseNpgsql(connection).Options;
    await using var first = new CulinaryBlogDbContext(sharedOptions);
    await using var tx = await first.Database.BeginTransactionAsync();
    await using var second = new CulinaryBlogDbContext(sharedOptions);
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

async Task ExpectConstraint(string name, string sql, string state)
{
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var tx = await connection.BeginTransactionAsync();
    var rejected = false;
    try { await new NpgsqlCommand(sql, connection, tx).ExecuteNonQueryAsync(); }
    catch (PostgresException ex) when (ex.SqlState == state) { rejected = true; }
    await tx.RollbackAsync();
    if (!rejected) throw new Exception("Missing constraint: " + name);
    Console.WriteLine("PASS: " + name);
}

await ExpectConstraint("negative cooking time", "UPDATE culinary.\"Recipes\" SET \"CookTime\"=-1 WHERE \"Slug\"='lab2-chuong-recipe-001'", "23514");
await ExpectConstraint("quantity without unit", "UPDATE culinary.\"RecipeIngredients\" SET \"Quantity\"=1, \"Unit\"=NULL WHERE \"RecipeId\"=(SELECT \"Id\" FROM culinary.\"Recipes\" WHERE \"Slug\"='lab2-chuong-recipe-001')", "23514");
await ExpectConstraint("duplicate slug", "UPDATE culinary.\"Recipes\" SET \"Slug\"='lab2-chuong-recipe-001' WHERE \"Slug\"='lab2-chuong-recipe-002'", "23505");
await ExpectConstraint("duplicate step number", "UPDATE culinary.\"RecipeSteps\" SET \"StepNumber\"=1 WHERE \"StepNumber\"=2 AND \"RecipeId\"=(SELECT \"Id\" FROM culinary.\"Recipes\" WHERE \"Slug\"='lab2-chuong-recipe-001')", "23505");
await ExpectConstraint("two primary images", """
    INSERT INTO culinary."RecipeImages" ("Id", "RecipeId", "OriginalUrl", "IsPrimary", "OrderIndex", "CreatedAt", "IsDeleted", "RowVersion")
    SELECT gen_random_uuid(), r."Id", 'https://example.invalid/test-' || s.n, true, s.n, now(), false, decode(repeat('00',16),'hex')
    FROM culinary."Recipes" r CROSS JOIN generate_series(1,2) s(n) WHERE r."Slug"='lab2-chuong-recipe-001';
    """, "23505");
await using (var connection = new NpgsqlConnection(connectionString))
{
    await connection.OpenAsync();
    var matches = (bool)(await new NpgsqlCommand("SELECT to_tsvector('culinary.vietnamese','Phở bò') @@ plainto_tsquery('culinary.vietnamese','pho bo')", connection).ExecuteScalarAsync())!;
    if (!matches) throw new Exception("Unaccent search failed.");
    var indexed = (long)(await new NpgsqlCommand("SELECT count(*) FROM culinary.\"Recipes\" WHERE \"SearchVector\" IS NOT NULL", connection).ExecuteScalarAsync())!;
    if (indexed < 100) throw new Exception("Search trigger did not index seed data.");
    Console.WriteLine("PASS: Vietnamese unaccent search and insert trigger");
}
Console.WriteLine("All database checks passed; test mutations rolled back.");
