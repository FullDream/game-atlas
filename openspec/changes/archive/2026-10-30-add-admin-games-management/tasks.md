## 1. Backend game retrieval

- [x] 1.1 Add the GetGame Application query, no-tracking handler and not-found error using existing DTO/image mapping; verify the solution builds.
- [x] 1.2 Register GET /games/{id} and its Games-tagged OpenAPI response/error contract; add focused backend integration/OpenAPI tests for both statuses, canonical image, missing/deleted and malformed IDs, and read-only behavior; verify the focused tests pass.
- [x] 1.3 Update relevant API documentation for retrieval; verify the documented response matches the endpoint.

## 2. Games transport and cache

- [x] 2.1 Regenerate Games transport through its Nx generate-api target against the running backend and export the detail operation; verify generated output stays domain-local and ordinary compilation needs no backend.
- [x] 2.2 Extend the user's prepared key helpers and GamesQueryOptions with normalized list prefixes and getById initialData/initialDataUpdatedAt from the freshest matching list; verify affected Nx lint and admin compilation and manually check fresh/stale/no-list navigation and request cancellation.
- [x] 2.3 Add safe optimistic detail editing, same-game mutation coordination, authoritative success replacement and failure rollback; verify manually that name/status/image edits and failure preserve correct data and do not overwrite newer successful writes.
- [x] 2.4 Seed detail after create and remove it only after successful delete; reconcile affected list pages and prevent stale list reseeding; verify manually with multiple cached searches/pages and failed create/delete.
- [x] 2.5 Update games/data-access README to describe the current public options API and lifecycle; verify examples match implementation. Preserve existing coverage and do not add or run frontend tests.

## 3. Reusable media picker

- [x] 3.1 Extract ready-to-use shared confirmation, thumbnail and picker components; use a shared query port with a Media adapter provided by the application; verify resolved tags and Nx lint without cross-domain imports.
- [x] 3.2 Wire Media to the extracted picker and document its consumer contract; verify Media image selection, cancellation, folder navigation and move mode manually and build admin.

## 4. Admin Games pages

- [x] 4.1 Generate the admin-only Games feature-list, lazy routes and shared header integration; verify resolved tags, /games, /games/new and /games/{id} direct navigation and admin production build.
- [x] 4.2 Implement responsive list cards, URL search/sort/pagination, loading/error/empty states and explicit pagination correction; verify browser back/forward and empty last-page behavior manually and run feature lint.
- [x] 4.3 Implement the shared Signal Forms editor with required metadata, explicit slug validation, library image selection and dirty-field PATCH submission; verify create-to-detail navigation, save/discard, conflict/error preservation and keyboard operation manually.
- [x] 4.4 Add publication controls, unsaved-change/submission navigation protection, detail not-found/retry states and confirmed cascading deletion; verify direct reload, failed mutations, background refetch during dirty editing and successful deletion manually.
- [x] 4.5 Update frontend architecture, admin setup and feature documentation to describe implemented Games routes and picker boundaries; verify documentation matches actual behavior and run affected Nx lint plus admin production build.

## 5. Integration verification

- [x] 5.1 Format changed supported files, run focused backend checks, affected frontend Nx lint and admin production build; record results without adding or running frontend unit/E2E suites.
- [x] 5.2 Run the documentation checker and git diff --check; verify user changes remain preserved and all delivered artifacts/code are within the change scope.
- [x] 5.3 Exercise the complete Games flow with the backend, including cached and uncached detail entry, optimistic failure rollback, image selection and cascading-delete confirmation; record observed behavior and remaining runtime limitations.
