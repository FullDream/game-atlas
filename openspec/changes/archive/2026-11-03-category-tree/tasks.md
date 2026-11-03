# Tasks

Current implementation scope: complete backend and UI with success-only category tree invalidation. Task 5.3 (optimistic updates) remains explicitly deferred by the user; current behavior must not be presented as optimistic.

## 1. Consolidate category ownership

- [x] 1.1 Move CategoryGroup domain files into Domain/Categories, group application operations into Application/Categories/Groups and group API contracts into Api/Categories/Groups; consolidate endpoint registration and update all namespace references, including tests and migration designers without changing historical schema operations. Verify with dotnet build GameAtlas.slnx and a diff showing unchanged table names and marker foreign keys.
- [x] 1.2 Update docs/architecture.md, docs/backend.md where affected and category API READMEs to describe the unified functional area and separate entities. Verify local links with node scripts/check-docs.mjs from the repository root.

## 2. Optional icons and safe persistence migration

- [x] 2.1 Make category icon references nullable and add optional group image references, preserving Optional PATCH presence and resolving only non-null IDs. Update DTOs, mapping and validators. Verify focused domain/application tests for omitted, null, malformed, missing, incompatible and shared icons, including rename of iconless resources.
- [x] 2.2 Add the restrictive group-image FK/index and extend media deletion guards before physical deletion. Verify focused backend integration tests for protected group images, shared references, clearing references and media retained after group/category cascade deletion.
- [x] 2.3 Add a forward migration for nullable icons and normalized existing sibling order, preserving order/createdAt/id sequence, IDs, timestamps and marker/media relationships. Verify fresh and populated SQLite upgrade tests, duplicate/gapped/Int32.MaxValue fixtures and explicit rejection of unsafe downgrade with iconless categories.
- [x] 2.4 Document migration application and downgrade limitations in the persistence/category READMEs and affected API guidance. Verify commands match configured EF tooling and documentation links pass.

## 3. Atomic relative placement

- [x] 3.1 Replace writable order with beforeId in creation and PATCH contracts, preserving omission versus null and existing current-group routes. Implement destination/anchor validation and entity no-op semantics. Verify focused validation/domain tests for self/foreign/malformed anchors, rejected numeric order and unchanged timestamps.
- [x] 3.2 Implement transaction-scoped placement and normalization for groups and categories, including same-map transfer and resulting-name validation. Verify focused integration tests for beginning/middle/end, same-parent no-op, omitted transfer placement, atomic rename-plus-transfer, source/destination order and sibling timestamp preservation.
- [x] 3.3 Verify concrete SQLite transaction serialization for structural operations, extending the existing transaction implementation only if required. Add and run focused concurrent append/move/delete and rollback tests proving no duplicates, lost members, partial writes or cross-map effects.
- [x] 3.4 Update docs/api-conventions.md and category READMEs with beforeId behavior, server-owned order, validation statuses and unchanged mutation routes. Verify examples against the updated integration API tests and run documentation checks.

## 4. Unified tree API

- [x] 4.1 Extend ListCategoryGroups to retrieve a consistent complete tree with nullable canonical images and computed category/group counts using bounded projected queries and pure mappings. Return complete groups from POST/PATCH. Verify focused integration tests for empty trees/groups, populated groups, count changes, current image metadata, ownership and maps without styles.
- [x] 4.2 Apply the Categories tag to all seven supported operations and remove ListCategories and its GET mapping while retaining category POST. Verify OpenAPI tests for operation IDs, one tag, nullable icons, beforeId presence, read-only fields, plain tree array and 405 for the removed GET.
- [x] 4.3 Update category API READMEs and contract documentation to the delivered tree shape and operation set. Verify sample responses against tests and run node scripts/check-docs.mjs.

## 5. Frontend transport and tree cache

- [x] 5.0 Prepare the existing per-group category query options with success-only map-scoped invalidation, public exports and consumer documentation. Verify maps-data-access typecheck/lint, production admin build, formatting and documentation checks. This preparatory step does not complete the tree query migration in 5.2 or the optimistic behavior in 5.3.

- [x] 5.1 Update maps Orval tag filtering, regenerate through the uncached maps-data-access generate-api Nx target against the running backend and migrate generated exports/consumers. Verify the unified Categories service, removal of the separate group service and ListCategories, and a second unchanged generation producing no meaningful diff.
- [x] 5.2 Evolve the prepared categories query-key, variables and query-options files into a map-scoped tree query with group/category mutation factories, explicit option typing and cancellation. Verify affected Nx lint and inspect requests to confirm one tree read and no per-group reads.
- [ ] 5.3 Add pure tree transformations and optimistic rename/placement with counts, per-map mutation scope, authoritative reconciliation, safe rollback and scoped invalidation for ambiguity. Keep UI notifications in feature callbacks. Verify affected Nx lint and manually exercise successful/failed transfers, stale reads, missing baselines and retained successful changes; do not add or run frontend tests.
- [x] 5.4 Update maps data-access README and docs/frontend/data-access.md for tree keys, supported factories, tag ownership and cache behavior. Verify examples match exported types and documentation links pass.

## 6. Left-panel category management

- [x] 6.1 Add a compatible Angular Aria dependency and compose MapCategories with the existing editor aside below styles. Add named tree rows, optional icons, counts, expansion and local loading/empty/error states using project tokens and Spartan/shared UI. Verify affected Nx lint, production admin compilation and manual desktop/narrow-screen rendering.
- [x] 6.2 Add group/category Signal Forms dialogs with existing media picker integration, optional icon clearing, required category color, destination selection and dirty PATCH submission. Add header/group creation actions and explain the no-group prerequisite. Verify manually that valid saves reconcile the tree, invalid saves preserve input, no-op saves send no PATCH and cancellation sends no write.
- [x] 6.3 Add row menus and confirmed cascading deletion with accurate media-retention copy and post-success focus restoration. Verify manually that failure/cancellation retains items and success updates membership/counts without deleting library media.
- [x] 6.4 Compose typed CDK root/child drag lists and labelled handles, including empty/collapsed-group destinations, and map drops to beforeId. Prevent conflicting submissions while category mutations are pending. Verify manually that changed drops persist, unchanged drops send no request, invalid nesting is blocked, failures reconcile counts/order, and style selection/camera remain unchanged.
- [x] 6.5 Add keyboard move-up/down and move-to-group actions; verify arrow navigation, expansion, nested menu focus, dialog return focus, touch scrolling, drag-tooltip suppression, dark/light themes and reduced-motion behavior manually.
- [x] 6.6 Update feature-editor README, docs/frontend/architecture.md and docs/frontend/ui.md to describe the category section and controls. Verify documentation links, affected Nx lint and production admin build; preserve frontend test deferral.

## 7. Integrated acceptance

- [x] 7.1 Run dotnet build GameAtlas.slnx and the focused backend suites affected by the complete change. Run formatting, resolved affected Nx lint targets and the production admin build. Verify that committed generated sources permit the build with the backend offline; record actual results and limitations without running frontend unit/E2E suites.
- [x] 7.2 Manually exercise the integrated path: create two groups and iconless/icon-bearing categories, move/reorder with pointer and keyboard, reject a duplicate-name transfer, clear an icon, reload and delete populated content. Verify persisted order, group/category counts, media retention and uninterrupted styles/viewer behavior.
- [x] 7.3 Run documentation checks, git diff --check and openspec validate category-tree --strict. Review the diff for preservation of user-prepared files, complete moves and absence of unrelated changes; record any unavailable runtime verification explicitly.

## Verification record (2026-11-01)

- Backend solution build passed. Focused suites passed: 29 domain, 29 application and 162 integration tests (220 total).
- Maps data-access and feature-editor lint, handwritten frontend formatting, and the production admin build passed. The final frontend build ran with the preview API stopped; generated transport sources are reproducible.
- Documentation checks and strict OpenSpec validation passed. Diff whitespace checks passed with Windows CRLF handling.
- Browser walkthrough covered tree loading and expansion, CRUD and failed saves, media selection and icon clearing, pointer and menu placement, focus restoration, marker counts, cancellation and populated deletion. Desktop, narrow-screen, dark theme and reduced-motion rendering were inspected. Native-device touch behavior was not verified. Frontend unit/E2E suites remain deferred.
- Only temporary databases were migrated. Apply CategoryTree to the working database before using the updated application.
- Task 5.3 is intentionally deferred by the user. Mutations currently invalidate the map tree after success; optimistic transformations, rollback and mutation scopes remain for the next step.

## Follow-up: category controls and group colors

- [x] Add Spartan Button Group creation controls and icons to category menus.
- [x] Add persisted group color, merge-patch validation, migration and regenerated API models; expose color in the editor and tree.
- [x] Verify focused backend tests, affected frontend lint/build and browser behavior. Optimistic updates remain deferred.

Follow-up verification: 65 focused integration tests passed, affected lint and production admin build passed. Browser checks confirmed split controls, menu icons, group color editing and persistence after reload. CategoryGroupColor was applied to the working database after backup; both existing groups and database integrity were preserved. Task 5.3 remains deferred.

Drag-and-drop follow-up: header append targets hide inserted placeholders to preserve row layout. Only the active header receives a muted target outline; insertion placeholders match map styles and source anchors remain invisible while retaining geometry. Browser verification confirmed a 303x36 header before and during drag, a hidden header placeholder and a persisted cross-group transfer. Feature-editor lint and production admin build passed.
