## Purpose

Provide a reusable game-map viewport that renders aligned imagery in the map's original coordinate space for administration and public consumers.

## ADDED Requirements

### Requirement: Render authoritative pixel geometry

The viewer SHALL render ready style tiles using the supplied extent, origin, tile size, resolutions and zoom bounds in centered pixel coordinates, without geographic conversion, wrapping or geometry rounding. The selected style background SHALL show through transparent tiles.

#### Scenario: Render a rectangular map with odd dimensions

- **WHEN** a map has fractional extent boundaries and transparent edge tiles
- **THEN** imagery retains its correct alignment and orientation without repeated worlds or stretched edges
- **AND** the style background fills transparent and uncovered areas

### Requirement: Select aligned styles without moving the viewport

The viewer SHALL initially select the supplied displayable default style. Only ready styles with a tile URL SHALL be selectable. Switching styles SHALL retain the viewport center and resolution. If the selected style disappears, selection SHALL fall back to the current displayable default or no style.

#### Scenario: Switch and remove a selected style

- **WHEN** a user switches between ready styles after panning and zooming
- **THEN** the same location remains visible at the same scale
- **WHEN** the selected style is removed
- **THEN** the next available default is shown without resetting the camera

### Requirement: Control the viewport

The viewer SHALL support pan, zoom in/out, fit-to-map and fullscreen controls with accessible names and keyboard operation. Initial load and explicit fit SHALL show the complete map. Resizing SHALL update the rendered viewport. Unsupported fullscreen SHALL be unavailable.

#### Scenario: Resize and fit

- **WHEN** the viewport changes size or enters fullscreen
- **THEN** the map fills the available region without a stale canvas size
- **WHEN** the user invokes fit
- **THEN** the complete extent is visible

### Requirement: Render without a displayable style

The viewer SHALL retain a usable map coordinate space when styles are absent or none are ready. It SHALL communicate the absence of displayable imagery without requesting unavailable tiles or presenting source previews as completed tiles.

#### Scenario: Open a processing-only map

- **WHEN** every style is queued, processing or failed
- **THEN** the viewer presents a neutral no-ready-style state without tile requests for those styles

### Requirement: Support independent browser consumers

The viewer SHALL accept map geometry and style presentation data independently of administration routes and API clients. It SHALL initialize browser resources only in the browser and release them on removal so navigation and repeated mounting do not retain obsolete maps.

#### Scenario: Mount after server rendering

- **WHEN** a consumer renders the viewer before browser initialization
- **THEN** rendering does not access unavailable browser APIs
- **AND** the interactive viewport initializes in the browser without requiring administration code
