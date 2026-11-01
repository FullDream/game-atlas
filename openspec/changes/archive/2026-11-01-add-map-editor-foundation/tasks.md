# Tasks

Frontend unit/E2E work remains deferred. Use the checks and manual scenarios below without adding or running those suites.

## 1. Shared map foundation

- [x] 1.1 Use nx-generate guidance to scaffold non-buildable maps/ui and maps/feature-editor with the type/scope/platform tags from design.md; install ol through npm. Verify resolved Nx configurations, package linking and boundary tags.
- [x] 1.2 Implement the presentation contract and browser-only OpenLayers lifecycle in maps/ui, including pixel Projection, explicit TileGrid, XYZ wrapX:false and style background. Verify ready tiles on a rectangular map with fractional extent edges and verify teardown on navigation.
- [x] 1.3 Add pan, zoom, fit, fullscreen, resize handling and ready-style selection using existing Spartan controls; verify camera preservation on selection, explicit fit, unsupported fullscreen and minZoom=maxZoom maps.
- [x] 1.4 Add blank/no-ready-style states and safe selection fallback; verify no requests for unfinished styles and removal of the active/final style without coordinate-space reset.
- [x] 1.5 Document the maps/ui consumer contract and lifecycle in its README and frontend architecture guidance; verify lint for the new UI project and admin production compilation once consumed.

## 2. Style transport and cache behavior

- [x] 2.1 Add updateStyle/deleteStyle mutation options using existing generated endpoints, toPromise, per-map scopes and metadata optimistic rollback. Verify successful PATCH replacement, unchanged style order, cancelled stale reads and preservation of other successful writes after failure.
- [x] 2.2 Reconcile complete-map creation responses, style-only update responses and void deletion responses in detail/list caches, including default derivation from server style order. Verify active/default/final deletion and no unconditional status refetch.
- [x] 2.3 Apply editor snapshot query settings without sockets, polling or focus/reconnect refresh; verify statuses remain last loaded until deliberate route loading/retry or known mutation results.
- [x] 2.4 Update maps/data-access README for new options and snapshot/cache semantics; run its configured Nx lint target and verify exported types through admin compilation.

## 3. Editor workspace and style actions

- [x] 3.1 Compose /games/:gameId/maps/:mapId/edit before the generic game route and outside game settings children; enable semantic Open editor links. Verify direct URL, refresh, back/forward, contextual breadcrumbs and absence of game settings actions in the map editor.
- [x] 3.2 Build the left style list and central viewer with synchronized selection, thumbnails, dimensions, default indicator and status badges; verify empty/loading/error/missing-resource states and snapshot copy without percentages or bars.
- [x] 3.3 Add responsive style-panel access and named disabled search/filter placeholders with Spartan primitives; verify desktop proportions, narrow-screen access, keyboard focus and no placeholder requests.
- [x] 3.4 Implement Add style dialog with Signal Forms, library picker, supported-image/dimension checks and backend error presentation; verify valid creation, incompatible image, duplicate name, cancellation and duplicate-submit prevention.
- [x] 3.5 Implement metadata editing for name/backgroundColor with dirty-field PATCH and shared confirmation for deletion; verify no-op save, optimistic rollback, input retention on errors, library-asset preservation messaging and active-style background updates without camera movement.
- [x] 3.6 Update maps feature READMEs and frontend architecture/data-access documentation to describe implemented editor behavior and deferred features; run lint for editor, list, admin and shared UI if changed, and verify admin production compilation.

## 4. Hosting and integration verification

- [x] 4.1 Add unchanged /map-tiles/** forwarding to the admin development proxy and document production tile routing in the admin README; verify a real tile GET resolves through the frontend origin without API prefix rewriting.
- [x] 4.2 Format changed supported files, run affected Nx lint targets and admin/web production builds, node scripts/check-docs.mjs from the repository root, git diff --check and strict OpenSpec validation; record actual results and limitations.
- [x] 4.3 Manually review the complete editor against both delta specs and the sketch: route loading, ready-style switching, CRUD, failure/cancel paths, empty/processing-only maps, resize/fullscreen, keyboard/mobile access and no automatic processing refresh. Record unavailable runtime checks instead of treating compilation as interaction verification.

## Verification record

- Frontend unit/E2E suites were neither added nor run.
- All six affected Nx lint targets passed: maps-ui, maps-feature-editor, maps-data-access, maps-feature-list, admin and ui-helm.
- Normal production builds passed for admin and web (web reused its valid Nx cache). Admin initial output is approximately 1.01 MB. Approved admin budgets warn/fail at 1.2 MB / 1.5 MB for the initial bundle and 6 KB / 8 KB for component styles. Web budgets remain unchanged. All affected lint targets, Maps typecheck, documentation checks, whitespace checking and strict change validation passed; task 4.2 is complete.
- Manual browser inspection used Edge and real backend HTTP calls against a temporary 640 x 783 review map. Ready tiles returned 200 through the frontend proxy; style creation, metadata updates, duplicate-name rejection and optimistic rollback, active/final deletion, cancellation and deletion failure were checked. Creation rejected incompatible dimensions before POST. A delayed request disabled submission and a second activation produced no additional POST.
- Ready-style switching preserved both the rendered camera transform and image pixels. Desktop/mobile layouts, resizing, fullscreen, unsupported fullscreen, keyboard focus containment/restoration, direct navigation, reload, history, 404 and explicit retry were checked without page errors. Focus/reconnect produced no metadata read.
- The single-level minZoom=maxZoom case used browser-substituted 33 x 31 metadata. Error responses and delayed/failing transport cases were also injected in browser inspection; these checks do not establish production hosting or a public-page hydration implementation.
- The temporary review map was deleted successfully and its original library image was verified to remain. Existing user maps were not changed.
- Documentation link checking, strict OpenSpec validation and git diff whitespace checking passed. Public product pages remain outside this change.
