# Official recipe dataset sync design

## Intent

Replace the Lab 2 synthetic dataset with the 100 Vietnamese recipes in `recipes.json`, using `images/manifest.json` and `images/` as the local image source. The API continues to serve recipes from PostgreSQL; MinIO stores their image files.

## Goals

- Provide an explicit sync command that imports or updates the official recipe dataset and can be rerun safely.
- Remove the Lab 2 seeding CLI branch and the `Database:SeedLab2` startup configuration/call, plus synthetic records owned by `Lab2Seeder`.
- Replace database-check assertions that call `Lab2Seeder.VerifyAsync` with checks for the official dataset.
- Preserve unrelated users, recipes, categories, and files.
- Keep recipe details in PostgreSQL and copy image bytes into MinIO, with database image records pointing to MinIO URLs.
- Validate the JSON, manifest, and all local image files before mutating the database.

## Non-goals

- Change frontend mock data or recipe screens.
- Add new recipe fields or migrations for source URLs, nutrition, or descriptions absent from the current dataset/schema.
- Automatically run the sync when the API starts.

## Approaches

1. **Explicit, repeatable sync command (chosen):** replace the Lab 2 CLI seed path with an opt-in command. This makes data changes deliberate and supports resyncing after editing the source files.
2. **Seed an empty database during API startup:** convenient for a fresh PostgreSQL volume, but does not update an existing database when the JSON changes and makes API startup depend on the data files and MinIO.

## Design

The new command reads `recipes.json` and uses recipe IDs in `images/manifest.json` to find each local image. It maps name, category, prep/cook time, servings, difficulty, ingredients, and steps to the current recipe entities. Difficulty maps `Dễ`/`Trung bình`/`Khó` to `Easy`/`Medium`/`Hard`. Each ingredient string is preserved whole in `RecipeIngredient.Name`. Each source step maps to a `RecipeStep` whose title is `Bước {n}` and whose description is the original step text; the required `Instructions` field is formed from the ordered steps. The dataset has no description or nutrition fields; nutrition remains empty, while description will use a short generated value based on the recipe name to satisfy the required schema field. Recipes are proposed to be imported as `Published` so they appear in public listing and search. Assign one UTC sync timestamp to `PublishedAt` for newly imported recipes and preserve it on later syncs.

The command uploads each local image through the existing storage service and stores the returned URL in a primary `RecipeImage` row. Stable IDs and a dedicated official seed author identify records owned by this dataset. Reruns update those records without duplicates; records removed from the source are soft-deleted. If a removed seed-owned recipe or category returns in a later sync, the command finds and reactivates that row rather than inserting a conflicting duplicate; the same stable identity rule applies to seed-owned child rows. Existing Lab 2 records are soft-deleted within the same replacement operation because the current DbContext blocks hard deletes. Matching active categories already in the database are reused; other categories and content remain untouched. Categories created by the official seed that disappear from the source are soft-deleted only when no non-seed recipe still references them. Newly uploaded MinIO objects are tracked during the run and deleted if a later upload or database write fails; old objects are deleted only after the database transaction commits.

The command fails before database mutation if required source data, manifest entries, or image files are missing or invalid, or if mapped text exceeds existing database limits. If upload or database persistence fails, it rolls back database work and attempts to delete every object created by that run; cleanup failures are reported for manual recovery. It reports imported recipe/category/image counts after success. The existing database-check executable is updated to verify the official recipe counts and relations rather than Lab 2 minimums. The exact CLI spelling and implementation details are left to the builder, following the existing `Program.cs` command pattern.

## Constraints and risks

- Image files must be available to the process running the command, and MinIO must be reachable. Recreating only the PostgreSQL container while keeping its volume does not make the database empty.
- `Description` is required by the current recipe schema but absent from `recipes.json`; the proposed generated description may be less useful than a human-written summary.
- The source contains `source_url` and `image_source`, but the current recipe model has no fields for them. They remain in the JSON and will not appear in the API or UI.
- Soft-deleted Lab 2 records remain physically stored in PostgreSQL, consistent with the application's deletion policy.
- MinIO cleanup after a failed write is compensating work; a process crash or MinIO deletion failure can still leave an unreferenced object.
- Publishing all records exposes the sample recipes publicly; this is the proposed default for an official ready-to-use dataset.
