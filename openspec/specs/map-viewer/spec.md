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

### Requirement: Render category-styled markers

The viewer SHALL accept transport-independent marker presentation data with stable identity, name, X/Y position and category icon/color. It SHALL render distinguishable selected and pending markers and use a colored pin fallback for missing icons. Marker updates and style switches SHALL preserve camera position and resolution.

#### Scenario: Render and update markers

- **WHEN** markers or category appearance change on an initialized map
- **THEN** marker presentation updates without resetting the camera or tile selection
- **AND** missing images show a usable fallback

#### Scenario: Retain markers without ready tiles

- **WHEN** map geometry exists but no ready style exists
- **THEN** marker rendering and interactions remain available in logical coordinate space
- **AND** before geometry exists placement is unavailable without fabricated coordinates

### Requirement: Expose mode-specific marker interactions

The viewer SHALL expose selection, placement and completed movement with marker identity or logical X/Y coordinates, without issuing persistence requests. Pan SHALL select markers and pan the map; Add SHALL place only on intentional map clicks; Move SHALL drag markers while empty-space dragging pans. Controls and cancelled gestures SHALL NOT emit placement or completed movement.

#### Scenario: Separate interactions

- **WHEN** an operator clicks, pans, drags a marker or uses an overlaid control
- **THEN** only the interaction enabled by the active mode is emitted
- **AND** a movement emits once on release, with no update for unchanged or cancelled gestures

### Requirement: Release marker browser resources

Marker layers, gesture listeners and image resources SHALL follow the viewer browser lifecycle. Changing maps or removing the viewer SHALL release old resources and prevent late interactions from affecting another map. Marker presentation SHALL remain independent of administration routes and API clients.

#### Scenario: Remount and switch maps

- **WHEN** a viewer is removed or its map changes during a gesture
- **THEN** the obsolete gesture cannot emit a completed operation into the new map
- **AND** later mounting creates only the current map's marker interactions

### Requirement: Reveal a marker within an unobscured viewport region

The viewer SHALL support revealing a selected marker within a supplied unobscured viewport region with padding. It SHALL pan only as much as necessary to fit the marker, preserving resolution and marker coordinates. This explicit reveal SHALL be distinct from ordinary marker-data updates, which preserve the camera.

#### Scenario: Reveal around an overlay

- **WHEN** the consumer requests reveal with the visible map region remaining above an overlay
- **THEN** an obscured marker is brought fully into that region with padding at the existing resolution
- **AND** a marker already fitting that region causes no camera movement or marker-movement event

### Requirement: Interact without an activation click

Pointer panning and wheel zoom SHALL work as soon as the initialized viewer receives those gestures, without requiring a preliminary click or browser focus. The viewer SHALL NOT steal focus on initialization or hover. Keyboard controls SHALL remain scoped to normal map focus.

#### Scenario: Zoom and pan before activating the map

- **WHEN** an operator moves the pointer over a newly initialized map and uses the wheel or begins dragging
- **THEN** the first gesture zooms or pans the map without an activation click
- **AND** wheel interaction does not move focus away from another control
