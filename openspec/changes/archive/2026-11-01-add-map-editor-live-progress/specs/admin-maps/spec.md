# Spec Delta

## MODIFIED Requirements

### Requirement: Keep processing status as a snapshot

The standalone map list SHALL show style status from its loaded HTTP snapshot without opening sockets, polling, status-triggered requests or automatic focus/reconnect refreshes. Known mutation results and updates already received by an active editor SHALL reconcile existing cached map data without starting a list subscription.

#### Scenario: Processing continues elsewhere

- **WHEN** a listed style is queued or processing and no editor subscription is active
- **THEN** elapsed time, window focus and reconnect do not trigger requests to discover progress
- **AND** the list does not imply live progress or completion

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

## ADDED Requirements

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
