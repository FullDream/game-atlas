# games-feature-list

Admin-only Games listing, tagged type:feature, scope:games, platform:admin. Import GAMES_ROUTES from @game-atlas/games/feature-list. Admin mounts this library at /games and independently composes [creation](../feature-create/README.md) at /games/new and [editing](../feature-editor/README.md) at /games/:id; feature libraries do not import one another.

- /games lists covers, names, slugs and statuses. Search, sorting and offset/limit pagination live in URL query parameters.
- Image selection uses the ready-made shared MediaPickerDialog. Its shared port is bound to MediaPickerAdapter in the admin application; Games imports no Media libraries.
- Maps UI and the public web catalog are outside this feature.

Verification uses Nx lint and the admin production build. Frontend unit/E2E test work remains deferred.

## Verification

Browser checks against the backend covered draft creation, direct detail reload, fresh-list initialization without a duplicate detail GET, stale-detail refetch, cancelled detail requests, cover replacement, publication, slug-conflict rollback with retained form input, rejected create/delete operations, unsaved-change protection, filtered-list reconciliation, out-of-range pagination, not-found pages, mobile width and keyboard focus order. A background detail refetch preserved dirty editor input. Temporary verification games were removed; existing games and library files were preserved.

Media move-mode folder browsing and cancellation were checked through the existing Media feature. Nx lint and the admin production build cover compilation and module boundaries. Frontend unit/E2E suites remain deferred, and these browser observations do not establish exhaustive concurrency or accessibility coverage. Existing initial-bundle and Media CSS budget warnings remain.

## Reference layout and placeholders

Games now offers Grid/List cards with a cover, banner placeholder, unavailable statistics (shown as dashes), map preview placeholders and actions. Manage maps opens the editor's Maps section. Existing game metadata and the cover picker continue using the API.

The Games list uses named container queries for its toolbar, search, view switch and card layout. The view switch appears only when the container can display horizontal list rows; below that width both modes use the same grid. Grid column counts are 1, 2, 3, 4 or 6, which divide all supported page sizes (24/48/72/96). Three columns start at a container width of 68rem, including a 1920×1080 display at 125% scaling with the sidebar open. The default page size is 24; Games API limits requests to 100, so the largest offered size is 96. Skeleton count follows the selected page size.

Games lists and both form pages reserve scrollbar space with scrollbar-gutter: stable. Media lists, its details drawer and both picker dialogs reserve the same stable gutter on their actual scroll containers.

A failed background list refresh preserves cached cards and shows a compact error with Retry above the results, matching Media. Initial failures without data show the full error state; retrying successfully clears the error.

The Grid/List view selector uses Spartan Toggle Group with single, non-nullable selection, bound directly to the view signal. It stays hidden when the container cannot display horizontal list rows.
