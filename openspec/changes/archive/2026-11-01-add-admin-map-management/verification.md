# Verification

## Backend

- `dotnet build GameAtlas.slnx --no-restore --nologo --verbosity quiet`: passed. One pre-existing nullable warning remains in CategoryGroupApiTests.
- Domain filters MapTests and MapRenameTests: 30 passed.
- Application filters MapValidationTests and UpdateMapValidationTests: 4 passed.
- Integration filters UpdateMapTests, MapOpenApiTests and MapAcceptanceTests: 14 passed.
- Integration filters MapApiTests, MapQueryTests and MapLifecycleTests: 30 passed.
- Existing OpenAPI coverage now expects nine map operations. New coverage checks name-only PATCH, merge-patch media/charset, validation errors, scoped ownership, concurrent duplicate names, complete responses and no-op timestamps.

## Frontend

- Affected Nx lint: maps-data-access, games-data-access, shared-util, games-feature-editor, maps-feature-list and admin passed together; ui-helm passed after the final blank-line correction.
- Maps data-access typecheck passed. API generation runs explicitly over local HTTP OpenAPI and retains generated sources.
- Admin production build passed with the configured limits unchanged; initial output is 999.55 kB. Existing initial-size warning and Media component CSS warning remain. The initial bundle has very little remaining budget.
- Games route composition and Maps providers are lazy application modules. Workspace libraries use generated TypeScript path aliases, not npm workspace packages, so package-manager workspace linking does not apply.
- Temporary Chrome smoke checks exercised actual create/rename/delete requests on a temporary map, then removed it. Initial list GET was followed by exactly one POST, PATCH and DELETE, with no post-mutation status reads. Focus and online events caused zero additional map-list reads.
- Additional isolated browser previews checked queued style snapshots, failed-read Retry, failed creation retaining input, read-only edit dimensions, unsaved General values across tab navigation and CRUD, map tab counts 0 -> 1 -> 0, General-only Save changes, header restoration on Media/Games navigation, modal focus containment/restoration and a 390px viewport without horizontal overflow. No uncaught page errors were observed.
- Browser previews used local request interception for error/status cases; they are not frontend unit/E2E suites. No frontend test files or suites were added or run. Visual checks do not establish every browser or assistive-technology behavior.

## Review

The additive endpoint requires no database migration. No sockets, polling, editable dimensions, Main badge, fabricated per-map counts or working map viewer were introduced. Open editor stays disabled. The change remains unarchived for review.

## Simplified detail invalidation

The shared map-change token, event types and Games summary adapter were removed after review. Admin now uses its application MutationCache success callback to invalidate only the exact game-detail query for successful map create/update/delete. Games list caches are left untouched; Maps still reconciles its own mutation responses without list refetches.

Affected admin, games-data-access, maps-data-access and shared-util lint passed. Admin production build passed with existing budgets unchanged. A new isolated Chrome smoke check observed exactly three game-detail GETs for create/update/delete, only one initial Maps list GET and one initial Games list GET, correct map tab counts, preserved unsaved General input and no uncaught page errors. These checks supersede the previous cross-domain cache-reconciliation approach. No frontend unit/E2E suites were added or run.
