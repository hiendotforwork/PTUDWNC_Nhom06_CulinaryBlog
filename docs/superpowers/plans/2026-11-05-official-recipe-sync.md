# Official Recipe Dataset Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Lab 2 synthetic seed with a repeatable command that syncs `recipes.json` and its local images into PostgreSQL and MinIO.

**Architecture:** A small dataset loader validates and maps JSON plus `images/manifest.json`; an infrastructure seeder applies that dataset to the existing entities and storage interface. An explicit API CLI command invokes the sync, while startup seeding is removed and the database-check executable verifies the new official records.

**Tech Stack:** C# / .NET 10, EF Core with PostgreSQL, `System.Text.Json`, existing `IFileStorageService` / MinIO implementation, xUnit, EF Core InMemory for focused sync tests, Docker Compose for local database and MinIO.

**Spec:** `docs/superpowers/specs/2026-11-05-official-recipe-seed-design.md`

## Global Constraints

- The API must not run the dataset sync automatically at startup.
- `recipes.json` and `images/manifest.json` identify source records and image files; PostgreSQL stores recipe details and MinIO stores image bytes.
- The sync command must use the MinIO storage provider and fail clearly when it is configured for local file storage.
- The sync preserves unrelated users, recipes, categories, and files.
- The replacement sync soft-deletes legacy `lab2-chuong-` recipes and their child rows in the same database transaction; it soft-deletes legacy categories only when no non-Lab-2 recipe still uses them.
- Removed seed-owned recipes and categories are soft-deleted; matching seed-owned rows are reactivated if they return in a later sync.
- A seed-owned category is soft-deleted only when no non-seed recipe still references it; a matching active category is reused.
- Imported recipes use `Published`; assign one UTC sync timestamp to `PublishedAt` for new imports and preserve it on later syncs.
- A source step maps to title `Bước {n}` and its exact text in `Description`.
- `source_url` and `image_source` remain in the JSON; do not add schema fields or a migration for them.
- Validate all source records, manifest entries, image files, and mapped text limits before database mutation; reject any manifest image path that resolves outside `images/`.
- On upload or database failure, roll back database work and attempt to delete every MinIO object uploaded in that run; delete replaced or removed old objects only after the database commit.
- Remove the Lab 2 seed CLI and `Database:SeedLab2` startup path; update database checks to validate official data.

## Review Focus

- Missing or malformed JSON, malformed manifest entries, missing image files, path traversal, unknown difficulty, or over-limit text must fail before database changes. Pin this in `OfficialRecipeDataLoaderTests`.
- A later image upload failing after earlier uploads must leave database content unchanged and delete earlier uploads. Pin this in `OfficialRecipeSeederTests`.
- A database save failure after uploads must roll back database work and attempt to delete all new uploads while retaining old referenced images. Pin this in `OfficialRecipeSeederTests` and the database-check run.
- Resync must reactivate a seed-owned recipe/category that returns, and must preserve a matching category used by non-seed content. Pin this in `OfficialRecipeSeederTests`.
- Published rows require `PublishedAt`; each imported step requires a non-empty title and valid text lengths. Pin this in the database checks and loader tests.

---

### Task 1: Load and validate the official dataset

**Files:**
- Create: `src/CulinaryBlog.Infrastructure/Persistence/OfficialRecipeDataLoader.cs`
- Test: `tests/CulinaryBlog.Application.Tests/OfficialRecipeDataLoaderTests.cs`

**Interfaces:**
- Produces: `Task<OfficialRecipeDataset> OfficialRecipeDataLoader.LoadAsync(string dataDirectory, CancellationToken cancellationToken = default)`
- `OfficialRecipeDataset` contains normalized recipe records with source ID, name, category, times, servings, mapped difficulty, full ingredient strings, ordered step strings, and local image path.

- [ ] **Step 1: Write loader tests** for correct mapping; missing files; malformed recipe JSON and manifest JSON/entries; a manifest path such as `../../outside.jpg`; invalid difficulty; missing recipe-to-image entry; and text exceeding existing entity limits.
- [ ] **Step 2: Run the focused tests** with `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter FullyQualifiedName~OfficialRecipeDataLoaderTests`; confirm the tests fail before implementation.
- [ ] **Step 3: Implement `OfficialRecipeDataLoader.LoadAsync`** with `System.Text.Json`; map `Dễ`/`Trung bình`/`Khó` to `Easy`/`Medium`/`Hard`, and validate every recipe and local asset before returning. Resolve manifest paths canonically and reject rooted paths or any resolved path outside the canonical `images/` directory.
- [ ] **Step 4: Run the focused tests** and confirm all loader tests pass.
- [ ] **Step 5: Commit** as `feat: load official recipe dataset`.

### Task 2: Synchronize recipe entities and MinIO images

**Files:**
- Create: `src/CulinaryBlog.Infrastructure/Persistence/OfficialRecipeSeeder.cs`
- Test: `tests/CulinaryBlog.Application.Tests/OfficialRecipeSeederTests.cs`

**Interfaces:**
- Consumes: `OfficialRecipeDataset` from Task 1 and `IFileStorageService` from `CulinaryBlog.Application.Interfaces`.
- Produces: `Task<OfficialRecipeSyncResult> OfficialRecipeSeeder.SyncAsync(ApplicationDbContext db, IFileStorageService storage, OfficialRecipeDataset dataset, CancellationToken cancellationToken = default)` and `Task<OfficialRecipeSyncResult> OfficialRecipeSeeder.VerifyAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)`.
- Produces: `OfficialRecipeSeeder.SeedAuthorId`, the stable author ID used by database checks to select official recipes.
- `OfficialRecipeSyncResult` reports category, recipe, ingredient, step, and image counts.

- [ ] **Step 1: Write sync tests** for one recipe mapping, PublishedAt assignment and preservation, rerun idempotency, soft-delete/reactivation, preservation of unrelated data, category reuse/deletion rules, upload failure cleanup, and database-save failure cleanup.
- [ ] **Step 2: Run `dotnet test tests/CulinaryBlog.Application.Tests/CulinaryBlog.Application.Tests.csproj --filter FullyQualifiedName~OfficialRecipeSeederTests`**; confirm the tests fail before implementation.
- [ ] **Step 3: Implement `OfficialRecipeSeeder.SyncAsync`** using stable identities and a dedicated official seed author; map ingredients as whole `Name` strings, steps to `Bước {n}` plus original descriptions, generated descriptions from recipe names, nullable nutrition, and one primary image per recipe. Upload all images through `IFileStorageService` before database changes so the existing storage service validates each image first; keep uploaded URLs for failure cleanup. In one PostgreSQL transaction, apply the official rows and soft-delete legacy `lab2-chuong-` recipes and children; soft-delete their categories only when no non-Lab-2 recipe still references them. Delete replaced or removed MinIO objects only after commit.
- [ ] **Step 4: Implement `OfficialRecipeSeeder.VerifyAsync`** to validate the official seed's expected recipe/category/child/image relationships and PublishedAt values.
- [ ] **Step 5: Run the focused tests** and confirm all sync and failure-cleanup tests pass.
- [ ] **Step 6: Commit** as `feat: sync official recipes and images`.

### Task 3: Wire the explicit command and replace Lab 2 checks

**Files:**
- Modify: `src/CulinaryBlog.API/Program.cs`
- Modify: `compose.yml`
- Modify: `compose.jobs-tests.yml`
- Modify: `tests/CulinaryBlog.DatabaseChecks/Program.cs`

**Interfaces:**
- Consumes: `OfficialRecipeSeeder.SyncAsync` and `OfficialRecipeSeeder.VerifyAsync` from Task 2.
- Produces: API CLI command `--sync-recipes`; keep `--lab2-migrate` for the dedicated local database migration workflow.

- [ ] **Step 1: Replace the database-check expectations**: remove `Lab2Seeder.VerifyAsync` and every `lab2-chuong-` fixture lookup; check 100 official recipes, 13 categories, published timestamps, child rows, primary image references, and retain the existing local-only database guard plus concurrency, soft-delete, SQL constraint, and Vietnamese search checks. Select fixture IDs by `OfficialRecipeSeeder.SeedAuthorId`; use an official recipe with at least two steps and a second official recipe for the duplicate-slug check, so SQL constraint updates always affect real rows.
- [ ] **Step 2: Run the updated database checks** against the local `culinary_lab2` database before import; confirm they fail because the official dataset is absent.
- [ ] **Step 3: Wire `--sync-recipes` in `Program.cs`** to load the repository-root dataset, resolve the existing MinIO `IFileStorageService` registration without starting the HTTP host, invoke `SyncAsync`, and print the returned counts; fail if `FileStorage:Provider` is `Local`. Remove `--lab2-seed`, `--lab2-verify`, and the `Database:SeedLab2` startup call while retaining `--lab2-migrate`.
- [ ] **Step 4: Remove `Database__SeedLab2`** from `compose.yml` and `compose.jobs-tests.yml`; keep the sync opt-in. Leave source JSON/images in the shared workspace and run the CLI from the repository root so the loader can resolve them.
- [ ] **Step 5: Run `dotnet build src/CulinaryBlog.API/CulinaryBlog.API.csproj` and `dotnet build tests/CulinaryBlog.DatabaseChecks/CulinaryBlog.DatabaseChecks.csproj`**; confirm both pass and no runtime code/configuration references to the removed seed/verifier remain.
- [ ] **Step 6: Run the local end-to-end path**: start `compose.lab2.yml` PostgreSQL and `compose.storage.yml` MinIO, apply `--lab2-migrate`, run `--sync-recipes` from the repo root with local PostgreSQL and MinIO settings, then run `tests/CulinaryBlog.DatabaseChecks` against the local `culinary_lab2` database. Confirm 100 official recipes reference all 13 official categories, 100 primary image records exist, PublishedAt values are valid, legacy Lab 2 recipes are hidden by soft-delete, and the existing database checks pass.
- [ ] **Step 7: Commit** as `feat: replace lab2 seed workflow with official sync`.

## Self-review

- **Spec coverage:** Task 1 covers source loading and validation; Task 2 covers mappings, MinIO upload/cleanup, stable ownership, legacy Lab 2 cleanup, soft-delete/reactivation, and counts; Task 3 removes both seed entry points, updates every database-check fixture to official IDs, and proves the explicit local sync path.
- **Step scan:** Each step names one deliverable and its check; CLI wiring and database-check updates share a task because they replace the same old entry points and must compile together.
- **Type consistency:** Task 2 consumes the normalized dataset and storage interface defined by Task 1; Task 3 calls the exact `SyncAsync`/`VerifyAsync` signatures from Task 2.
- **Review focus:** All five listed failure modes have a focused loader/sync test or the isolated PostgreSQL database-check run.
- **Proportion:** Three tasks match the loader, sync engine, and workflow wiring; no schema migration or new dependency is planned.
