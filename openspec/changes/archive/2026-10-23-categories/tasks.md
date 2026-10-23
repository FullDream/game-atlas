# Tasks

## 1. Domain state and atomic updates

- [x] 1.1 Add Category and separate domain errors with UUID v7 creation, required metadata, name/icon trimming, invariant name key, uppercase hexadecimal color, positive order, and atomic updates including CategoryGroupId. Verify domain tests cover valid creation, invalid values without partial assignment, individual/combined changes, membership changes, immutable identity/creation time, and normalized no-ops preserving UpdatedAt.

## 2. Persistence and migration

- [x] 2.1 Add Categories to the application context contract and EF context, configure required fields, unique (CategoryGroupId, NormalizedName), nonunique display-order index, UTC timestamp conversions, and the cascading group FK. Generate the migration, designer, and snapshot through EF CLI; verify migrated SQLite round trips, name uniqueness, orphan prevention, and that no marker counter column exists.
- [x] 2.2 Extend precise category name constraint classification. Verify tests distinguish the actual category uniqueness constraint from group/game uniqueness, foreign-key violations, and unrelated database errors.
- [x] 2.3 Document upgrade and rollback commands in the persistence README. Verify migration upgrade from a seeded CategoryGroups database and rollback preserve existing games and groups while creating/removing category storage, using disposable databases only.

## 3. Application operations and validation

- [x] 3.1 Add CategoryDto and separate mapping extensions returning all nine agreed fields with MarkersCount zero. Verify the Application project builds and DTO assertions cover stored values without exposing NormalizedName or redundant GameId.
- [x] 3.2 Implement CreateCategory and UpdateCategory commands, validators, and separate application error classes using the shared validation behavior and Optional helpers. Verify validation tests cover trimmed name/icon boundaries, exact color format, required/null values, positive optional order, nullable optional destination, and empty PATCH; an all-zero destination GUID remains a lookup miss rather than a format validation failure.
- [x] 3.3 Implement CreateCategoryHandler using the existing transaction abstraction, scoped parent checks, group-local default order allocation with overflow guard, and normalized-name conflict checks. Verify handler/integration tests accept explicit duplicate orders, reject scoped Latin/Cyrillic duplicates, return 404 for foreign/missing parents, and preserve group/game timestamps.
- [x] 3.4 Implement ListCategoriesHandler with a scoped parent existence check and ordered, untracked DTO projection. Verify tests return a full array with zero counts, existing-empty versus missing-parent behavior, isolation across groups/games, and Order/CreatedAt/Id tie breaking.
- [x] 3.5 Implement UpdateCategoryHandler with current-route scoping, same-game destination validation, resulting destination-name checks, atomic domain mutation, and save-on-effective-change behavior. Verify rename/transfer combinations, conflict rollback, missing/foreign destinations, old/new routes after transfer, unchanged transfer order, explicit order changes, and no-op timestamp preservation.
- [x] 3.6 Implement DeleteCategoryHandler with scoped physical deletion. Verify empty success, missing/repeated/foreign category behavior, reusable names, retained order gaps, and unchanged surviving category and parent metadata.

## 4. HTTP, OpenAPI, and API documentation

- [x] 4.1 Add resource-level Categories/Endpoints.cs, separate create/update request and mapping files, endpoint registration, and existing OutcomeResult/shared PATCH integration. Verify HTTP tests exercise all four operations for draft/published games, nine-field responses, server-owned creation values, GUID route failures, PATCH omission/null behavior, unsupported media, invalid bodies/types, and read-only/unknown update members.
- [x] 4.2 Add the category name conflict exception handler using the persistence classifier and register it consistently with existing conflict handlers. Verify real save-time create/rename/transfer uniqueness failures return 409 name ProblemDetails with full rollback; unrelated failures return safe 500 responses.
- [x] 4.3 Register request validation metadata in the existing FluentValidation-to-OpenAPI integration and document category routes, transfers, order, icon references, color, count, and cascade behavior in the API README and HTTP examples. Verify OpenAPI tests cover four operation IDs, complete schemas, bounds/patterns, GUID routes, optional PATCH properties, both media types, relevant failures, and absent individual GET/PUT/reorder/batch/pagination operations.

## 5. Cross-operation integrity and regression checks

- [x] 5.1 Verify integration tests for concurrent default creation and same-name conflicts, and injected persistence failures during create/update/transfer/delete. Assert durable successful writes, no partial failed writes, unchanged parent metadata, and exact 409 versus 500 classification.
- [x] 5.2 Verify real SQLite and HTTP cascade tests for group and direct game deletion, unrelated-data preservation, and transferred categories surviving former-group deletion. Confirm existing group/game response contracts remain compatible.
- [x] 5.3 Run the full solution test suite, project formatting verification, `git diff --check`, and `openspec validate categories --strict`. Verify all pass and the implementation and documentation match the agreed category contract before marking the change complete.

## 6. Short hexadecimal colors

- [x] 6.1 Accept #RGB and #RRGGBB on creation and updates, expand short colors to uppercase #RRGGBB, and preserve equivalent-color no-ops. Update the specification, design, API documentation, and shared validation metadata; verify domain, validation, HTTP persistence/no-op, and OpenAPI tests, formatting of the changed files, and strict OpenSpec validation.
