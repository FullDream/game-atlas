# Spec Delta

## MODIFIED Requirements

### Requirement: Compose category creation and colored tree controls

The Categories header SHALL offer separate icon-only ghost buttons: plus for Add category and folder-plus for Add group, without a creation dropdown. Group and category action menus SHALL include decorative Material Symbols icons. The editor SHALL expose group color using the same hex rules as category color. Categories without library images SHALL display a map-pin icon in the category color. Groups without library images SHALL render no icon. Optional library images SHALL remain supported.

#### Scenario: Create and color a group

- **WHEN** the operator opens Add group from its dedicated header button and saves a valid color
- **THEN** the group persists that color and no default icon is added
- **AND** menu icons accompany accessible text labels

### Requirement: Browse markers in a separate left accordion

The left panel SHALL include a separate Markers accordion with an independently scrolling list using the available content height while it is the sole expanded section. Rows SHALL display the category icon or colored fallback, marker name and category name and offer keyboard-accessible editing. Loading, failure/retry and empty states SHALL remain local to this section.

#### Scenario: Select a marker from the list

- **WHEN** an operator activates a marker row with pointer or keyboard
- **THEN** the existing marker editor opens for that point
- **AND** on narrow screens the content panel closes so the map remains visible above the Drawer

#### Scenario: Collapse and recover the list

- **WHEN** the Markers section is collapsed or expanded
- **THEN** at most one section is open and its content uses the available height and scrolls independently
- **AND** loading, empty and failed requests have distinct local presentations with retry on failure

## ADDED Requirements

### Requirement: Present compact content section headers

The left panel SHALL show Map styles, Categories, Markers and Settings & Tools in that order with 40 px headers and a decorative icon before each label. Style and marker counts SHALL remain visible. Header creation actions SHALL be icon-only ghost buttons with accessible names and tooltips, separate from the section trigger and before the far-right chevron. Category creation buttons SHALL form one labelled button group.

#### Scenario: Operate compact headers

- **WHEN** the operator clicks a section icon, label or chevron, or activates the trigger with the keyboard
- **THEN** the section toggles with visible focus and conveyed expanded state
- **AND** header creation actions open their existing forms without toggling the section or interacting with the map
- **AND** Map styles offers Add style while Categories offers distinct Add category and Add group buttons

#### Scenario: Create a category without a group

- **WHEN** the operator activates the plus button on a map without groups
- **THEN** the existing prerequisite guidance offers group creation
- **AND** the plus button never silently changes meaning to Add group

### Requirement: Expand one content section at a time

The panel SHALL initially expand Map styles only. Opening another section SHALL collapse the previous one; closing the current section SHALL allow all sections to be collapsed. The expanded content SHALL use the remaining panel height with internal scrolling and all four headers reachable.

#### Scenario: Switch and collapse sections

- **WHEN** the operator opens Categories after entering the editor
- **THEN** Map styles closes and only Categories content is shown
- **WHEN** the operator closes Categories
- **THEN** all four compact headers remain and no section content is shown

### Requirement: Toggle the editor content panel from the map

A named panel toggle SHALL appear immediately before the map style selector on desktop and mobile, remaining available when the selector is absent. Hiding and showing the panel SHALL preserve the current section, style selection, marker draft and camera center/resolution. The desktop panel SHALL start visible; the mobile panel SHALL start hidden.

#### Scenario: Hide and restore the panel

- **WHEN** the operator hides the panel while Categories is expanded and then shows it
- **THEN** Categories remains the sole expanded section and the viewport adapts without a camera reset
- **AND** hiding the panel does not leave focus inside hidden content

#### Scenario: Operate without a style selector

- **WHEN** a map has zero or one style and no style selector is rendered
- **THEN** the panel toggle remains available
- **AND** on mobile an accessible close action inside the content panel remains reachable while it is shown, without a separate header above the map

### Requirement: Rename the map from Settings and Tools

Settings & Tools SHALL offer only a labelled Map name field and an icon-only Save action inside the trailing input addon, with an accessible name and tooltip. Names SHALL contain 1..200 trimmed characters. Only a changed valid name SHALL be saved; unchanged submission SHALL send no PATCH. Failures SHALL preserve input and explain the error; pending saves SHALL prevent duplicate submission.

#### Scenario: Save a map name in the editor

- **WHEN** the operator saves a valid changed name
- **THEN** the returned name appears in the field, breadcrumbs and affected map views and survives reload
- **AND** style selection, map geometry and marker editing remain intact

#### Scenario: Retain a name draft

- **WHEN** the operator edits a name and switches sections or hides and restores the panel
- **THEN** the unsaved name remains available without a write
- **AND** import controls and other future tools are absent

#### Scenario: Reject or skip a save

- **WHEN** the name is blank or exceeds the limit, a save fails, or the trimmed name is unchanged
- **THEN** invalid input blocks submission, failure retains input with an accessible error, and unchanged input sends no PATCH

### Requirement: Animate content panel visibility

Panel visibility changes SHALL use a brief slide and fade, disabled when reduced motion is requested. Hidden controls SHALL immediately become unavailable to focus and assistive technology without losing drafts. The panel toggle SHALL use directional left/right chevrons distinct from the application sidebar control.

#### Scenario: Toggle panel with or without motion

- **WHEN** the operator hides or shows the content panel
- **THEN** the panel slides and fades over 200ms while preserving section and draft state
- **AND** reduced-motion preference makes the transition immediate
- **AND** the toggle direction reflects whether it will show or hide the panel

### Requirement: Keep map controls on one narrow-screen row

The map toolbar SHALL keep the panel toggle, style selector, zoom, fit and fullscreen controls on one row at viewport widths of 320px and above. The selector SHALL shrink as space decreases and truncate long selected names while retaining its accessible label and usable options. Map control buttons SHALL retain their existing sizes.

#### Scenario: Use the toolbar on a narrow phone

- **WHEN** the editor map is displayed at 320px or 390px width
- **THEN** all toolbar controls remain within the viewport on the same row
- **AND** the style selector takes the remaining width without pushing controls below it

### Requirement: Keep editor panels available in fullscreen

Editor fullscreen SHALL include the map workspace, left content panel and marker editor. Existing panel visibility, selected section, style selection and drafts SHALL survive entry and exit. Dialogs, selects and mobile marker drawers SHALL remain usable in fullscreen, with normal responsive panel behavior.

#### Scenario: Enter and leave fullscreen with an editor open

- **WHEN** an operator enters fullscreen with the content panel and marker editor open
- **THEN** both panels remain available alongside the map and overlays appear within fullscreen
- **AND** leaving fullscreen preserves their state without resetting the camera or draft

#### Scenario: Use mobile panels in fullscreen

- **WHEN** an operator opens map content or selects a marker in mobile fullscreen
- **THEN** the left panel or marker drawer remains usable without leaving fullscreen
- **AND** the existing close controls and draft protection remain available

## REMOVED Requirements

### Requirement: Keep search and filter as placeholders

**Reason**: The editor no longer displays inactive search and filter controls.
**Migration**: Remove both placeholder buttons and their tooltips; retain the existing working map controls.
