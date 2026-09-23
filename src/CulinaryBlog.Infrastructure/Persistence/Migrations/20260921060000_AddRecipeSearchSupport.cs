using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CulinaryBlogDbContext))]
[Migration("20260921060000_AddRecipeSearchSupport")]
public sealed class AddRecipeSearchSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE SCHEMA IF NOT EXISTS extensions;
            CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA extensions;
            CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA extensions;
            CREATE TEXT SEARCH CONFIGURATION culinary.vietnamese (COPY = pg_catalog.simple);
            DO $block$
            DECLARE extension_schema text;
            BEGIN
                SELECT n.nspname INTO extension_schema
                FROM pg_extension e JOIN pg_namespace n ON n.oid=e.extnamespace
                WHERE e.extname='unaccent';
                EXECUTE format('ALTER TEXT SEARCH CONFIGURATION culinary.vietnamese ALTER MAPPING FOR hword, hword_part, word WITH %I.unaccent, pg_catalog.simple', extension_schema);
            END $block$;
            CREATE FUNCTION culinary.update_recipe_search() RETURNS trigger
            LANGUAGE plpgsql AS $body$
            BEGIN
                NEW."SearchVector" := setweight(to_tsvector('culinary.vietnamese', coalesce(NEW."Title",'')), 'A')
                    || setweight(to_tsvector('culinary.vietnamese', coalesce(NEW."Description",'')), 'B');
                RETURN NEW;
            END $body$;
            CREATE TRIGGER recipe_search BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON culinary."Recipes" FOR EACH ROW EXECUTE FUNCTION culinary.update_recipe_search();
            UPDATE culinary."Recipes" SET "Title"="Title";
            CREATE UNIQUE INDEX "IX_Categories_Name_CaseInsensitive" ON culinary."Categories" (lower("Name"));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX culinary."IX_Categories_Name_CaseInsensitive";
            DROP TRIGGER recipe_search ON culinary."Recipes";
            DROP FUNCTION culinary.update_recipe_search();
            DROP TEXT SEARCH CONFIGURATION culinary.vietnamese;
            """);
        // Extensions may be shared with Supabase or other applications; keep them.
    }
}
