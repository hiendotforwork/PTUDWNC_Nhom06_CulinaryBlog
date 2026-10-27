# FR-AUTH-007 Profile Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an authenticated user persist changes to their display name and bio, and upload or remove their avatar through the profile page.

**Architecture:** Add a MediatR update command for display name and bio at `PATCH /api/v1/auth/me`. Reuse the existing authenticated avatar `POST` and `DELETE` endpoints; safely queue cleanup only for old avatar URLs owned by the configured storage. Connect these APIs to the existing profile form.

**Tech Stack:** ASP.NET Core Identity, MediatR, FluentValidation, EF Core, Next.js, React, TypeScript, MinIO/local file storage.

**Spec:** `docs/specs/fr-auth/FR-AUTH_APIContract.md` (API-7, API-6 response); `docs/specs/fr-auth/FR-AUTH_BaoCao.md` (FR-AUTH-007 and Decision 4A), with the approved avatar-operation decision below.

## Global Constraints

- `PATCH /api/v1/auth/me` requires Bearer authentication.
- **Approved API-7 adjustment:** PATCH updates only `displayName` and `bio`; avatar changes use the existing authenticated avatar upload/delete endpoints. Update both referenced API specification documents to reflect this decision before implementing the endpoint.
- Only `displayName` and `bio` can change through PATCH; `email`, `userName`, and `avatarUrl` remain read-only there.
- `displayName` is 2–100 characters; `bio` is at most 500 characters.
- Omitted or null PATCH values leave their stored values unchanged; an empty `bio` clears it.
- Avatar upload reuses `POST /api/v1/users/me/avatar`, stores only for the authenticated user, and accepts supported JPEG, PNG, WebP, or AVIF images up to 5 MB as validated by `ImageFile`.
- Avatar removal reuses `DELETE /api/v1/users/me/avatar`. On upload or removal, queue cleanup only when the previous URL belongs to the configured local or MinIO storage; external URLs (such as Google avatars) are not sent to the deletion queue.
- Adding `IFileStorageService.IsOwnedUrl` requires updating each implementation/test double; only `AvatarsController` uses the new method. Existing recipe image and background job behavior remains unchanged.
- No database migration or new dependency is needed.
- Preserve the existing uncommitted retry-button change in `frontend/app/profile/page.tsx`.

## Review Focus

- Omitted PATCH fields must retain existing values; empty bio clears it. Pin both in update-handler tests.
- Invalid display name or bio length must return validation errors (422). Pin field boundaries in validator tests and outer-whitespace trimming in a handler test; verify the form reflects the saved value in the Task 2 manual check.
- A missing user ID claim must not update a profile and returns 401. Pin this in controller tests.
- An authenticated ID for a deleted user returns 404 without mutating another user. Pin this in handler/controller tests.
- Invalid, oversized, or spoofed avatar files must not replace the stored avatar. Keep the existing `ImageFile` signature/size checks.
- Replacing/removing a storage-owned avatar queues cleanup; an external avatar is never queued. If upload/update fails, the old avatar remains referenced and the cleanup job's reference check protects it.

---

### Task 1: Persist profile edits through the authenticated API

**Files:**
- Modify: `docs/specs/fr-auth/FR-AUTH_APIContract.md`
- Modify: `docs/specs/fr-auth/FR-AUTH_BaoCao.md`
- Create: `src/CulinaryBlog.Application/DTOs/Auth/UpdateProfileRequest.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/UpdateProfile/UpdateProfileCommand.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/UpdateProfile/UpdateProfileCommandValidator.cs`
- Create: `src/CulinaryBlog.Application/Commands/Auth/UpdateProfile/UpdateProfileCommandHandler.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IUserRepository.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Services/UserRepository.cs`
- Modify: `src/CulinaryBlog.API/Controllers/AuthController.cs`
- Modify: `src/CulinaryBlog.Application/Interfaces/IFileStorageService.cs`
- Modify: `src/CulinaryBlog.API/Services/LocalFileStorageService.cs`
- Modify: `src/CulinaryBlog.Infrastructure/Storage/MinioFileStorageService.cs`
- Modify: `src/CulinaryBlog.API/Controllers/AvatarsController.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Commands/UpdateProfileCommandValidatorTests.cs`
- Create: `tests/CulinaryBlog.Application.Tests/Commands/UpdateProfileCommandHandlerTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/AuthControllerProfileUnitTests.cs`
- Modify: `tests/CulinaryBlog.Application.Tests/MinioFileStorageTests.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/TestFileStorage.cs`
- Modify: `tests/CulinaryBlog.Integration.Tests/JobsObservabilityDatabaseTests.cs` (`RecordingStorage` test double)
- Create: `tests/CulinaryBlog.Integration.Tests/LocalFileStorageTests.cs`
- Create: `tests/CulinaryBlog.Integration.Tests/AvatarTests.cs`

**Interfaces:**
- Consumes: `IUserRepository.FindByIdAsync(string, CancellationToken)`, `GetRolesAsync(ApplicationUser)`; existing `UserProfileDto`.
- Produces: `UpdateProfileCommand(string UserId, string? DisplayName, string? Bio) : IRequest<UserProfileDto>`; repository `UpdateAsync(ApplicationUser, CancellationToken) -> Task<(bool Succeeded, IEnumerable<string> Errors)>`; `IFileStorageService.IsOwnedUrl(string url) -> bool` for safe old-avatar cleanup.
- `UpdateProfileRequest` has nullable `DisplayName` and `Bio` JSON properties. Controller obtains the user ID from the authenticated claims, validates the command, sends it, and returns the updated `UserProfileDto`.

- [ ] **Step 1: Align both API specification documents with the approved API-7 adjustment.** Document `displayName` and `bio` as the PATCH fields and direct avatar upload/removal to the existing `POST`/`DELETE /api/v1/users/me/avatar` endpoints.
- [ ] **Step 2: Add request/command contracts, an empty compile-ready validator, and validator tests.** Name the tests `DisplayNameMustBe2To100WhenSupplied` and `BioMayBeEmptyButCannotExceed500`; assert null values pass, 2/100 and 500 pass, and 1/101 and 501 fail.
- [ ] **Step 3: Run validator tests** with `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~UpdateProfileCommandValidatorTests`; expect the boundary assertions to fail (not compilation) before adding rules.
- [ ] **Step 4: Implement `UpdateProfileCommandValidator`** with FluentValidation; validate only supplied values, require the trimmed display name to be 2–100 characters, and allow empty bio. Leave persistence normalization to the handler.
- [ ] **Step 5: Add the `IUserRepository.UpdateAsync` contract and an empty compile-ready handler, then add handler tests** named `MissingUserThrowsUserNotFound`, `PartialUpdatePreservesOmittedFieldsAndNulls`, `DisplayNameIsTrimmedBeforePersistence`, `EmptyBioClearsAndReturnsProfileWithRoles`, and `IdentityUpdateFailureThrowsIdentityOperationException`; assert each outcome and that Identity persistence receives the modified user only.
- [ ] **Step 6: Run handler tests** with `dotnet test tests/CulinaryBlog.Application.Tests --filter FullyQualifiedName~UpdateProfileCommandHandlerTests`; expect the behavior assertions to fail before implementing the repository and handler logic.
- [ ] **Step 7: Implement `IUserRepository.UpdateAsync`** by forwarding to `UserManager.UpdateAsync` and returning success plus Identity error descriptions.
- [ ] **Step 8: Implement `UpdateProfileCommandHandler.Handle(UpdateProfileCommand, CancellationToken) -> Task<UserProfileDto>`**: load by `UserId`, throw `AuthException.UserNotFound()` if missing, apply only supplied fields, persist `DisplayName.Trim()` when supplied, normalize blank bio to `null`, persist through the repository, throw `IdentityOperationException` on Identity failure, and return the updated public profile with roles.
- [ ] **Step 9: Add controller tests** named `PatchMeWithoutUsableUserIdReturnsUnauthorizedWithoutSendingCommand` and `PatchMeUsesClaimUserIdAndReturnsUpdatedProfile`; assert missing claims cause no mediator call and the response uses the authenticated claim ID.
- [ ] **Step 10: Add `[HttpPatch("me")]` to `AuthController`** with `[Authorize]`, request-to-command mapping, the existing explicit FluentValidation pattern, and `200 OK` containing `UserProfileDto`.
- [ ] **Step 11: Add `IsOwnedUrl(string url) -> bool` to `IFileStorageService`**; implement it in local storage using its existing URL shape validation, in MinIO by validating through `GetObjectName` without deleting, and in both integration test doubles (`TestFileStorage` and `RecordingStorage`) using their generated URL shapes. Keep existing `UploadAsync`/`DeleteAsync` behavior unchanged; only `AvatarsController` calls the new method.
- [ ] **Step 12: Update `AvatarsController` upload and delete** to enqueue the previous avatar only when `storage.IsOwnedUrl(oldUrl)` is true; retain queue-before-Identity-update ordering and existing cleanup of a newly uploaded file on update failure.
- [ ] **Step 13: Add ownership and endpoint tests.** In `MinioFileStorageTests`, assert `IsOwnedUrl` accepts a valid bucket URL and rejects another host or malformed key. In `LocalFileStorageTests`, assert it accepts a generated local avatar URL and rejects external/malformed paths. In `AvatarTests`, assert upload replacement and DELETE enqueue an owned old URL, skip an external old URL, and a failed replacement leaves the old URL on the user.
- [ ] **Step 14: Run focused backend tests** for the new handler, validator, profile controller, storage ownership, and avatar controller behavior; expect all to pass.
- [ ] **Step 15: Commit** as `feat(auth): persist profile updates and avatar cleanup`.

### Task 2: Connect profile form and avatar upload to the API

**Files:**
- Modify: `frontend/app/lib/api.ts`
- Modify: `frontend/app/lib/auth.ts`
- Modify: `frontend/app/context/AppContext.tsx`
- Modify: `frontend/app/profile/page.tsx`

**Interfaces:**
- Consumes: Task 1 `PATCH /api/v1/auth/me`; existing `GET /api/v1/auth/me`; existing `POST /api/v1/users/me/avatar` returning `{ avatarUrl: string }`.
- Produces: `updateProfileApi(data: { displayName?: string; bio?: string }): Promise<UserProfileResponse>`, `uploadAvatar(file: File): Promise<{ avatarUrl: string }>`, and `deleteAvatar(): Promise<void>` in `api.ts`; `AppContext.updateProfile(data): Promise<User>` returns the saved server profile mapped to `User` after updating React state and localStorage. The profile page uses that returned `User` to reset its local display-name and bio fields to the values actually saved. Mapping uses `DEFAULT_AVATAR` for null avatar URLs, derives `role` from `roles`, and preserves server `createdAt` in localStorage.

- [ ] **Step 1: Add `uploadAvatar` API helper** using `FormData`, authenticated `authFetch`, and `handleResponse`; do not set a JSON content type for multipart requests.
- [ ] **Step 2: Add `updateProfileApi`** using authenticated `PATCH /api/v1/auth/me` with JSON and a `UserProfileResponse` result; send only display name and bio.
- [ ] **Step 3: Add `deleteAvatar` API helper** using authenticated `DELETE /api/v1/users/me/avatar` and handle its `204 No Content` response.
- [ ] **Step 4: Convert `AppContext.updateProfile` to async persistence**; only after success map the response to `User`, update `currentUser`, persist the matching `AuthUser` fields with the response's `createdAt`, and return the mapped `User`. Map `avatarUrl ?? DEFAULT_AVATAR` and `bio ?? undefined` for `User`, and derive `role` from `roles`. Update `setStoredUser` to preserve a supplied `createdAt` and fall back to the current time only when it is absent. Surface API failure rather than reporting false success.
- [ ] **Step 5: Update the profile form** with an async submit handler: await `updateProfile`, then call `setDisplayName(savedUser.displayName)` and `setBio(savedUser.bio ?? "")`; catch failures to show an error and always clear `isSaving` in `finally`. Remove the simulated `setTimeout` save. Remove the manual avatar URL field, and add an accessible file chooser plus upload and remove actions. Accept JPEG/PNG/WebP/AVIF and reject files over 5 MB before upload.
- [ ] **Step 6: Keep stored and visible avatar state in sync.** On successful upload, sync the returned URL to the form, `currentUser`, and stored `AuthUser`; on successful removal, use `DEFAULT_AVATAR` in the UI/current user and clear `avatarUrl` in stored `AuthUser`. Preserve the existing `createdAt` in both cases. On failure keep the prior avatar and show an error. Do not change the existing retry-button edit in this file.
- [ ] **Step 7: Run frontend checks** with `pnpm --dir frontend lint` and `pnpm --dir frontend exec tsc --noEmit`; expect both to pass.
- [ ] **Step 8: Manually verify profile save in the browser.** Submit a display name with leading/trailing spaces and two internal spaces; after success, confirm both the form and profile display show the trimmed value while preserving internal spaces. Reload the page and confirm the saved name remains. Cause a save failure and confirm the error appears, inputs remain available, and saving state clears.
- [ ] **Step 9: Commit** as `feat(frontend): persist profile edits and avatar upload/delete`.

## Self-Review

- **Spec coverage:** profile fields, authentication, validation, response DTO, approved API-7 adjustment, avatar upload/removal, ownership-scoped cleanup, and frontend/localStorage synchronization are covered by Tasks 1–2.
- **Step scan:** each implementation step names one test or code deliverable and a check; task interfaces identify the API contract between backend and frontend.
- **Type consistency:** the update command/request/frontend PATCH fields match (`displayName`, `bio`); `AppContext.updateProfile` returns the `User` mapped from the API response so React state and form state use the same normalized values; upload/delete helpers use the existing avatar endpoint contracts; the response mapping accounts for nullable API avatar/bio fields and the frontend `User` shape.
- **Review focus:** all six review items map to validator, handler, controller, or existing image validation checks above.
- **Proportion:** no migration, new endpoint, manual URL editing, or extra dependency is planned; `IsOwnedUrl` reuses the existing local URL validation and MinIO object-name validation. Recipe image and background job behavior remains unchanged. `setStoredUser` uses a supplied `createdAt` and falls back to the current time only when the API/auth response omits it.
