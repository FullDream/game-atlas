# Tasks

## 1. Domain and persistence

- [x] 1.1 Add nullable release date, developer, plain-text description and banner relationship; rename cover terminology and support atomic updates/no-ops. Verify focused backend tests cover normalization, clearing, required cover and timestamps.
- [x] 1.2 Add a migration preserving existing cover references and metadata while initializing new values to null. Verify fresh database creation and upgrade with existing games, maps and content using migration coverage and explicit temporary-database application.
- [x] 1.3 Include banner references in media usage/deletion protections and preserve shared assets on replacement/deletion. Verify focused persistence tests cover shared cover/banner references and rejected deletion of referenced assets.

## 2. Game API and projections

- [x] 2.1 Extend create/PATCH contracts with coverId, bannerId and nullable descriptive metadata, including merge-patch omission/null semantics. Verify backend contract tests for valid requests, invalid dates/references, legacy field rejection, atomic failures and no-op timestamps.
- [x] 2.2 Return consistent expanded GameDto from create/list/get/update with canonical cover/banner, isolated counts and ordered map summaries using bounded reads. Verify tests for empty/multiple games, zero counts, categories without markers, ready-style transitions and preserved pagination without per-game reads.
- [x] 2.3 Document the expanded OpenAPI response, read-only fields and canonical nullable ImageAsset references; update game README, HTTP examples and API conventions. Verify focused schema tests, documentation links and removal of obsolete seven-field claims.

## 3. Client generation and metadata editing

- [x] 3.1 Regenerate Games clients explicitly from running backend OpenAPI and migrate all image/imageAssetId consumers. Verify generated contracts expose coverId/bannerId, nullable metadata and map summaries; run affected lint and consuming production builds.
- [x] 3.2 Extend create/edit form state, dirty patches, reset/discard and navigation protection for metadata and banner selection/removal. Verify manually that saved values survive reload, cleared optional fields send null, failures retain input and unchanged forms send no patch.
- [x] 3.3 Enable plain-text description and keep all formatting buttons disabled; remove obsolete placeholder guidance. Verify manually that multiline text saves and renders safely and release dates survive without timezone shifts; run formatting, affected lint and production build.

## 4. Real content in game views

- [x] 4.1 Replace card placeholders with cover/banner/description, numeric marker/category counts and real map previews/names, deriving map count from maps.length. Verify grid/list layouts manually with empty, populated and null-image data, then run affected lint and production build.
- [x] 4.2 Replace Maps tab placeholder/count with read-only summaries and empty states, retaining disabled unimplemented actions. Verify direct navigation/reload and maps with no ready style; update affected feature documentation and check its links.

## 5. Integration verification

- [x] 5.1 Build GameAtlas.slnx and run focused backend suites spanning game responses, media references and migrations. Verify an existing game retains its cover and timestamps while exposing current counts and previews.
- [x] 5.2 Run affected frontend formatting/lint and consuming production builds without unit/E2E suites. Manually verify create/edit/list flows, banner clearing and default-style preview changes after refresh; record results and limitations.
- [x] 5.3 Run documentation checking, git diff --check and OpenSpec validation. Verify the delivered behavior matches both delta specs and all tracked implementation tasks have evidence before completion.

## Verification evidence

- Backend solution builds. Domain game coverage: 123 passed; Application suite: 95 passed. Focused game API/schema/media-reference and migration checks passed, with follow-up runs for updated SQL-count expectations and metadata/default-style previews.
- A fresh temporary SQLite database was explicitly migrated. Upgrade coverage preserves populated games, covers, maps and timestamps; EF reports no pending model changes. The working database was not migrated.
- Games clients were regenerated over HTTP from the development OpenAPI document. All five affected frontend projects pass Nx lint; admin production build passes its existing budgets. Initial-bundle and Media CSS size warnings remain.
- Browser smoke checks against a temporary database covered creation with cover/banner selection, direct reload, date and plain-text description persistence, disabled formatting, null clearing, Maps summaries, list/mobile rendering, failed-save input preservation, pristine state after save and navigation guards. Native date value handling was adjusted to avoid Signal Forms validity changes marking a saved field dirty.
- Frontend unit/E2E suites were not added or run. Browser checks are targeted smoke observations, not exhaustive accessibility or concurrency coverage.

A broader regression sample found an existing Media pagination test expecting 422 for limit=101 while the unchanged validator allows 1..120. This unrelated test mismatch remains; Media pagination behavior was not changed. Catalog map-read expectations were updated for the newly required summaries.
