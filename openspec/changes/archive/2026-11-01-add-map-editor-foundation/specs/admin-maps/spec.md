## MODIFIED Requirements

### Requirement: Accessible responsive map management

Map management SHALL follow the existing administration appearance, work on narrow screens and support keyboard use, labelled controls, dialog focus management, visible validation errors and pending submission states. The map list SHALL offer functional editor navigation.

#### Scenario: Use a modal with a keyboard

- **WHEN** an operator opens and closes a map modal using the keyboard
- **THEN** focus is contained while open and returns to its trigger after closure
- **AND** all inputs and actions remain reachable on narrow screens

#### Scenario: Open a map editor

- **WHEN** an operator selects Open editor for a map
- **THEN** its dedicated editor opens within the administration shell
- **AND** direct navigation, refresh and browser history identify the same game and map

## ADDED Requirements

### Requirement: Present the map editing workspace

The editor SHALL show the routed map with a left styles panel and central interactive viewer, retaining shell navigation and contextual breadcrumbs. Loading, missing map, request failure, empty styles and no-ready-style states SHALL be distinct. Categories, markers and the right details editor SHALL remain outside this stage.

#### Scenario: Open and resize the workspace

- **WHEN** the operator opens a map on desktop
- **THEN** styles and the map are visible side by side
- **WHEN** the viewport becomes narrow
- **THEN** the styles panel remains accessible without making map controls or dialogs unreachable

#### Scenario: Recover from a request failure

- **WHEN** the map request fails
- **THEN** an appropriate error or missing-resource state appears
- **AND** a recoverable request failure offers an explicit retry

### Requirement: Browse and select map styles

The styles panel SHALL show styles in server order with source preview, name, dimensions, snapshot status and the server-derived default indicator. Percentages and processing progress bars SHALL NOT appear. Ready-style selection SHALL stay synchronized between the panel and viewer without changing the server default.

#### Scenario: Select a ready style

- **WHEN** the operator selects a ready style from the panel or viewer
- **THEN** both controls identify that style and the viewer displays its tiles
- **AND** no mutation or camera reset occurs

#### Scenario: Inspect unfinished styles

- **WHEN** a style is queued, processing or failed
- **THEN** it remains listed with its snapshot status but cannot become the displayed tile layer
- **AND** elapsed time, focus and reconnect do not request updated processing state

### Requirement: Create styles from library images

Add style SHALL collect a name, background color and one committed library image and submit the existing style creation contract. Names SHALL be trimmed to 1..200 characters; colors SHALL accept #RGB or #RRGGBB. Sources SHALL be supported raster images matching map dimensions. The server SHALL remain authoritative for compatibility and conflicts.

#### Scenario: Attach and await processing

- **WHEN** a valid style is created
- **THEN** the returned map updates the list and the creation dialog closes
- **AND** the new style shows its returned status without fabricated progress or automatic completion discovery
- **AND** the existing displayed style remains selected while it is available

#### Scenario: Reject or cancel creation

- **WHEN** creation is cancelled
- **THEN** no style mutation is sent
- **WHEN** the source is incompatible or the server rejects the request
- **THEN** input is retained and the error is explained
- **AND** pending submission prevents duplicate requests

### Requirement: Edit style metadata

Each style SHALL allow editing its name and background color through the existing PATCH endpoint, regardless of processing status. Only changed mutable fields SHALL be submitted; unchanged submission SHALL send no PATCH. Source replacement, editable order and editable default SHALL NOT be offered.

#### Scenario: Save metadata

- **WHEN** valid changed metadata is saved
- **THEN** the authoritative returned style updates the panel and any active viewer background
- **AND** source, tile imagery, order and viewport remain unchanged

#### Scenario: Reject metadata

- **WHEN** an edit is rejected for a duplicate name, validation or transport failure
- **THEN** the form preserves input and presents the error
- **AND** optimistic changes are rolled back without overwriting another successful operation

### Requirement: Confirm style deletion

Style deletion SHALL require confirmation naming the style and explaining that library media and map content remain. The style SHALL remain visible until server success. Success SHALL reconcile map caches and default selection; failure SHALL preserve the style and report an error.

#### Scenario: Delete the active or final style

- **WHEN** confirmed deletion of the active style succeeds
- **THEN** the viewer selects the surviving default without moving its camera
- **WHEN** no ready styles remain
- **THEN** the no-ready-style or empty-style state is shown
- **AND** the coordinate space and map content remain intact

#### Scenario: Cancel or fail deletion

- **WHEN** confirmation is cancelled
- **THEN** no DELETE is sent
- **WHEN** deletion fails
- **THEN** the style remains available and an error is shown

### Requirement: Keep search and filter as placeholders

The map toolbar SHALL include named search and filter controls visibly marked unavailable. Activating them SHALL NOT initiate requests, alter visible map content or suggest that search or filtering is implemented.

#### Scenario: Inspect the toolbar

- **WHEN** an operator reaches the search or filter control
- **THEN** its unavailable state is conveyed visually and to assistive technology
