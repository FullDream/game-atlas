## Purpose

Let administrators manage game-owned maps through an independent feature embedded in the game editor, with modal actions and clear snapshot state.

## ADDED Requirements

### Requirement: Browse game maps

The Maps page SHALL display every map of the routed game in server order, showing name, dimensions, preview, style count and previews, createdAt and updatedAt. Missing previews SHALL have neutral fallbacks. No Main badge or fabricated category/marker counts SHALL appear.

#### Scenario: Open and reload the list

- **WHEN** an operator opens or reloads /games/{gameId}/maps
- **THEN** that game's full list loads with dates and available style previews
- **AND** empty, loading and request-failure states are distinct, with creation and error retry actions as applicable

### Requirement: Create maps in a modal

The page SHALL offer Add map in the workspace header. Its modal SHALL require a trimmed name of 1..200 characters and integer width and height in 1..16384, matching backend validation. Successful creation SHALL close the modal and display the persisted blank map.

#### Scenario: Failed or cancelled creation

- **WHEN** creation fails validation, conflicts with an existing name, or encounters a transport error
- **THEN** the modal preserves input and explains the failure
- **AND** cancellation sends no mutation and duplicate submission is prevented while pending

### Requirement: Rename maps in a modal

Each map SHALL offer editing in a modal with an editable name and read-only dimensions. Saving SHALL send only changed mutable values and display authoritative returned data. An unchanged form SHALL NOT send an empty PATCH.

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

The page SHALL show style status from its loaded HTTP snapshot without sockets, polling, status-triggered requests or automatic focus/reconnect refreshes. Known mutation results SHALL update displayed data without an extra status read.

#### Scenario: Processing continues elsewhere

- **WHEN** a loaded style is queued or processing
- **THEN** elapsed time, window focus and reconnect do not trigger requests to discover progress
- **AND** the UI does not imply live progress or completion

### Requirement: Accessible responsive map management

Map management SHALL follow the existing administration appearance, work on narrow screens and support keyboard use, labelled controls, dialog focus management, visible validation errors and pending submission states. Unimplemented editor navigation SHALL remain disabled.

#### Scenario: Use a modal with a keyboard

- **WHEN** an operator opens and closes a map modal using the keyboard
- **THEN** focus is contained while open and returns to its trigger after closure
- **AND** all inputs and actions remain reachable on narrow screens
