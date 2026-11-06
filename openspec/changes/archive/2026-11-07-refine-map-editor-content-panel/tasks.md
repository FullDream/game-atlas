# Tasks

## 1. Compact sections and single-section expansion

- [x] 1.1 Coordinate single-section expansion across MapContentPanel and its children: initially expand Map styles and allow all sections to collapse; verify switching among four sections, no simultaneous expansion and internal scrolling of active content.
- [x] 1.2 Reduce headers to 40 px, add leading Material Symbols icons and combine icon/text/chevron in an accessible trigger; verify dimensions, counts, keyboard expansion, visible focus and reachability of all headers.
- [x] 1.3 Replace Add style with an icon-only ghost button and the category split dropdown with plus/folder-plus ghost buttons with accessible names and Tooltip; verify independence from expansion, retained row menus, pending guards and correct Add category guidance without groups.
- [x] 1.4 Update category, header and expansion guidance in docs/frontend/ui.md and the feature-editor README; verify consistency with behavior and removal of previous multiple/all-open/split-dropdown instructions.

## 2. Settings & Tools and renaming

- [x] 2.1 Add the fourth section with a local Signal Forms Map name/Save form using existing MapsQueryOptions.update without importing feature-list; verify 1..200 trimmed characters, one name-only PATCH, no empty PATCH and no duplicate submission.
- [x] 2.2 Preserve drafts during collapse, hiding and intermediate map cache updates, show local errors and reset state when changing maps; manually verify failed saves, reopening, persisted names in breadcrumbs/list and after reload without changing camera/style/markers.
- [x] 2.3 Document the form and its boundaries in the feature-editor README and docs/frontend/ui.md; verify that import/other tools are absent and modal renaming in the map list remains available.

## 3. Panel visibility and the map

- [x] 3.1 Add a neutral projection slot before the style selector in maps/ui and a panel button in FeatureEditor, outside the selector rendering condition; verify position and availability with zero, one and multiple styles, preservation of the right toolbar and no admin/API dependencies in maps/ui.
- [x] 3.2 Make desktop visibility controlled, initially open on desktop and closed on mobile, retain an accessible mobile close action and current expansion while hidden; verify reopening, breakpoints, focus, absence of hidden focusable controls and draft preservation.
- [x] 3.3 Use existing viewport resizing without remounting the engine or fitting the map; manually verify center/resolution/style before and after hiding, fullscreen and marker operation without duplicate events.
- [x] 3.4 Update maps/ui and feature-editor READMEs and, if necessary, state ownership in docs/frontend/architecture.md; verify projection, visibility and camera-preservation documentation.

## 4. Integration verification

- [x] 4.1 Format changed handwritten files, run affected Nx lint targets and the admin production build after checking resolved configuration; record actual results without adding or running frontend unit/E2E suites.
- [x] 4.2 Check the panel in desktop/mobile browsers: all four sections, creation, hiding/restoration, draft preservation, rename failure/success, maps without selectors, keyboard operation, themes and reduced motion; record results and limitations.
- [x] 4.3 Run node scripts/check-docs.mjs from the repository root, git diff --check and OpenSpec validation; verify only expected changes, no new dependencies and no server-contract changes.

## Verification notes

- Browser checks through Chrome DevTools CLI on desktop 1400x1000 and mobile/touch 390x844 covered four 40px headers, single expansion and collapse-all, independent Add style/category/group dialogs with cancellation, retained active section and drafts while hidden, accessible mobile closing and focus restoration.
- Renaming was verified with a real PATCH to a temporary name followed by restoration of the original, a subsequent GET, breadcrumb updates and a double Save click. An intercepted network failure retains input; blank, 201-character and unchanged trimmed values send no PATCH.
- Draft loss during optimistic rollback was discovered and fixed; repeated failure/retry/success checks passed. Zero/one-style cases used temporary GET fixtures in a separate browser page; server styles were unchanged.
- Canvas geometry before/after hiding retained a 511x511 image, shifted horizontally by half the viewport-width change and returned to identical image coordinates. Fullscreen resized the canvas without recreating it and preserved the selected style.
- Real ArrowDown/Enter keys changed focus and expansion; light/dark themes and reduced motion were checked (transition-property: none). Creation buttons have 32px hit areas and accessible names.
- Prettier, Nx lint for maps-feature-editor/maps-ui, the admin production build, strict OpenSpec change validation, node scripts/check-docs.mjs and git diff --check passed. No new dependencies, server changes or client generation were needed.
- Unit/E2E suites were neither added nor run, following project policy. Browser checks are manual scenario checks; every possible remote concurrent-change case was not separately exercised.

## 5. Panel refinement

- [x] 5.1 Align chevrons at the right edge, combine category/group actions in Button Group and replace the toggle icon; verify placement, independent button operation and accessible names.
- [x] 5.2 Animate the panel with inert/aria-hidden and reduced-motion support; verify desktop/mobile, focus restoration, draft and camera preservation, then lint/build/documentation/OpenSpec.

Refinement verified: all chevron right edges align beyond the action buttons; category/group use Spartan Button Group and open their respective dialogs. Frame-by-frame checks show intermediate margins down to -320px on desktop and -390px on mobile, followed by visibility:hidden/inert; reduced motion produces transition:none. Camera and style are preserved; maps-feature-editor lint and the admin production build passed, and documentation and strict OpenSpec validation are valid.

## 6. Panel and form simplification

- [x] 6.1 Move animation to Tailwind and remove CSS; verify responsive margins, transitions, inert and reduced motion.
- [x] 6.2 Simplify the name model following the game editor pattern; verify validation, unchanged PATCH avoidance, failure/retry and retained input.
- [x] 6.3 Remove the external mobile header, add close inside the panel and a diskette Save action in Input Group; verify placement, focus and draft preservation, lint/build and documentation.

Simplification verified: the separate panel CSS was removed; Tailwind supplies 200ms margin/opacity/visibility transitions and disables them for reduced motion. Center, resolution, canvas and selected style are preserved. At mobile 390x844 the external header is absent, internal Close restores focus to the map, drafts survive and Save sits at the right of Input Group. The simplified model passed blank/201-character/unchanged, network failure, repeated Save and successful PATCH checks with restoration of the original name. Lint, admin production build, Prettier, documentation checker, strict OpenSpec validation and diff check passed; unit/E2E suites were not run.

## 7. Compact map toolbar

- [x] 7.1 Remove search/filter and unused icons; verify absent buttons and update documentation and the delta.
- [x] 7.2 Make the selector flexible on narrow displays; verify a single row within the viewport at 320px and 390px, preserving button sizes and style selection.
- [x] 7.3 Run affected lint, admin production build, Prettier, documentation checker, strict OpenSpec validation and diff check without unit/E2E suites.

Toolbar verification: the selector is 108px wide at 320px and 178px at 390px. All six controls share one row within the viewport; button sizes are preserved and search/filter are absent. maps-feature-editor/maps-ui lint, admin production build, Prettier, documentation checker, strict OpenSpec validation and diff check passed. Unit/E2E suites were not run.

## 8. Editor fullscreen

- [x] 8.1 Add optional fullscreenTarget and expand the whole editor workspace; verify both panels, fullscreen state, entry/exit and form preservation.
- [x] 8.2 Verify dropdowns and mobile panel/drawer within fullscreen, run maps-ui/maps-feature-editor lint, admin production build and documentation/OpenSpec checks.

Fullscreen verified at 1440x1000: the target is the workspace DIV, both panels are inside it and available, the form survives entry/exit, and eight style options appear within fullscreen. On mobile/touch 390x844 the left panel opens without leaving fullscreen; the marker Drawer is inside fullscreen and visible (about 506px tall), and closes through normal Cancel. maps-ui/maps-feature-editor lint, admin production build, Prettier, documentation checker, strict OpenSpec validation and diff check passed. Existing FullscreenOverlayContainer was unchanged; unit/E2E suites were not run.
