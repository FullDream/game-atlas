# Tasks

## 1. Backend name-only map PATCH

- [x] 1.1 Add domain rename behavior preserving immutable geometry and no-op timestamps; extend focused MapTests for trimming, validation, case changes and actual/no-op updatedAt behavior and run them.
- [x] 1.2 Add UpdateMap command, Optional name validation and handler with scoped reads, existing mutation lock/transaction and game-scoped uniqueness excluding self; add and run focused application validation tests.
- [x] 1.3 Register the PATCH request, mapping and merge-patch endpoint returning the complete map; add and run focused integration tests for success, no-op, 400/404/409/415/422, charset variants, foreign ownership, immutable geometry/content and concurrent name conflicts. Verify OpenAPI exposes UpdateMap with optional name and no mutable dimensions.
- [x] 1.4 Update API Maps README and API conventions for the ninth operation, rename semantics and unchanged game timestamps; verify documented routes against endpoint registration and run the documentation checker.

## 2. Maps transport and cache integration

- [x] 2.1 Regenerate Maps-only transport from the running backend through maps-data-access:generate-api; verify the generated UpdateMap operation/request and run configured typecheck/lint targets without manually editing generated files.
- [x] 2.2 Add update/delete mutation factories and readable map error handling, with scoped optimistic rename/rollback, complete response merging, cancellation and success-only deletion; verify cache lifecycle by code review and affected Nx lint/typecheck, with no redundant post-mutation list read.
- [x] 2.3 Configure app-level mutation success handling to invalidate only the exact game-detail query after map create/update/delete; verify affected lint and confirm detail summaries/counts refresh without resetting unsaved fields or touching Games list caches.
- [x] 2.4 Update Maps data-access README with update/delete consumer API and snapshot limitations; format changed Markdown and verify documentation links.

## 3. Independent Maps feature and editor composition

- [x] 3.1 Generate maps/feature-list using installed Nx generation guidance, assign required tags and wire package dependencies with workspace package-manager commands; verify resolved project configuration and library public exports.
- [x] 3.2 Move game editor child-route composition to admin, export only required Games components/guard and remove its map placeholder; bind gameId to Maps through app composition. Verify direct /games/{id}/maps loading and lint confirms neither domain imports the other.
- [x] 3.3 Separate the game form from the Maps outlet while retaining General form ID, state and leave protection; verify tab switches preserve dirty values and map actions cannot submit the game form.
- [x] 3.4 Extend shared page-header contributions for nested actions and inherited/restored context; wire Save changes only on General and Add map on Maps. Verify breadcrumbs, loading and actions on tab navigation, route exit and existing Media/Games screens.
- [x] 3.5 Implement map rows/cards with dimensions, default-style preview fallback, style previews/counts, labelled status snapshots and creation/modification dates; implement matching skeleton, empty and error states. Verify responsive layout, absence of Main and unsupported counters, and stable server ordering.
- [x] 3.6 Implement Spartan create/edit modal with Signal Forms, editable name, creation-only dimension inputs, read-only edit dimensions and feature-owned mutation feedback; verify validation, duplicate-submit prevention, unchanged-save behavior and preservation of input on failure.
- [x] 3.7 Add row action menus and shared destructive confirmation for deletion, including map name, cascades and retained library media; verify cancellation sends nothing, errors preserve the row and success removes it. Keep Open editor disabled.
- [x] 3.8 Configure the Maps query snapshot policy without automatic status refreshes; inspect network behavior during pending styles, focus, reconnect and CRUD to verify no sockets, polling or status-driven GETs, while explicit error retry works.
- [x] 3.9 Document implemented app composition and Maps management in affected frontend architecture/library/admin documentation; format handwritten files, run affected Nx lint and configured typechecks and run the repository documentation checker. Do not add or run frontend unit/E2E tests.

## 4. Integration verification

- [x] 4.1 Build the backend solution and run relevant focused backend suites covering PATCH and existing map creation/deletion; record actual commands/results and any limitations.
- [x] 4.2 Run admin production build through Nx and manually verify modal keyboard/focus behavior, narrow layouts, direct route reload, dirty General navigation, cache summary counts/dates and no status refetches. Record checks performed without claiming deferred frontend test coverage.
- [x] 4.3 Run OpenSpec validation, documentation link checks and git diff --check; review the final diff for unrelated changes, generated-source reproducibility and enforced domain/platform boundaries.
