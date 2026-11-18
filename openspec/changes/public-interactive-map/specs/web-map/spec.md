# Spec Delta

## Purpose

Provide a public interactive game map page with full-screen map navigation, offline-first client-side category visibility filtering, and marker completion tracking backed by TanStack DB and IndexedDB.

## ADDED Requirements

### Requirement: Resolve and render public map page

The web application SHALL route `/games/:slug/maps/:mapSlug` to a dedicated public map component. It SHALL query published map details via `PublicMapsQueryOptions` (generated from the `Public Maps` OpenAPI tag) and display the map using `MapViewport` in a full-viewport container.

#### Scenario: Successfully loading a published map

- **WHEN** a visitor navigates to `/games/:slug/maps/:mapSlug` for an existing published game and map
- **THEN** the application SHALL fetch map details, initialize the map viewport with logical geometry and available styles, and render published markers.

#### Scenario: Map or game not found

- **WHEN** a visitor navigates to an invalid slug or a draft/unpublished map
- **THEN** the application SHALL display a friendly not-found or error state with an action to return to the game page.

### Requirement: Filter category and group visibility

The map page SHALL provide a category filter panel displaying category groups and their child categories with icons, names, and marker counts. The user SHALL be able to toggle the visibility of individual categories and entire groups.

#### Scenario: Toggling group visibility

- **WHEN** a user unchecks a category group
- **THEN** all markers belonging to all categories within that group SHALL be hidden from the map viewport.

#### Scenario: Bulk visibility actions

- **WHEN** a user clicks "Hide all" or "Show all" in the filter panel
- **THEN** all categories and groups SHALL be updated simultaneously to hidden or visible respectively.

### Requirement: Persist visibility preferences in IndexedDB

The application SHALL persist category and group hidden states in an IndexedDB collection managed by `@tanstack/db` under the current `mapId`.

#### Scenario: Restoring visibility preferences across reloads

- **WHEN** a user returns to a map where specific categories or groups were previously hidden
- **THEN** the application SHALL load the preferences from IndexedDB without server requests and initialize the map with those categories and groups hidden.

### Requirement: Track marker completion

The user SHALL be able to toggle any marker as completed (found/done). Completed markers SHALL be persisted in an IndexedDB collection managed by `@tanstack/db` containing `id` (markerId), `categoryId`, `mapId`, `gameId`, and `completedAt`.

#### Scenario: Marking a marker as completed

- **WHEN** a user clicks the completion toggle on an uncompleted marker
- **THEN** the marker ID with its metadata SHALL be saved to the `completedMarkers` collection, and the marker on the map SHALL visually dim to `opacity: 0.35`.

#### Scenario: Unmarking a completed marker

- **WHEN** a user unchecks the completion toggle on a completed marker
- **THEN** the record SHALL be removed from the `completedMarkers` collection, and the marker on the map SHALL return to full opacity.

### Requirement: Toggle hiding completed markers

The filter panel footer SHALL provide a switch to hide completed markers. When enabled, completed markers SHALL be completely excluded from the map rendering.

#### Scenario: Enabling hide completed switch

- **WHEN** the user enables the "Hide completed" switch
- **THEN** all markers present in the `completedMarkers` collection for this map SHALL disappear from the map viewport.

#### Scenario: Disabling hide completed switch

- **WHEN** the user disables the "Hide completed" switch
- **THEN** all completed markers (not otherwise hidden by category filters) SHALL reappear on the map dimmed at `opacity: 0.35`.

### Requirement: Desktop hybrid marker inspection

On desktop screens, clicking a marker SHALL open an OpenLayers anchored popup positioned directly over the marker pin. The popup SHALL feature a quick completion checkbox at its bottom edge immediately adjacent to the pin, and an expand button (`⤢`) in its header to open a full-height right-side details panel.

#### Scenario: Quick completion from popup

- **WHEN** a user clicks a marker and clicks the checkbox at the bottom of the popup
- **THEN** the completion state SHALL toggle immediately without requiring cursor movement to a remote sidebar.

#### Scenario: Expanding to side panel

- **WHEN** a user clicks the expand button in the popup header
- **THEN** the right drawer/sidebar SHALL open with full marker descriptions and media assets, closing the anchored popup.

### Requirement: Mobile non-modal interactive bottom drawer

On mobile devices, selecting a marker SHALL open a bottom sheet in a non-modal peek state (~120px) containing the marker name, category, and a completion switch on the right side of the header. The map above the peek sheet SHALL remain interactive for panning, zooming, and selecting other markers.

#### Scenario: Interacting with map while drawer is in peek state

- **WHEN** the bottom sheet is in peek state
- **THEN** the user SHALL be able to pan the map and tap another marker, which updates the sheet content without closing it.

#### Scenario: Expanding and dismissing mobile sheet

- **WHEN** the user drags the peek sheet upwards
- **THEN** it SHALL expand to ~85vh revealing the full description and media gallery; dragging down from expanded returns to peek, and dragging down from peek dismisses the sheet.

### Requirement: Collapsible floating filter panel with identity header

The filter panel SHALL feature a sticky header displaying the game icon, game title, and map title. On desktop, the panel SHALL float over the map and be collapsible into a floating menu button (`[ ☰ ]`). On mobile, the filter panel SHALL open as a bottom drawer from a floating filter action.

#### Scenario: Collapsing filter panel on desktop

- **WHEN** the user clicks the collapse button on the floating left panel
- **THEN** the panel SHALL slide off-screen to the left, revealing 100% of the map viewport, leaving a floating button to restore it.

### Requirement: Refined map navigation and style initialization

The public map page SHALL occupy the dynamic viewport without the global header. The filter panel SHALL provide home/game navigation and theme selection below its identity header. Its toggle SHALL appear immediately before style selection in the map toolbar. Style thumbnails SHALL use resolved image URLs.

#### Scenario: First map load

- **WHEN** ready map styles arrive without a user selection
- **THEN** the default ready style SHALL be displayed and selected in the toolbar
- **AND** the first ready style SHALL be used if the default is unavailable.

### Requirement: Refined inspection and mobile filters

Desktop inspection SHALL use Spartan Popover with a bounded scrollable full description and manual media carousel, while retaining quick completion and expansion. Mobile category filters SHALL use Spartan Drawer. These refinements supersede the earlier brief-preview-only popup and custom filter overlay implementation choices.

#### Scenario: Inspecting a marker with multiple media assets

- **WHEN** a visitor opens a desktop marker popup
- **THEN** all media SHALL be reachable through carousel controls and the full description SHALL scroll within the bounded popup.

#### Scenario: Dismissing mobile filters

- **WHEN** a visitor dismisses the filter drawer with its close control, Escape, backdrop, or native swipe
- **THEN** the map SHALL remain visible and the filter trigger SHALL remain available.

### Requirement: Complete mobile marker inspection with snap heights

The mobile marker inspector SHALL use Spartan Drawer with the complete marker content available in its independently scrollable body at initial height of approximately one third of the viewport. It SHALL have full-height expansion, remain open during map interaction and marker switching, and expose completion controls. This supersedes the earlier 120px peek and 85vh expanded sizes.

#### Scenario: Expanding and collapsing mobile inspection

- **WHEN** a visitor drags the Drawer resize handle upward
- **THEN** it SHALL expand to full viewport height
- **AND** dragging downward SHALL restore the previous one-third height before another downward drag dismisses it
- **AND** keyboard or click activation SHALL toggle between these sizes.

#### Scenario: Revealing a newly selected marker

- **WHEN** a visitor selects another marker with the Drawer open
- **THEN** its full content SHALL replace the previous marker content without dismissing the Drawer or resetting its height
- **AND** the viewport SHALL minimally pan the marker into the area above the Drawer without changing zoom.

### Requirement: Public map system controls and English copy

The viewer SHALL compose installed shared UI and Helm controls for actions, category accordion, search, breadcrumbs, switches and resource feedback. All interface text SHALL be English; game and map content SHALL retain its original language.

#### Scenario: Opening category filters on mobile

- **WHEN** mobile category filters open
- **THEN** the desktop collapse arrow SHALL be omitted and native Drawer dismissal SHALL remain available.

### Requirement: Persistent public layout header

The global layout header SHALL remain visible on the map route, superseding the earlier requirement to suppress it. The map SHALL fill the remaining viewport height below the header.

#### Scenario: Opening the public map

- **WHEN** a visitor opens a public map
- **THEN** the normal layout header SHALL be visible
- **AND** the map SHALL fit below it without adding vertical page scrolling.

### Requirement: Map panel navigation without duplicate theme controls

The category panel SHALL omit theme selection and SHALL use Spartan Breadcrumb for home icon, linked game name and current map name. Theme selection SHALL remain in the layout header. This supersedes earlier panel theme-selection requirements.

#### Scenario: Navigating from the category panel

- **WHEN** a visitor opens the category panel
- **THEN** the breadcrumbs SHALL show a home icon linking to the catalog, the game linking to its details page and the map name as the current page
- **AND** no theme switcher SHALL be displayed in this panel.
