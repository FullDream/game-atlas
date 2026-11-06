# Tasks

## 1. Marker data access

- [x] 1.1 Add scoped marker list keys and MarkersQueryOptions using existing generated transport and toPromise; verify full list retrieval, error propagation and map isolation against the running API.
- [x] 1.2 Add create/update/delete mutation options with authoritative sorted cache reconciliation, stale-read cancellation and per-marker conflict guards; verify manual create/edit/delete round trips and no empty PATCH.
- [x] 1.3 Reconcile category/group counts and category/group cascades using exact query invalidation; verify transfers, deletion counts and removal of affected markers without unrelated cache invalidations.
- [x] 1.4 Update maps/data-access README with cache ownership and retry limitations; verify its documented behavior matches the delivered options.

## 2. Shared map interactions

- [x] 2.1 Extend viewport presentation types and OpenLayers engine with stable-ID marker features, icon/color fallback, selection and pending appearance; verify style/category changes and no-ready-style geometry preserve the camera and markers.
- [x] 2.2 Expose mode-aware selection, placement and release-coordinate events with hit-tested translation and empty-space pan; manually verify controls/pans do not create points and unchanged/cancelled drags emit no save.
- [x] 2.3 Dispose marker resources and reject obsolete map gestures; verify navigation, remounting and map changes do not duplicate callbacks or update the wrong map.
- [x] 2.4 Update maps/ui README with marker inputs/events and lifecycle guarantees; verify public exports remain API-client and admin independent.

## 3. Adaptive marker form

- [x] 3.1 Inspect installed Spartan components and generate only missing Drawer, Sheet and Carousel entrypoints in existing shared UI; verify imports resolve in the consuming admin build without extra projects or icon families.
- [x] 3.2 Use native Drawer handle/swipe dismissal without custom snap points or fullscreen expansion. Implement shared marker draft/form and adaptive bottom Drawer/right Sheet with visible actions, focus management and dirty-dismiss/navigation protection; verify resize, backdrop, swipe, Escape and route departure preserve or explicitly discard drafts.
- [x] 3.3 Add validated name/plain-text description/X/Y and grouped icon-bearing category selection; verify field boundaries, colored fallback, loading/errors and category-empty guidance manually.
- [x] 3.4 Compose image picker and manual photo carousel with detach/reorder controls and preservation of existing videos; verify attachment order survives save/reload and underlying media assets are untouched.
- [x] 3.5 Wire selection, changed-field saving and confirmed deletion, plus a keyboard-reachable marker selector and coordinate-edit alternative; verify keyboard access, retained failed drafts and authoritative saved values.
- [x] 3.6 Update frontend UI guidance and feature-editor README for adaptive panel, icon selection and draft behavior; verify documentation distinguishes delivered behavior from exclusions.

- [x] 3.7 Keep the selected marker above the mobile drawer by measuring its actual overlap and invoking a padded minimal-pan viewport reveal; verify obscured and already-visible markers, drawer-height/orientation changes, unchanged zoom/coordinates and no PATCH. Document this explicit camera-preservation exception in the viewport and editor READMEs.

## 4. Toolbar and immediate writes

- [x] 4.1 Compose bottom-centered Pan / Move marker / Add toolbar with single/repeated choices, labels, active state and safe-area layout; verify exclusive modes and no propagation of toolbar clicks into map placement.
- [x] 4.2 Wire single placement from click to draft and explicit create; verify cancel sends no POST, success returns to Pan and selects the result, and unavailable geometry/categories explain the prerequisite.
- [x] 4.3 Add repeated template form and placement session with template status, successful count and Finish/Escape; verify one create per intentional click, concurrent independent outcomes and settling requests after finishing.
- [x] 4.4 Show placement pending/error states with explicit retry only after definitive failure and reconciliation for uncertain outcomes; verify lost responses never trigger automatic POST retries or silent duplicate creation.
- [x] 4.5 Persist marker translation immediately on release with per-marker pending lock and failure restoration; verify exactly one changed-coordinate PATCH per drop, server-coordinate adoption and no separate save action.
- [x] 4.6 Update frontend architecture and feature-editor README to remove marker deferral and describe ownership, immediate writes and mode behavior; verify docs match implementation without expanding public-web scope.

## 5. Integration verification

- [x] 5.1 Format changed handwritten files, run resolved affected Nx lint targets and the admin production build; record actual results while preserving frontend unit/E2E deferral.
- [x] 5.2 Manually exercise desktop/mobile creation, repeated placement, movement, photo ordering, keyboard access, dirty dismissal, failures, category cascades, resize, navigation and styles without ready tiles; record outcomes and limitations.
- [x] 5.3 Run the repository documentation checker, git diff --check and OpenSpec validation; verify only intended files changed and no generated transport was edited manually.

## Verification notes

- API checks against the local backend passed: create/list, recursive coordinate PATCH, category transfer, authoritative counts, sibling-map isolation and cleanup.
- Standalone browser interaction checks in headless Microsoft Edge passed on desktop and a 390x844 touch viewport: single create, field save, two repeated placements, reload persistence, confirmed deletion, photo selection/reordering/detachment, retained library assets, movement with one PATCH on release and rollback after an intercepted 422.
- Mobile checks passed: draft retention across Sheet/Drawer breakpoint changes, selected marker visibly above the measured Drawer with no coordinate PATCH, native swipe/Escape/cancel protection and explicit discard. SPA navigation guards, category-group cascades and remounting with one viewport also passed.
- Validation rejected a 201-character template name. Intercepted failed and lost-response POSTs showed explicit retry or uncertain-outcome refresh guidance, with no automatic repeat. A scoped HTTP snapshot with styles=[] verified selection and placement without ready tiles while retaining logical geometry.
- Browser checks used temporary markers/groups and cleaned them up. The development-only TanStack Devtools floating control was hidden during interaction checks because it overlaps bottom-right editor actions. These are manual browser checks, not a frontend unit/E2E suite; frontend test-suite deferral remains in effect. Existing-video preservation was reviewed in the attachment draft/payload logic without creating video fixtures.
- Final checks passed: affected lint targets for maps-data-access, maps-ui, maps-feature-editor, ui-helm and admin; admin production build; handwritten-file Prettier checks; repository documentation checker; git diff --check; strict OpenSpec validation. Lint retains the pre-existing unused MoveCallbacks warning in map-categories.ts. Generated API sources are unchanged.

## 6. Marker navigation revision

- [x] 6.1 Replace toolbar marker/mode dropdowns with separate single/repeated creation buttons and add the independently scrolling left Markers accordion; verify pointer/keyboard selection, mobile map visibility and focus return.
- [x] 6.2 Update affected docs/specs and verify formatting, affected lint, admin production build and documentation/OpenSpec checks without unit/E2E suites.

Revision verification: affected feature lint and admin production build passed. Standalone browser checks at desktop 1400px and mobile 390px confirmed no toolbar selectors, separate single/repeated creation buttons, marker-row keyboard activation, section collapse/expand, focus return and mobile panel dismissal. GET-only marker response fixtures were used; no persisted marker data was changed.

## 7. Immediate viewport interaction

- [x] 7.1 Disable activation-click gating for pointer pan/wheel zoom while preserving keyboard focus semantics; verify wheel zoom before any map click and first drag after blur, affected lint, admin build and documentation/spec checks.

Immediate interaction verification passed in the browser: wheel zoom changed the map before any map click without taking focus, and the first drag after blur panned immediately. Maps UI lint, admin production build, Prettier, documentation check, strict OpenSpec validation and git diff --check passed. No unit/E2E suites were run.
