# Spec Delta

## MODIFIED Requirements

### Requirement: Present the map editing workspace

The editor SHALL show the routed map with styles and a category tree in the existing left panel and a central interactive viewer. It SHALL retain shell navigation and breadcrumbs, distinguish map loading/missing/failure and style empty/not-ready states, and keep category and marker loading/errors local. Marker editing SHALL use a bottom drawer on narrow screens and a right-side sheet on wide screens.

#### Scenario: Open and resize the workspace

- **WHEN** the operator opens a map on desktop
- **THEN** styles and categories appear in the same left panel beside the map
- **WHEN** the viewport becomes narrow
- **THEN** a named panel toggle exposes both sections and all controls and dialogs remain reachable

#### Scenario: Recover from a request failure

- **WHEN** the map request fails
- **THEN** the appropriate error or missing-resource state appears with explicit retry when recoverable
- **WHEN** only category or marker retrieval fails
- **THEN** its local error offers retry while loaded styles and the viewer remain usable

#### Scenario: Resize an open marker editor

- **WHEN** the screen crosses the editor breakpoint with unsaved marker values
- **THEN** the appropriate drawer or sheet presents the same draft without data loss or duplicate submission

## ADDED Requirements

### Requirement: Select and edit a marker

Selecting a marker in Pan mode SHALL open its form with name, plain-text description, category, ordered media and X/Y coordinates. Name SHALL require 1..200 trimmed characters, description at most 5000 trimmed characters, and coordinates finite numbers. Category choices SHALL be grouped and display category icons in options and the selected value, with a colored pin fallback.

#### Scenario: Open and validate

- **WHEN** an operator selects an existing marker
- **THEN** its persisted values appear, its category is identifiable by name and icon, and the marker is highlighted
- **AND** invalid fields prevent submission with accessible field errors
- **AND** no slug, notes or formatting toolbar is shown

### Requirement: Save or discard marker field edits

Field edits SHALL remain a draft until Save. Save SHALL send only changed mutable values and accept authoritative returned data; unchanged forms SHALL send no PATCH. Failures SHALL retain input. Closing, switching markers/tools or navigating away with unsaved changes SHALL require an explicit decision to keep editing or discard. Pending submissions SHALL prevent duplicate writes.

#### Scenario: Save and fail

- **WHEN** a changed form is submitted
- **THEN** one update is sent and success refreshes the marker presentation and affected counts
- **AND** rejection retains values and explains the failure

#### Scenario: Discard protection

- **WHEN** a dirty editor is dismissed by close, backdrop, Escape, swipe, selection or navigation
- **THEN** changes are retained unless the operator explicitly discards them

### Requirement: Manage marker photographs

The editor SHALL show photographs in an accessible manually controlled carousel, select images from the existing library, and allow detachment and reordering. Detachment SHALL NOT delete library assets. Existing video attachments SHALL remain represented and preserved when saving unrelated fields or photographs; adding videos is outside this stage.

#### Scenario: Edit an ordered gallery

- **WHEN** an operator adds, reorders or removes photographs and saves
- **THEN** the ordered attachment IDs are persisted without duplicates, library deletion or silent loss of existing videos
- **AND** empty and single-image states remain usable without meaningless navigation

### Requirement: Choose a map editing tool

A toolbar SHALL remain bottom-centered within the map with Pan, Move marker, Add marker and Add several markers in that order. Single and repeated creation SHALL use separate buttons. The toolbar SHALL NOT contain marker-selection or placement-mode dropdowns. Exactly one mode SHALL be active, defaulting to Pan. Controls SHALL have accessible names and visible selection. Toolbar actions SHALL NOT place markers or pan the map accidentally.

#### Scenario: Change tools

- **WHEN** an operator switches tools
- **THEN** only that tool responds to subsequent map interactions
- **AND** the toolbar respects narrow screens and safe areas without covering editor actions

### Requirement: Create a single marker

Single placement SHALL use a map click to set X/Y and open the form. Create SHALL persist one valid marker; success SHALL select the persisted marker and restore Pan. Cancellation SHALL create nothing. Placement SHALL require an available coordinate space and at least one category, with explanatory guidance when unavailable.

#### Scenario: Place and create

- **WHEN** the operator clicks the map in single-add mode, fills required fields and submits
- **THEN** exactly one marker is created at the selected coordinates and appears after reload
- **AND** failure retains the draft for correction

### Requirement: Repeatedly place a marker template

Repeated placement SHALL validate a template once before placement begins. Each subsequent intentional map click SHALL immediately create a separate marker using that template and clicked coordinates. The mode SHALL persist until Finish, Escape or another tool is selected. A visible status SHALL identify the template and successful creation count. Finishing SHALL retain already persisted markers.

#### Scenario: Place several markers

- **WHEN** a valid template starts placement and three map positions are clicked
- **THEN** three separate creates are submitted and each successful marker is counted once
- **AND** map drags and toolbar clicks create no marker
- **AND** stopping the mode cancels future placement but preserves in-flight outcomes

#### Scenario: Failed placement

- **WHEN** a placement fails or its response is lost
- **THEN** its position is distinguishable from successful persisted markers and the failure is explained
- **AND** the create is not automatically retried
- **AND** a definitive rejection offers explicit retry, while an uncertain outcome requires reconciliation before any duplicate-risk retry

### Requirement: Persist marker movement on release

Move mode SHALL drag markers and immediately save changed X/Y on release, without a Save action. The same marker SHALL not move again while its update is pending. A failed update SHALL restore its prior position and show an error. Empty-space dragging SHALL continue panning. Coordinate fields SHALL provide a keyboard-accessible alternative.

#### Scenario: Drag and persist

- **WHEN** a marker is released at a changed position
- **THEN** one coordinate update is sent immediately and success adopts server coordinates
- **AND** no-op or cancelled dragging sends no update

#### Scenario: Reject a movement

- **WHEN** the coordinate update fails
- **THEN** the marker returns to its previous position and can be moved again after the error

### Requirement: Delete a marker and reconcile ownership

Deletion SHALL require confirmation naming the marker and retaining shared media. The marker SHALL remain until success; failure SHALL preserve it. Successful creates, transfers and deletions SHALL reconcile the map marker list and affected category/group counts. Cascading category/group removal SHALL remove affected markers and close obsolete selections without losing unrelated drafts.

#### Scenario: Confirm deletion

- **WHEN** marker deletion succeeds
- **THEN** the marker disappears, its editor closes and affected counts refresh
- **AND** cancellation or failure leaves the marker and its attachments intact

### Requirement: Keep the selected marker visible above the mobile drawer

Opening the mobile marker drawer SHALL keep the selected marker fully visible in the uncovered map area with a visual margin. If necessary, the camera SHALL pan minimally without changing zoom or marker coordinates. A marker already visible with that margin SHALL NOT cause a camera shift. The drawer SHALL leave enough map area to show the marker.

#### Scenario: Open a marker near the bottom

- **WHEN** a selected marker would be covered by the opening drawer
- **THEN** the camera shifts enough to show the complete marker above it with a margin
- **AND** zoom and persisted marker position remain unchanged and no marker update is sent

#### Scenario: Select a marker already visible

- **WHEN** the selected marker fits in the uncovered map area with the margin
- **THEN** opening the drawer does not move the camera

#### Scenario: Change the visible map area

- **WHEN** drawer height or screen orientation changes while a marker is selected
- **THEN** visibility is recalculated against the actual uncovered map area without repeated unnecessary movement

### Requirement: Browse markers in a separate left accordion

The left panel SHALL include a separate Markers accordion with an independently scrolling list, sharing height with styles and categories. Rows SHALL display the category icon or colored fallback, marker name and category name and offer keyboard-accessible editing. Loading, failure/retry and empty states SHALL remain local to this section.

#### Scenario: Select a marker from the list

- **WHEN** an operator activates a marker row with pointer or keyboard
- **THEN** the existing marker editor opens for that point
- **AND** on narrow screens the content panel closes so the map remains visible above the Drawer

#### Scenario: Collapse and recover the list

- **WHEN** the Markers section is collapsed or expanded
- **THEN** remaining open sections share the available height and list content scrolls independently
- **AND** loading, empty and failed requests have distinct local presentations with retry on failure
