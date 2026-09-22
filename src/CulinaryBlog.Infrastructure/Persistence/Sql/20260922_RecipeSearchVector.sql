CREATE EXTENSION IF NOT EXISTS unaccent;

ALTER TABLE "Recipes"
    ADD COLUMN IF NOT EXISTS "SearchVector" tsvector;

CREATE OR REPLACE FUNCTION culinaryblog_recipe_search_vector_update()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW."SearchVector" := to_tsvector(
        'simple',
        unaccent(coalesce(NEW."Title", '') || ' ' || coalesce(NEW."Description", ''))
    );
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS recipe_search_vector_update ON "Recipes";

CREATE TRIGGER recipe_search_vector_update
BEFORE INSERT OR UPDATE OF "Title", "Description"
ON "Recipes"
FOR EACH ROW
EXECUTE FUNCTION culinaryblog_recipe_search_vector_update();

UPDATE "Recipes"
SET "SearchVector" = to_tsvector(
    'simple',
    unaccent(coalesce("Title", '') || ' ' || coalesce("Description", ''))
);

CREATE INDEX IF NOT EXISTS "IX_Recipe_SearchVector"
ON "Recipes" USING GIN ("SearchVector");