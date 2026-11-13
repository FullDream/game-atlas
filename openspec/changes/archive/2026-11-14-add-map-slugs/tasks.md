# Tasks

## 1. Shared slug value and domain integration

- [x] 1.1 Add the immutable shared Domain Slug factory, errors and value equality; verify focused backend tests cover trim, null/blank, 200/201 characters, accepted syntax, invalid separators, uppercase, non-ASCII and equality.
- [x] 1.2 Convert Game and Map slug properties and domain create/update operations to the value object, incorporating existing user edits and updating domain callers/fixtures; verify domain tests cover required names/slugs, atomic failures, omission, rename independence and no-op timestamps, and affected projects compile.
- [x] 1.3 Replace duplicated Application slug validation with shared validation while retaining raw nullable request inputs, PATCH presence and resource-specific field errors; verify focused validation/handler tests reject invalid name/slug and preserve existing game HTTP semantics.

## 2. Persistence and safe upgrade

- [x] 2.1 Add the Infrastructure slug string converter and configure required 200-character scalar storage for both resources; verify SQLite round-trip and equality-query tests, existing global game slug uniqueness and independent map name uniqueness.
- [x] 2.2 Create a new migration with deterministic map-{id:N} backfill, required column and unique (GameId, Slug) index, preserving old migrations; verify fresh-store migration and a populated upgrade retain exact IDs, timestamps, geometry, defaults, styles, processing state, content relationships and files, with unchanged game slugs.
- [x] 2.3 Extend map slug conflict classification and centralized handling and add handler prechecks excluding the current map; verify duplicate create/update and save-time races return 409 atomically, equal slugs across games succeed, deletion permits reuse and unrelated failures remain 500.
- [x] 2.4 Update persistence migration/rollback documentation with explicit application, backup requirements and slug loss on downgrade; verify the documented migration commands and expected upgrade/downgrade behavior against an isolated database.

## 3. HTTP contracts and resource representations

- [x] 3.1 Add required map creation slug and optional PATCH slug to API/Application contracts and mappings while keeping endpoints and queries ID-based; verify API tests cover missing/null/invalid name and slug, valid trimmed values, both PATCH media types, atomic mixed updates, omission, no-op timestamps, scope and malformed input.
- [x] 3.2 Add string slug to complete map responses and game map summaries across all producers; verify list/detail/mutation and game list/detail response tests expose the current slug, preserve ordering and leave owning game timestamps unchanged.
- [x] 3.3 Update OpenAPI string constraints and requiredness without exposing a Slug object schema; verify contract tests document required creation values, optional PATCH presence, null rejection, response slug and 409 failures, and existing game slug contract tests pass.
- [x] 3.4 Update API conventions, resource READMEs and HTTP examples for name/slug creation, metadata PATCH and map summaries; verify examples match the updated contracts and documentation links pass node scripts/check-docs.mjs.

## 4. Existing admin integration

- [x] 4.1 Regenerate Maps and Games clients through their existing generate-api Nx targets against the updated running API; verify generated request/response strings and summary fields match OpenAPI and handwritten consumers compile without manually editing generated files.
- [x] 4.2 Add required manual slug input and matching validation to map creation and editing in the existing list dialog; verify manually that create sends both fields, editing sends changed fields only, name changes do not generate slug, conflicts preserve input and unchanged forms send no PATCH.
- [x] 4.3 Add slug editing to Settings & Tools and adapt affected cache transformations/consumers; verify manually that slug-only edits persist, draft/pending/error behavior is retained, authoritative responses update existing views and navigation/cache identity stays ID-based.
- [x] 4.4 Update affected frontend consumer documentation, format changed files, run affected Nx lint targets and production builds for consuming applications; verify those checks pass and document manual form checks. Preserve existing test coverage and do not add or run frontend unit/E2E tests under the current deferral policy.

- [x] 4.5 Preserve nullable value-type PATCH schemas exposed by client regeneration and adapt Games sorting imports to the current generated names; verify category reset/nullability and game sorting contract tests, regenerate clients and pass affected lint and the admin production build.

## 5. Integrated verification

- [x] 5.1 Build GameAtlas.slnx and run focused backend regression suites for game/map domain behavior, persistence, migrations, API and OpenAPI; verify unchanged game slug behavior, populated upgrade safety, map slug round-trips and no unintended processing or identity changes.
- [x] 5.2 Validate the completed change with openspec validate add-map-slugs, run node scripts/check-docs.mjs and git diff --check, and review the final scope; verify artifacts match delivered behavior and report actual checks and any remaining limitations.
