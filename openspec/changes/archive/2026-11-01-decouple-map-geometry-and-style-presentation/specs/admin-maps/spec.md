# Spec Delta

## MODIFIED Requirements

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

### Requirement: Create styles from library images

Add style SHALL collect a name, background color and one committed library image and submit the existing style creation contract. Names SHALL be trimmed to 1..200 characters; colors SHALL accept #RGB or #RRGGBB. Sources SHALL be supported raster images of any dimensions admitted by the library. Aspect ratio differences SHALL produce a non-blocking warning before submission, explaining that the image will stretch to fill the logical extent. The server SHALL remain authoritative for compatibility and conflicts.

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

Each style SHALL allow editing its name and background color through the existing PATCH endpoint, regardless of processing status. Only changed mutable fields SHALL be submitted; unchanged submission SHALL send no PATCH. Source replacement SHALL NOT be offered. Default and ordering actions SHALL be separate from the metadata form.

#### Scenario: Save metadata

- **WHEN** valid changed metadata is saved
- **THEN** the authoritative returned style updates the panel and any active viewer background
- **AND** source, tile imagery, order and viewport remain unchanged

#### Scenario: Reject metadata

- **WHEN** an edit is rejected for a duplicate name, validation or transport failure
- **THEN** the form preserves input and presents the error
- **AND** optimistic changes are rolled back without overwriting another successful operation

## ADDED Requirements

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
