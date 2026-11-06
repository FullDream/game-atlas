# Design

## Context

Motivation and scope are described in [proposal.md](proposal.md); the behavior contract is in the [admin-maps delta](specs/admin-maps/spec.md).

MapContentPanel already owns category/marker queries, style selection and mobile visibility. Its three child sections use h-14 headers and independent sectionOpen state; the parent accordion uses multiple mode. Desktop md:flex forces the panel to remain visible. MapViewport renders its selector only when several styles exist; the existing mapToolbar slot is projected on the right.

Renaming is available in MapEditorDialog in feature-list. MapsQueryOptions.update already performs PATCH with detail/list cache reconciliation and rollback on error. Importing feature-list from feature-editor would violate dependency directions.

## Goals / Non-Goals

**Goals:** one expansion state, panel visibility independent of data, and preservation of local drafts while the panel is hidden.

**Non-Goals:** new libraries, APIs or a separate state store; changing the map connection lifecycle; persistence across reloads. The map-list modal form remains in place.

## Decisions

### 1. Coordinate sections in MapContentPanel

Use the installed Spartan accordion in single mode with one consistent selected-section state: styles, categories, markers, settings or no selection. Initially select styles. Bind child sections to this state instead of three independent initial true values. A collapsed section occupies only its header; the expanded section receives the remaining height and scrolls internally. Do not unmount forms merely because their sections collapse.

Keeping multiple mode and closing siblings through unrelated handlers would create competing state sources and complicate restoration after hiding.

### 2. Compact headers and independent actions

Use 40 px headers with leading Material Symbols Rounded icons for layers, folders, markers and settings. Icons are 18–20 px; icon-only ghost buttons are about 32 px. Preserve counts and existing request guards. The expansion trigger combines icon, text and chevron; actions are adjacent siblings rather than nested buttons. Use existing ui-button and Spartan Tooltip components.

Categories combines two buttons in a Spartan Button Group: plus creates a category, and folder-plus creates a group. Remove the creation dropdown while retaining tree-row menus. Without groups, Add category retains its meaning and shows the existing group prerequisite guidance.

Outline buttons would leave unnecessary borders in every row; ghost provides a quieter appearance while preserving hover and focus feedback.

### 3. Visibility and projection before the selector

Add a neutral slot in MapViewport before the selector, outside its rendering condition. FeatureEditor projects its panel button there; maps/ui acquires no administration dependencies. Retain the existing right-side mapToolbar slot.

Replace forced desktop visibility with controlled state: visible by default on desktop and hidden on mobile. On mobile, preserve switching between the map and panel and an accessible close action because the map button may be hidden with the viewport. Do not create a second independent visibility flag; use existing breakpoint detection. Showing and hiding do not unmount the panel, change its selected section or recreate the map engine. Existing viewport resizing handles size changes without fit-to-map.

State lasts for the mounted editor; localStorage is unnecessary. Hiding restores focus to an available control.

### 4. Local Settings & Tools form

Add a child section in feature-editor using Signal Forms for Map name, Save and local errors. Call MapsQueryOptions.update for the current gameId/mapId, submitting only the changed trimmed name. Match the existing form rules: 1..200 characters, no empty PATCH and no duplicate save. Use existing cache reconciliation for breadcrumbs and the map list.

Do not import MapEditorDialog from feature-list or extract a shared library for one field. Accept authoritative names without overwriting dirty input during intermediate cache updates. Collapsing the section or panel preserves its draft; changing maps does not transfer the draft to another map. Do not add import controls, tool placeholders or new dialogs.

### 5. Align documentation and specifications

The existing UI guidance requires a split dropdown and initially expanded sections. Replace those provisions and the README description of height sharing during implementation. The delta updates Compose category creation and colored tree controls and Browse markers in a separate left accordion while retaining their scenarios. Added requirements cover compactness, single-section expansion, hiding and inline renaming. The requirement for modal renaming in the list remains unchanged.

## Risks / Trade-offs

- Direct isOpened bindings can conflict with single accordion mode → use one expansion source and verify keyboard switching among all four sections.
- Mobile close can become unavailable when the viewport is hidden → retain accessible controls in the visible panel/mobile row.
- Collapsing can lose a draft → keep the form owner mounted and verify hiding, section changes and breakpoint changes.
- Width changes can reset the camera or duplicate handlers → use existing resizing and verify center, resolution and style preservation.
- Compactness reduces hit areas → retain separately reachable buttons with visible focus, accessible names and tooltips.

## Migration Plan

Frontend only; no data migrations or client generation. Ship with updated documentation. Roll back by reverting frontend changes and documentation; the server contract is unchanged.

## Refinement after visual review

Align section chevrons at the far right; creation actions precede them as separate sibling elements without nested buttons. The panel toggle uses left/right chevrons. Animate margin-inline-start and opacity for 200ms: content retains its width while viewport resizing occurs without fitting the map. The closed panel immediately becomes inert and aria-hidden, with visibility hidden after the transition; reduced motion disables transitions.

## Simplification after verification

Move the animation entirely into Tailwind host classes: transition properties, duration/easing, responsive negative inline margin, opacity, visibility and reduced motion. Remove the separate CSS file. Use a short linkedSignal that preserves the form value for the same map ID, following the game editor pattern; remove branches based on the previous name and mutation state. After Save, accept the response and reset dirty state. Remove the mobile row above the map and place close inside the open panel. Put the diskette Save action in a Spartan Input Group inline-end addon.

## Compact map toolbar

Remove inactive search/filter controls and their icon registrations from feature-editor. Prevent wrapping of the viewport's top row: the leading group and selector use min-w-0 and flexible width, the selected name truncates, and working controls remain shrink-0. On phones, reduce only gaps and outer spacing. Verification at 320px gives the selector 108px while preserving button sizes; desktop retains its 14rem selector limit.

## Fullscreen workspace

The panels disappeared because requestFullscreen targeted only maps-viewport while the panels were its siblings. MapViewport accepts an optional fullscreenTarget and defaults to its host. FeatureEditor passes the workspace div containing the viewport and both panels; fullscreenchange compares the active element with that target. Elements remain mounted, and the existing ResizeObserver updates the map. The configured FullscreenOverlayContainer keeps dropdowns, dialogs and drawers available; global configuration is unchanged.
