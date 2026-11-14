# Spec Delta

## MODIFIED Requirements

### Requirement: Render category-styled markers

The viewer SHALL accept transport-independent marker presentation data with stable identity, name, X/Y position, category icon/color, and optional draft indicator metadata (`draftStates?: readonly ('marker' | 'category' | 'group')[]`). It SHALL render distinguishable selected and pending markers and use a colored pin fallback for missing icons. When `draftStates` is non-empty on a marker, the viewer SHALL draw a colored indicator dot with a contrasting outline in the top-right corner of the marker icon using the most specific draft level (`'marker'` `#F59E0B` > `'category'` `#0EA5E9` > `'group'` `#8B5CF6`), and the marker hover tooltip SHALL display a second line below the marker name listing every draft level (`Draft: marker`, `Draft: marker, category, group`, etc.). Marker updates and style switches SHALL preserve camera position and resolution.

#### Scenario: Render and update markers

- **WHEN** markers or category appearance change on an initialized map
- **THEN** marker presentation updates without resetting the camera or tile selection
- **AND** missing images show a usable fallback

#### Scenario: Render draft indicator dot and multiline tooltip

- **WHEN** a marker has `draftStates` containing one or more of `'marker'`, `'category'`, `'group'`
- **THEN** the rendered marker icon includes the colored top-right corner dot corresponding to the first (most specific) level (`#F59E0B`, `#0EA5E9`, or `#8B5CF6`)
- **AND** hovering the marker displays its name on the first line and the active draft levels (`Draft: <levels>`) on the second line of the tooltip
- **AND** a marker without `draftStates` renders no corner dot and shows only its single-line name tooltip

#### Scenario: Retain markers without ready tiles

- **WHEN** map geometry exists but no ready style exists
- **THEN** marker rendering and interactions remain available in logical coordinate space
- **AND** before geometry exists placement is unavailable without fabricated coordinates
