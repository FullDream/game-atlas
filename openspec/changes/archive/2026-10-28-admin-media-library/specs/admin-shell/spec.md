# Spec Delta

## ADDED Requirements

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

## MODIFIED Requirements

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

### Requirement: Keep the initial shell limited to available capabilities

The shell SHALL provide shared styling and navigational structure without introducing authentication, authorization, user accounts or role management. Resource mutations SHALL be exposed only through implemented features such as the Media manager. It SHALL NOT display invented user identity, business statistics or functional links to absent features.

#### Scenario: Inspect initial navigation

- **WHEN** an operator opens administration
- **THEN** Games and Media destinations are present, with functional media management
- **AND** no login flow, fake profile, dashboard statistics or nonfunctional user-management link is presented
