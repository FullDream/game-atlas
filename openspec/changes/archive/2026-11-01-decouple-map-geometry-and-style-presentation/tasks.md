# Tasks

## 1. Deferred logical geometry and persistence

- [x] 1.1 Change Map creation and geometry invariants to name-only creation and paired nullable dimensions; initialize on first committed attachment and preserve geometry after deletion/failure. Update MapTests and geometry-related backend tests; verify focused domain tests cover both resolution orders, odd dimensions and preexisting finite marker coordinates.
- [x] 1.2 Add persisted style display order and effective default independent of creation sequence, with append, ready fallback and deletion rules. Verify focused domain tests cover late readiness, manual selection, reordering and removal of the final ready style.
- [x] 1.3 Add EF mappings and a new non-destructive migration backfilling existing order/defaults while preserving all dimensions. Verify a populated-store migration test retains maps with and without styles, markers and source references; verify no startup migration or applied migration rewrite is introduced.
- [x] 1.4 Update first-attachment transaction coordination for competing requests and rollback. Verify integration tests cover failed attachment leaving geometry null, simultaneous first attachments establishing one frame, and preserving geometry after a first-style processing failure.

## 2. Source-native processing and map API

- [x] 2.1 Update worker claims, source validation, manifest validation and tile generation inputs to source geometry. Verify processor integration tests handle sources both larger and smaller than logical dimensions and preserve native levels, transparent edges, existing ready files and static URLs.
- [x] 2.2 Update CreateMap contracts/validation and nullable map mapping; expose style tileGeometry consistently in HTTP and full style notifications. Verify API/OpenAPI tests cover name-only creation, obsolete field rejection, uninitialized responses and per-style grids without source replacement.
- [x] 2.3 Extend map PATCH with Optional defaultStyleId/styleOrder, atomic complete-permutation validation and no-op timestamps. Verify API tests cover both media types, malformed shapes, nulls, duplicates, stale membership, foreign IDs, non-ready defaults and combined name/default/order rollback.
- [x] 2.4 Integrate persisted fallback with ready publication/deletion and use the effective default in game/map preview queries. Verify integration tests cover readiness/deletion races, restart persistence, unchanged creation sequence and consistent preview/default responses.
- [x] 2.5 Update map transport/OpenAPI descriptions, API conventions and infrastructure map README to describe deferred logical geometry, native tile geometry, stretching, default/order changes and explicit migration/rollback. Verify documented examples match generated OpenAPI and run the documentation checker.

## 3. Angular contracts and map creation

- [x] 3.1 Explicitly regenerate the affected API clients from the updated running API and adapt query-options/cache reconciliation for presentation PATCH fields and nullable geometry. Verify generated contracts match OpenAPI and affected projects lint and consuming applications compile.
- [x] 3.2 Remove manual map dimension inputs and obsolete validation/error/helper text. Render a pending-first-style label for uninitialized maps in creation/list/edit consumers. Update feature-list documentation; verify production compilation and manually create, rename and reload an uninitialized map.
- [x] 3.3 Remove exact-size style validation and add the non-blocking aspect-ratio warning using cross-products. Update feature-editor documentation; verify manually that differing ratios warn while allowing submit, equal ratios at different resolutions do not warn, and the first source has no ratio warning.

## 4. Viewer stretching and camera stability

- [x] 4.1 Extend maps UI inputs with native style tile geometry and initialize the engine only once logical geometry exists. Verify SSR-safe consuming builds and manually inspect initial empty, queued-only and final-style-deleted states.
- [x] 4.2 Implement forward/inverse source-to-logical affine projection transforms, native tile grids, logical clipping and disposal in MapEngine. Verify a browser rendering check aligns center, corners and off-center anchors for 8000/1000 sources in both initialization orders, unequal ratios, odd dimensions and transparent padded edges.
- [x] 4.3 Decouple camera zoom constraints from individual source grids and preserve local selection, center and resolution across source/default/order changes. Verify manually that switching styles at multiple zooms never resets/clamps the camera, larger later sources retain detail, and fit/fullscreen/remount still work. Update maps UI documentation and run affected lint/production builds.

## 5. Default and ordering interactions

- [x] 5.1 Add a ready-only Make default card action with pending/error handling and authoritative cache updates. Verify manually that reload and previews use the selected default while current local viewer selection/camera stay stable; update feature-editor documentation and run affected lint/build checks.
- [x] 5.2 Add CDK drag/drop ordering initiated only by a dedicated labelled card handle, plus keyboard Move up/down actions. Submit full ID arrays for changed orders, disable conflicting pending actions and roll back failures. Verify pointer/keyboard persistence, all processing states, no-op drops, error recovery and independent card selection; update documentation and run affected lint/build checks.

## 6. Integrated verification

- [x] 6.1 Build the backend and run focused map domain/application/integration suites covering the changed contracts, populated migration and processor behavior. Record the commands and results; do not run or add frontend unit/E2E suites under the current project policy.
- [x] 6.2 Run affected frontend formatting/lint/production builds and a manual browser acceptance pass combining name-only creation, different-resolution styles, ratio warnings, reorder/default persistence, deletion fallback and unchanged stored marker positions. Record actual checks and any runtime limitations.
- [x] 6.3 Run OpenSpec validation, the documentation checker and git diff --check; verify only intended files changed and no unrelated media limits, live processing discovery or source replacement were introduced.
