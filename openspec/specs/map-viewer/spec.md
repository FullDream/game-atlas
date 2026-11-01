# map-viewer Specification

## Purpose

Provide a reusable game-map viewport that renders aligned imagery in the map's original coordinate space for administration and public consumers.

## Requirements

### Requirement: Render authoritative pixel geometry

The viewer SHALL use the initialized logical extent for its camera and content coordinates and each ready style's own tile geometry for imagery. The entire source extent SHALL map onto the entire logical extent with independent X/Y scaling, without cropping, letterboxing, geographic conversion, wrapping or geometry rounding. Background SHALL show through transparency.

#### Scenario: Render a rectangular map with odd dimensions

- **WHEN** logical or source dimensions are odd with transparent edge tiles
- **THEN** fractional boundaries remain aligned and padding does not enlarge or shift the image footprint

#### Scenario: Stretch both ways

- **WHEN** 8000x8000 and 1000x1000 styles share a map initialized from either one
- **THEN** both fill the same logical extent and a fixed logical point remains fixed

#### Scenario: Stretch unequal aspect ratios

- **WHEN** a 1000x1000 style is selected on an 8000x4000 logical map
- **THEN** the full square image stretches to the full rectangle with center and all four corners aligned

### Requirement: Select aligned styles without moving the viewport

The viewer SHALL initially select the supplied displayable default style. Only ready styles with a tile URL SHALL be selectable. Switching styles SHALL retain the viewport center and resolution. Changing the server default or display order SHALL NOT replace a surviving local selection. Source resolution differences SHALL NOT reset or clamp the camera on selection. If the selected style disappears, selection SHALL fall back to the current displayable default or no style.

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

Before logical geometry exists, the viewer SHALL show an awaiting-first-style state without creating fabricated geometry or requesting tiles. After initialization, absent or non-ready styles SHALL retain the usable logical coordinate space with a no-ready-style state. Source previews SHALL NOT substitute for completed tiles.

#### Scenario: Open a processing-only map

- **WHEN** geometry exists but every style is queued, processing or failed
- **THEN** the viewer retains logical geometry and requests no unavailable tiles

#### Scenario: Initialize an empty map

- **WHEN** a newly created map receives its first style attachment
- **THEN** the viewer can initialize from returned geometry while showing no-ready-style until a ready snapshot is loaded

#### Scenario: Remove every style

- **WHEN** an initialized map loses its final style
- **THEN** its coordinate space persists and the camera does not reset

### Requirement: Support independent browser consumers

The viewer SHALL accept map geometry and style presentation data independently of administration routes and API clients. It SHALL initialize browser resources only in the browser and release them on removal so navigation and repeated mounting do not retain obsolete maps.

#### Scenario: Mount after server rendering

- **WHEN** a consumer renders the viewer before browser initialization
- **THEN** rendering does not access unavailable browser APIs
- **AND** the interactive viewport initializes in the browser without requiring administration code
