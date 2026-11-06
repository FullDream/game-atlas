# Spec Delta

## MODIFIED Requirements

### Requirement: Control the viewport

The viewer SHALL support pan, zoom in/out, fit-to-map and fullscreen controls with accessible names and keyboard operation. Initial load and explicit fit SHALL show the complete map. Resizing SHALL update the rendered viewport. Fullscreen SHALL expand a consumer-supplied containing region when provided, otherwise the viewer itself; its active state SHALL follow that region. Unsupported fullscreen SHALL be unavailable.

#### Scenario: Resize and fit

- **WHEN** the viewport changes size or enters fullscreen
- **THEN** the map fills the available region without a stale canvas size
- **WHEN** the user invokes fit
- **THEN** the complete extent is visible

#### Scenario: Expand a consumer workspace

- **WHEN** the consumer supplies a containing region and the user enters fullscreen
- **THEN** that region and its panels appear together while the viewport resizes without resetting selection or camera
- **AND** exit through the control or browser restores the previous workspace with synchronized fullscreen state
