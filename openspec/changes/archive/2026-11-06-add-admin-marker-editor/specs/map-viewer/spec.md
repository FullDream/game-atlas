# Spec Delta

## ADDED Requirements

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
