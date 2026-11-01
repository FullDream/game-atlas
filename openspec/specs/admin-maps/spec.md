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
