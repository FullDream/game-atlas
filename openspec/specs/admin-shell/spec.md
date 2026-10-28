# admin-shell Specification

## Purpose

Provide an independently runnable administration workspace whose navigation and layout can host future GameAtlas content-management features.

## Requirements

### Requirement: Run administration independently

The frontend SHALL provide a separately runnable and buildable `admin` application. It SHALL run as a browser SPA without a production rendering server. Starting or building administration SHALL NOT require building or serving the public website or starting the backend merely to render its shell.

#### Scenario: Open administration without the backend

- **WHEN** an operator starts administration while the backend is unavailable
- **THEN** the application shell and foundation destinations render without fabricated API data or a blank screen
- **AND** the public website is not a runtime dependency

#### Scenario: Serve a built application

- **WHEN** the production output is served with the documented SPA fallback
- **THEN** direct navigation and refresh on supported administration routes render the application
- **AND** no production Node.js rendering process is required

### Requirement: Navigate foundation destinations

Administration SHALL expose Games and Media destinations within one persistent shell. The root route SHALL lead to Games. Navigation SHALL indicate the active destination, update the page heading and breadcrumbs, and support browser back/forward. Games SHALL remain a foundation page identifying its purpose; Media SHALL provide the implemented media manager.

#### Scenario: Navigate between resource areas

- **WHEN** an operator selects Media from Games and then uses browser Back
- **THEN** the matching destination, heading and active navigation item change together
- **AND** the shell remains available throughout navigation, subject to confirmed unsaved-change protection

#### Scenario: Reach a foundation page directly

- **WHEN** an operator opens `/games` or `/media` directly
- **THEN** the Games foundation page or Media manager renders inside the same shell
- **AND** a visit to `/` resolves to Games

### Requirement: Adapt navigation to the viewport

The administration shell SHALL have a left sidebar and compact header. Desktop navigation SHALL support expanded and icon-only collapsed states. On narrow screens navigation SHALL open as a dismissible overlay without permanently reducing the content area. Choosing a destination on a narrow screen SHALL close the overlay.

#### Scenario: Collapse desktop navigation

- **WHEN** an operator toggles the sidebar on a desktop viewport
- **THEN** the content area remains usable and the current destination stays identifiable
- **AND** collapsed navigation controls retain accessible names

#### Scenario: Navigate on a narrow screen

- **WHEN** an operator opens navigation, selects Media, or dismisses the overlay
- **THEN** the overlay behaves correctly for that action without trapping the page in an inaccessible state
- **AND** closing it restores access to the main content

### Requirement: Provide accessible shell controls

The shell and its owned Helm components SHALL use Material Symbols Rounded SVG icons consistently. Navigation and shell actions SHALL use semantic landmarks, accessible names, visible keyboard focus and a logical tab order. Overlay navigation SHALL manage focus and support keyboard dismissal. The current page SHALL have a clear primary heading, and decorative icons SHALL NOT replace control names.

#### Scenario: Use navigation with a keyboard

- **WHEN** an operator navigates the shell without a pointing device
- **THEN** the sidebar toggle and destinations are reachable and operable
- **AND** the active destination is conveyed beyond color alone

#### Scenario: Dismiss an overlay

- **WHEN** focus is inside narrow-screen navigation and the operator presses Escape
- **THEN** the overlay closes and focus returns to the opening control

### Requirement: Keep the initial shell limited to available capabilities

The shell SHALL provide shared styling and navigational structure without introducing authentication, authorization, user accounts or role management. Resource mutations SHALL be exposed only through implemented features such as the Media manager. It SHALL NOT display invented user identity, business statistics or functional links to absent features.

#### Scenario: Inspect initial navigation

- **WHEN** an operator opens administration
- **THEN** Games and Media destinations are present, with functional media management
- **AND** no login flow, fake profile, dashboard statistics or nonfunctional user-management link is presented

### Requirement: Choose and remember a color theme

Administration SHALL offer Light, Default (system), and Dark in an accessible native radio group. Default SHALL follow live system preferences; explicit modes SHALL restrict the document to that scheme. Apply selection through the head color-scheme meta tag and semantic CSS tokens, without a dark-mode class.

#### Scenario: Choose and restore a theme

- **WHEN** an operator selects Light or Dark and later reloads administration
- **THEN** the selection is persisted using ngxtension `injectLocalStorage` and restored before Angular bootstraps
- **AND** the document meta advertises only the selected scheme

#### Scenario: Follow the system theme

- **WHEN** Default is selected, no saved preference exists, or a saved value is invalid
- **THEN** the document supports `light dark` and the browser chooses the palette from its current system preference
- **AND** later system preference changes are reflected without requiring another selection

#### Scenario: Synchronize another tab

- **WHEN** the saved theme changes in another tab on the same origin
- **THEN** the theme control and document color scheme update consistently

#### Scenario: Select a theme using the keyboard

- **WHEN** an operator focuses the native theme radio group
- **THEN** arrow keys change the selected option using native browser behavior
- **AND** Tab enters or leaves the group

#### Scenario: Suggest a matching browser chrome color

- **WHEN** administration restores or changes its theme preference
- **THEN** its theme-color meta hints enable white for Light, dark zinc for Dark, or matching system media queries for Default
- **AND** the startup script applies saved hints before Angular bootstraps; browser/platform support determines whether surrounding browser UI is colored

### Requirement: Keep the workspace header visible

The application header SHALL remain visible while workspace content scrolls. Resource features SHALL be able to supply their breadcrumb path without coupling the shell to their API models. Breadcrumbs SHALL use available header width according to their rendered labels before collapsing long paths through an accessible overflow menu.

#### Scenario: Browse a media folder

- **WHEN** the operator scrolls through media cards in a nested folder
- **THEN** the header shows the current folder path and remains available alongside page actions and navigation controls

#### Scenario: Keep short mobile paths visible

- **WHEN** the breadcrumb labels fit the available space beside mobile navigation and page actions
- **THEN** all fitting path items remain visible instead of collapsing according to a fixed width per label
- **AND** the current location remains identifiable while ancestors that do not fit remain accessible through the overflow menu

#### Scenario: Adapt breadcrumb overflow to resizing

- **WHEN** the available header width or breadcrumb labels change
- **THEN** the visible path is recalculated to use the available space without overflowing adjacent controls

### Requirement: Keep theme controls in the sidebar footer

The sidebar footer SHALL center the three theme choices when expanded and align its top border with the media pagination footer. Collapsed desktop navigation SHALL show only the active preference icon as a named button cycling light, system and dark. Mobile navigation SHALL retain all three choices.

#### Scenario: Switch theme with collapsed navigation

- **WHEN** the operator activates the theme button in the collapsed sidebar
- **THEN** the next preference becomes active and its icon and accessible name update
- **AND** the existing local-storage persistence and head color-scheme behavior apply

### Requirement: Show workspace loading without header layout shifts

Active workspace navigation or page-data requests SHALL display a thin indeterminate progress line at the bottom edge of the header. Showing and hiding it SHALL preserve the header's height, breadcrumb width and action positions and SHALL NOT block navigation controls.

#### Scenario: Fetch page data in the background

- **WHEN** a page starts fetching or refreshing its data
- **THEN** a 2px indeterminate progress line appears at the header's bottom edge without an invented percentage
- **AND** existing content and navigation remain usable

#### Scenario: Settle a workspace request

- **WHEN** all active navigation and page-data loading tracked by the header settle
- **THEN** the progress line disappears without moving the header, breadcrumbs, actions or content below it

#### Scenario: Announce indeterminate loading

- **WHEN** the progress line is visible
- **THEN** assistive technology can identify its loading purpose without a misleading numeric progress value
