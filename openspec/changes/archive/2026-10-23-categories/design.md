# Design

## Context

See proposal.md for motivation and specs/categories/spec.md for the behavior contract. The solution already implements CategoryGroups across Domain, Application, Infrastructure, and Api with feature folders, normalized-name uniqueness, positive display order, shared Optional validation and PATCH handling, and short mutation transactions. `IApplicationDbContext` exposes entity sets and `BeginTransactionAsync` returning `IApplicationTransaction`; provider APIs remain in Infrastructure. Existing tests cover HTTP behavior, OpenAPI schemas, actual SQLite constraints, concurrency, and rollback.

This change adds a child entity and permits its parent foreign key to change within the same game. That creates enough data-model and migration impact to warrant an explicit design.

## Goals / Non-Goals

**Goals:**

- Extend the established group patterns with a directly persisted Category entity and four focused operation slices.
- Make membership, metadata, and uniqueness decisions consistent within a transaction, including combined rename and transfer.
- Keep the model ready for future marker relationships without implementing marker persistence or counter maintenance now.

**Non-Goals:**

- No new module projects, repository layer, general transaction pipeline, image service, or PostgreSQL provider.
- No eager category navigation collections on Game or CategoryGroup and no marker placeholders.
- No general color/image value-object framework or generic sorting/reordering abstraction.

## Decisions

### 1. Persist a Category entity owned through CategoryGroupId

Add Domain/Categories/Category with server-generated UUID v7, mutable CategoryGroupId, Name, NormalizedName, Icon, Color, Order, and UTC CreatedAt/UpdatedAt. Id and CreatedAt are immutable to clients. Store the group FK directly; derive the owning game through CategoryGroup rather than duplicating GameId on Category. This avoids maintaining two ownership columns during transfers. Explicitly scoped handler queries still verify both route game and route group.

Domain creation and update methods enforce required state, positive order, color syntax, normalization, and atomic assignment. Compute all resulting values before mutating; return Result for expected failures. Update returns whether effective state changed, following CategoryGroup.Update. Application validation applies request bounds and optional-field rules through the existing shared behavior. Keep fixed and parameterized errors in separate static classes in the owning layer.

### 2. Keep representations and mappings separate

Add CategoryDto with exactly Id, CategoryGroupId, Name, Icon, Color, Order, MarkersCount, CreatedAt, and UpdatedAt. GameId is available in the route and is not duplicated in the DTO. MarkersCount is the integer literal zero supplied by projection/mapping, with no entity field or database column.

Use separate files for DTOs, mapping extensions, and API request declarations. CreateCategoryRequest uses ordinary properties with required-value validation for name/icon/color and Optional<int?> for order. Avoid optional constructor defaults that interfere with the existing OpenAPI schema exporter. UpdateCategoryRequest uses Optional<string?> for strings, Optional<int?> for order, and Optional<Guid?> for CategoryGroupId, plus the established unknown-member rejection. Reuse Optional.AnySpecified, OptionalRuleFor, and GetValueOrDefault as appropriate.

### 3. Apply normalization narrowly

Names follow CategoryGroup: trim for display, then ToUpperInvariant for the uniqueness key. Icons are trimmed opaque strings, limited to 2048 characters by request validation. Do not fetch references or infer image type from extensions. Color accepts ASCII hexadecimal `#RGB` and `#RRGGBB` values, expands short values by duplicating each digit, and stores uppercase `#RRGGBB`. Whitespace is not part of that format and is rejected. These choices keep persisted values predictable without parsing CSS colors or introducing remote validation.

### 4. Reuse the four operation slices and resource endpoint file

Add CreateCategory, ListCategories, UpdateCategory, and DeleteCategory feature folders under Application/Categories. Handlers use IApplicationDbContext directly. Add Api/Categories/Endpoints.cs mapping `/games/{gameId}/category-groups/{groupId}/categories` and its `/{id}` item route, with separate request mapping files. Use existing OutcomeResult metadata and shared PATCH conventions; register new validators in the existing OpenAPI validation integration. No individual GET is introduced.

List verifies the route group belongs to the route game before returning an AsNoTracking, fully scoped array ordered by Order, CreatedAt, and Id. Missing parent returns 404; an existing empty parent returns an empty array. Project only DTO fields, including the zero count, rather than loading parent graphs.

### 5. Use short transactions for allocation and transfers

Follow the existing CategoryGroups transaction pattern for writes, beginning the SQLite write transaction before the relevant reads through IApplicationTransaction. Creation checks parent membership, computes max order within that group when omitted, guards int.MaxValue, checks the normalized name, creates the entity, saves, and commits. An explicit duplicate positive order remains valid.

Update loads the category with its current group and route game scope. Resolve the destination from the optional CategoryGroupId; when supplied, verify it belongs to the route game. Check the resulting normalized name against other categories in that destination, excluding the target ID. Check the destination whenever membership changes even when the name is unchanged. A simultaneous rename is checked using the new name, not the old one.

Call the domain update only after required lookups and business checks. Membership and metadata are saved together. Preserve order on transfer unless supplied; never allocate a new default order for PATCH. Save only for effective changes, commit, and return the resulting DTO. Failed checks or failed saves dispose the transaction without commit. Group and game timestamps are not touched. No concurrency token or new last-writer policy is added beyond the established transaction behavior.

### 6. Use relational constraints as the final integrity check

Add DbSet<Category> to IApplicationDbContext and ApplicationDbContext. Configure a Categories table with UUID primary key, required CategoryGroupId FK to CategoryGroups using cascade delete, required bounded name/normalized-name/icon/color columns, positive-order domain validation, and the existing UTC timestamp converter. Add a unique index on (CategoryGroupId, NormalizedName) and a nonunique index on (CategoryGroupId, Order). Duplicate orders are intentionally permitted.

Only the group FK is needed. Its cascade combines with the existing Game-to-CategoryGroup cascade, including direct ExecuteDeleteAsync game deletion. Transfers update that FK, so deleting a former group cannot delete the moved category. Verify cascades using a real migrated SQLite database rather than relying on EF tracking behavior.

Extend IPersistenceFailureClassifier with category name uniqueness recognition and add an API exception handler following CategoryGroupNameConflictExceptionHandler. Match the specific SQLite unique constraint on Categories.CategoryGroupId and Categories.NormalizedName. Preflight and save-time conflicts both produce 409 with name errors; other database failures remain centralized safe 500 responses. A generic catch mapping database errors to conflicts is not appropriate.

### 7. Extend existing validation metadata

Use the existing FluentValidation-to-OpenAPI sources for name/icon lengths, positive order, and color syntax where supported. Keep semantic descriptions for transfers, optional fields, and zero marker count. Prefer a narrow addition to shared metadata support only if an actual tested schema gap remains; do not introduce a per-operation transformer duplicating request rules. Native JSON binding and the shared PATCH convention handle incompatible types, unsupported media, and unknown/read-only update members.

## Risks / Trade-offs

- [SQLite serializes writers] -> Keep transactions short, perform no remote work inside them, and test concurrent default appends and name conflicts using the existing integration fixture.
- [Nested item paths change after transfer] -> Document current-group routing explicitly and verify the old path returns 404 while the new path works.
- [Names depend on invariant normalization rather than SQLite NOCASE] -> Persist the normalized key as for groups and test Latin and Cyrillic conflicts, including transfers and save-time violations.
- [Icon strings may reference unavailable or unsupported client content] -> Treat them as references only; file upload and rendering policy remain outside this change.
- [Zero marker count is temporary] -> Document it as server-owned and unpersisted; replace the projection when marker functionality is specified.
- [Cascade deletion is destructive] -> Test current ownership after transfer and verify unrelated groups and games are preserved; rollback of the migration removes category data.

## Migration Plan

1. Generate an EF CLI migration in Infrastructure after adding the entity configuration, including its designer and model snapshot. Start from the existing CategoryGroups migration; do not rewrite migration history.
2. Test upgrade from the preceding migration with existing games and groups, then verify category round trips, uniqueness, orphan prevention, cascades, and transfer durability on SQLite.
3. Apply the migration explicitly through the documented EF CLI deployment process. Do not run migrations automatically at API startup or change an existing local database during implementation without a request to apply them.
4. For rollback, deploy the previous application version and migrate back to the preceding CategoryGroups migration. The Categories table and its data are removed; existing games and groups remain. Document backup needs and the exact generated migration commands in the persistence README.
