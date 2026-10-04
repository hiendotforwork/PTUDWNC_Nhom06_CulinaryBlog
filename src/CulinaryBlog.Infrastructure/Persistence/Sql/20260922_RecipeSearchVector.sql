-- Apply only after the culinary recipe schema has been created.
CREATE SCHEMA IF NOT EXISTS extensions;
CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA extensions;

DO $block$
DECLARE extension_schema text;
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_ts_config c JOIN pg_namespace n ON n.oid = c.cfgnamespace
        WHERE n.nspname = 'culinary' AND c.cfgname = 'vietnamese'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION culinary.vietnamese (COPY = pg_catalog.simple);
    END IF;
    SELECT n.nspname INTO extension_schema
    FROM pg_extension e JOIN pg_namespace n ON n.oid = e.extnamespace
    WHERE e.extname = 'unaccent';
    EXECUTE format('ALTER TEXT SEARCH CONFIGURATION culinary.vietnamese ALTER MAPPING FOR hword, hword_part, word WITH %I.unaccent, pg_catalog.simple', extension_schema);
END $block$;

ALTER TABLE culinary."Recipes" ADD COLUMN IF NOT EXISTS "SearchVector" tsvector;

CREATE OR REPLACE FUNCTION culinary.update_recipe_search()
RETURNS trigger LANGUAGE plpgsql AS $body$
BEGIN
    NEW."SearchVector" :=
        setweight(to_tsvector('culinary.vietnamese', coalesce(NEW."Title", '')), 'A')
        || setweight(to_tsvector('culinary.vietnamese', coalesce(NEW."Description", '')), 'B');
    RETURN NEW;
END $body$;

DROP TRIGGER IF EXISTS recipe_search_vector_update ON culinary."Recipes";
DROP TRIGGER IF EXISTS recipe_search ON culinary."Recipes";
CREATE TRIGGER recipe_search
BEFORE INSERT OR UPDATE OF "Title", "Description" ON culinary."Recipes"
FOR EACH ROW EXECUTE FUNCTION culinary.update_recipe_search();

UPDATE culinary."Recipes" SET "Title" = "Title";
CREATE INDEX IF NOT EXISTS "IX_Recipes_SearchVector"
ON culinary."Recipes" USING GIN ("SearchVector");
