# admin-maps Specification

## Purpose

Let administrators manage game-owned maps through an independent feature embedded in the game editor, with modal actions and clear snapshot state.

## Requirements

### Requirement: Browse game maps

The Maps page SHALL display every map of the routed game in server order, showing name, initialized logical dimensions or a pending-first-style label, preview, style count and previews, createdAt and updatedAt. Missing previews SHALL have neutral fallbacks. No Main badge or fabricated category/marker counts SHALL appear.

#### Scenario: Open and reload the list

- **WHEN** an operator opens or reloads /games/{gameId}/maps
- **THEN** that game's full list loads with dates and available style previews
- **AND** empty, loading and request-failure states are distinct, with creation and error retry actions as applicable

### Requirement: Create maps in a modal

The page SHALL offer Add map in the workspace header. Its modal SHALL require a trimmed name of 1..200 characters, matching backend name validation, without width or height inputs. Successful creation SHALL close the modal and display the persisted blank map.

#### Scenario: Failed or cancelled creation

- **WHEN** creation fails validation, conflicts with an existing name, or encounters a transport error
- **THEN** the modal preserves input and explains the failure
- **AND** cancellation sends no mutation and duplicate submission is prevented while pending

### Requirement: Rename maps in a modal

Each map SHALL offer editing in a modal with an editable name and read-only logical dimensions or a pending-first-style label. Saving SHALL send only changed mutable values and display authoritative returned data. An unchanged form SHALL NOT send an empty PATCH.

#### Scenario: Save and reject renames

- **WHEN** an operator saves a valid changed name
- **THEN** the modal closes after success and the list shows the returned name and updatedAt
- **AND** a rejected save preserves input and explains the error without losing another successful change

### Requirement: Confirm map deletion

Deletion SHALL require a modal confirmation naming the map and describing cascading removal of styles, groups, categories and markers while retaining library media. The map SHALL remain visible until server success and remain available on failure.

#### Scenario: Cancel or confirm deletion

- **WHEN** the operator cancels confirmation
- **THEN** no DELETE is sent
- **WHEN** confirmed deletion succeeds
- **THEN** only the deleted map is removed from the displayed list and caches

### Requirement: Keep processing status as a snapshot

The standalone map list SHALL show style status from its loaded HTTP snapshot without opening sockets, polling, status-triggered requests or automatic focus/reconnect refreshes. Known mutation results and updates already received by an active editor SHALL reconcile existing cached map data without starting a list subscription.

#### Scenario: Processing continues elsewhere

- **WHEN** a listed style is queued or processing and no editor subscription is active
- **THEN** elapsed time, window focus and reconnect do not trigger requests to discover progress
- **AND** the list does not imply live progress or completion

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

### Requirement: Browse and select map styles

The editor styles panel SHALL show server order, source preview, name, dimensions, current processing status and the server-derived default indicator. Processing cards SHALL show an accessible progress bar and server percentage; queued cards SHALL show waiting state, and failed cards SHALL expose safe error information. Ready-style selection SHALL stay synchronized between panel and viewer without changing the server default.

#### Scenario: Select a ready style

- **WHEN** the operator selects a ready style from the panel or viewer
- **THEN** both controls identify that style and the viewer displays its tiles
- **AND** no mutation or camera reset occurs

#### Scenario: Inspect unfinished styles

- **WHEN** a style is queued, processing or failed
- **THEN** it remains listed with its current status but cannot become the displayed tile layer
- **AND** processing percentages come from the server without simulated advancement or polling

#### Scenario: Finish processing

- **WHEN** a style becomes ready while the editor is open
- **THEN** its tiles become selectable and the server default is reconciled
- **AND** an existing valid ready selection and camera are preserved

### Requirement: Create styles from library images

Add style SHALL collect a name, background color and one committed library image and submit the existing style creation contract. Names SHALL be trimmed to 1..200 characters; colors SHALL accept #RGB or #RRGGBB. Sources SHALL be supported raster images of any dimensions admitted by the library. Aspect ratio differences SHALL produce a non-blocking warning before submission, explaining that the image will stretch to fill the logical extent. The server SHALL remain authoritative for compatibility and conflicts.

#### Scenario: Attach and await processing

- **WHEN** a valid style is created
- **THEN** the returned map updates the list and the creation dialog closes
- **AND** the new style follows live server processing updates without fabricated progress
- **AND** the existing displayed style remains selected while it is available

#### Scenario: Reject or cancel creation

- **WHEN** creation is cancelled
- **THEN** no style mutation is sent
- **WHEN** the source is incompatible or the server rejects the request
- **THEN** input is retained and the error is explained
- **AND** pending submission prevents duplicate requests

### Requirement: Edit style metadata

Each style SHALL allow editing its name and background color through the existing PATCH endpoint, regardless of processing status. Only changed mutable fields SHALL be submitted; unchanged submission SHALL send no PATCH. Source replacement SHALL NOT be offered. Default and ordering actions SHALL be separate from the metadata form.

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

### Requirement: Warn about aspect ratio differences without blocking attachment

The style creation dialog SHALL compare source and initialized logical aspect ratios. Different ratios SHALL show an accessible warning explaining full-area stretching and possible distortion while allowing submission. Equal ratios at different resolutions SHALL show no warning. Before first initialization there SHALL be no ratio warning.

#### Scenario: Attach square imagery to a rectangular map

- **WHEN** a 1000x1000 source is chosen for an 8000x4000 map
- **THEN** the warning appears and a valid form remains submittable

#### Scenario: Replace the selected source

- **WHEN** a mismatched source is replaced by one with the map's ratio
- **THEN** the warning disappears; changing dimensions alone at equal ratio causes no warning

### Requirement: Choose a default style explicitly

Ready style cards SHALL offer a labelled Make default action and identify the returned effective default. Non-ready styles SHALL NOT offer an enabled default action. Default changes SHALL retain the current displayed ready style and camera. Pending submission SHALL prevent duplicate/conflicting mutations and failures SHALL preserve state with an error.

#### Scenario: Persist a default choice

- **WHEN** an administrator makes another ready style default
- **THEN** the returned default indicator and affected cached previews update, reload retains it, and current local viewer selection remains unchanged

#### Scenario: Reject a default mutation

- **WHEN** the server rejects or fails the action
- **THEN** the prior default remains indicated and an accessible error is shown

### Requirement: Reorder styles through card handles

The styles panel SHALL allow drag ordering through a labelled drag-handle button on each card. Dragging SHALL start only from the handle, and dropping SHALL persist the full ordered ID list. All processing states SHALL participate. Reordering SHALL preserve default, local selection and camera. An equivalent keyboard operation SHALL be available.

#### Scenario: Drop and reload

- **WHEN** an administrator drags a card handle to another position
- **THEN** the order saves and survives reload without selecting the dragged style or changing default

#### Scenario: Handle failed or unchanged drops

- **WHEN** a save fails or a card is dropped without changing order
- **THEN** failure restores the authoritative order and shows an error; an unchanged drop sends no mutation

#### Scenario: Use keyboard ordering

- **WHEN** an administrator operates the provided keyboard ordering controls
- **THEN** the same persisted order can be produced without pointer dragging

#### Scenario: Preserve card interactions

- **WHEN** an administrator clicks card actions or selects the card away from its handle
- **THEN** the intended action occurs without starting a drag

### Requirement: Subscribe only for active editor maps

An active editor SHALL subscribe to its map notifications and obtain current HTTP state. Reconnect SHALL restore group membership and reconcile current state. Leaving the editor or changing map SHALL release the previous membership and reject its late work. Processing completion SHALL NOT end membership. No connection SHALL remain active without consumers.

#### Scenario: Open and switch maps

- **WHEN** an operator opens an editor and then navigates to another map or leaves
- **THEN** notifications are registered before joining and reading the map
- **AND** the old subscription is released and cannot modify the new editor

#### Scenario: Reconnect or fail connection

- **WHEN** the connection reconnects
- **THEN** the editor rejoins its map and reconciles HTTP state before reporting live synchronization
- **WHEN** initial connection or subsequent recovery fails
- **THEN** HTTP-loaded data remains usable with a visible stale/reconnecting state and a recovery action
- **AND** connection failure does not mark styles failed

### Requirement: Reconcile live map changes safely

Notifications SHALL update existing map detail and list caches without a separate resource store. Percentage ticks SHALL NOT trigger HTTP reads. Initial loading, reconnect, lifecycle completion and ambiguous overlaps SHALL reconcile HTTP state without polling. Stale responses and events SHALL NOT resurrect deleted resources, regress accepted lifecycle state or overwrite newer metadata or successful mutations.

#### Scenario: Receive events during reads and edits

- **WHEN** notifications overlap initial loading, metadata edits or mutation responses
- **THEN** received events are reconciled with the response without losing newer accepted fields
- **AND** ambiguous state triggers a scoped reconciliation rather than guessing or invalidating unrelated domains

#### Scenario: Receive obsolete attempt progress

- **WHEN** a delayed tick belongs to an obsolete processing attempt
- **THEN** it does not override the current attempt
- **AND** uncertainty about attempt identity is resolved through current HTTP state

#### Scenario: Delete a style or map remotely

- **WHEN** a deletion notification arrives during a pending read
- **THEN** the deleted style or map cannot reappear from that read
- **AND** style deletion reconciles default/selection; map deletion releases membership and shows the missing-map state

### Requirement: Retry failed styles from their cards

Failed style cards SHALL offer a labelled retry action through the existing retry operation. Pending retry SHALL prevent duplicate or conflicting submissions. Success SHALL accept the returned reset processing state and follow live updates. Failure SHALL retain the card and expose the error. Non-failed styles SHALL NOT offer an enabled retry action.

#### Scenario: Retry and complete

- **WHEN** an operator retries a failed style
- **THEN** the style and source identities are retained, prior error/progress are reset by the server response, and the card follows the new attempt
- **AND** prior attempt events cannot restore the old failure or progress

#### Scenario: Reject retry

- **WHEN** retry fails validation, conflicts with current server state or encounters a transport failure
- **THEN** the card remains present, pending submission ends and an accessible error is shown
- **AND** a state conflict triggers reconciliation with server state

### Requirement: Browse a two-level category tree

The panel SHALL place Categories below styles and show groups with their category children, optional image icons and persisted marker counts. Group rows SHALL contain drag handle, expansion arrow, optional icon, name, count and actions menu. Child rows SHALL contain an indented drag handle, optional icon, name, count and actions menu. Missing icons SHALL NOT create broken-image indicators.

#### Scenario: Expand groups and inspect counts

- **WHEN** an operator expands or collapses a group
- **THEN** its children show or hide without a mutation or another category request
- **AND** counts remain visible on collapsed groups and names truncate without hiding actions or counts

#### Scenario: Loading empty and error states

- **WHEN** categories initially load, the map has no groups, or a request fails
- **THEN** the section respectively displays matching row skeletons, an actionable empty state, or an error with retry
- **AND** an empty existing group remains a valid creation and drop destination

### Requirement: Manage category and group metadata

The Categories header SHALL offer creation of a group or category. Group menus SHALL offer add category, edit and delete; category menus SHALL offer edit and delete. Forms SHALL support names and optional library-image icons, with color for both kinds and category destination group. Save SHALL submit changed fields only; unchanged editing SHALL send no PATCH.

#### Scenario: Create a category without a group

- **WHEN** a map has no groups and the operator chooses Add category
- **THEN** the interface explains that a group is required and provides a group-creation action
- **AND** it does not invent or silently create a default group

#### Scenario: Create edit and clear an icon

- **WHEN** valid creation or editing is saved
- **THEN** the server result updates the tree and the dialog closes after success
- **AND** icons can be omitted on creation or explicitly cleared during editing

#### Scenario: Reject or cancel a form

- **WHEN** submission fails validation, uniqueness checks or transport
- **THEN** entered values remain available and the failure is explained
- **WHEN** the operator cancels
- **THEN** no mutation is sent
- **AND** pending submission prevents duplicate requests

### Requirement: Reorder groups and move categories optimistically

Groups SHALL reorder among map groups; categories SHALL reorder within a group or move to another group of the same map. Dragging SHALL start only from labelled handles. A changed drop SHALL update visible order and group counts immediately, then persist relative placement. Unchanged drops SHALL send no mutation. Failure SHALL reconcile affected data without losing another successful change and display an error.

#### Scenario: Move between groups

- **WHEN** a populated category is dropped at a destination position, including an empty group
- **THEN** it immediately appears there exactly once and both group counts adjust
- **AND** successful persistence survives reload without changing marker identity or ownership

#### Scenario: Reject a conflicting move

- **WHEN** the destination has a conflicting category name or the request fails
- **THEN** membership, position and counts reconcile safely and the operator sees an error

#### Scenario: Preserve other interactions

- **WHEN** an operator activates expansion, a row menu or a dialog
- **THEN** no drag starts and no map style selection or camera change occurs
- **AND** a category cannot be dropped as a root node or nested under another category

### Requirement: Operate category management accessibly

The tree SHALL support named rows, visible focus, arrow-key navigation and group expansion. All CRUD and placement actions SHALL be available without pointer dragging, including moves to another group. Dialogs SHALL manage and restore focus. Controls SHALL remain usable on narrow screens and preserve the project's themes and reduced-motion behavior.

#### Scenario: Use keyboard placement

- **WHEN** an operator uses menu actions to move up, move down or choose another group
- **THEN** the same server placement operation and optimistic reconciliation are used
- **AND** focus remains on the moved item or a sensible surviving control

#### Scenario: Use row actions and collapsed destinations

- **WHEN** the operator reaches a row's menu with the keyboard
- **THEN** it can be opened without selecting, expanding or dragging the row
- **AND** a collapsed group can receive a category through an explicit keyboard move action

### Requirement: Confirm cascading category deletion

Deleting a group or category SHALL require confirmation naming it and explaining the removal of its current categories or markers while retaining library media. Items SHALL remain visible until server success. Success SHALL remove only affected tree data and reconcile counts; failure SHALL retain items and explain the error.

#### Scenario: Cancel fail or complete deletion

- **WHEN** confirmation is cancelled
- **THEN** no DELETE is sent
- **WHEN** deletion fails
- **THEN** the item remains available
- **WHEN** deletion succeeds
- **THEN** the tree and counts update and focus moves to a surviving row or creation control

### Requirement: Compose category creation and colored tree controls

The Categories header SHALL use Spartan Button Group with a primary creation action and a separate dropdown trigger. Group and category action menus SHALL include decorative Material Symbols icons. The editor SHALL expose group color using the same hex rules as category color. Categories without library images SHALL display a map-pin icon in the category color. Groups without library images SHALL render no icon. Optional library images SHALL remain supported.

#### Scenario: Create and color a group

- **WHEN** the operator opens Add group from the split control and saves a valid color
- **THEN** the group persists that color and no default icon is added
- **AND** menu icons accompany accessible text labels

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
