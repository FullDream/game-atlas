# Spec Delta

## MODIFIED Requirements

### Requirement: Create maps in a modal

The page SHALL offer Add map in the workspace header. Its modal SHALL require a trimmed name and client-supplied slug of 1..200 characters each, matching backend validation, without width or height inputs. Successful creation SHALL close the modal and display the persisted blank map.

#### Scenario: Failed or cancelled creation

- **WHEN** creation fails validation, conflicts with an existing name or game-scoped slug, or encounters a transport error
- **THEN** the modal preserves input and explains the failure
- **AND** cancellation sends no mutation and duplicate submission is prevented while pending

### Requirement: Rename maps in a modal

Each map SHALL offer editing in a modal with editable name and slug fields and read-only logical dimensions or a pending-first-style label. Saving SHALL send only changed mutable values and display authoritative returned data. An unchanged form SHALL NOT send an empty PATCH.

#### Scenario: Save and reject renames

- **WHEN** an operator saves a valid changed name
- **THEN** the modal closes after success and the list shows the returned name and updatedAt
- **AND** a rejected save preserves input and explains the error without losing another successful change

### Requirement: Rename the map from Settings and Tools

Settings & Tools SHALL offer labelled Map name and Map slug fields with icon-only Save actions inside trailing input addons, with an accessible name and tooltip. Names SHALL contain 1..200 trimmed characters. Only changed valid metadata SHALL be saved; unchanged submission SHALL send no PATCH. Failures SHALL preserve input and explain the error; pending saves SHALL prevent duplicate submission.

#### Scenario: Save a map name in the editor

- **WHEN** the operator saves a valid changed name
- **THEN** the returned name appears in the field, breadcrumbs and affected map views and survives reload
- **AND** style selection, map geometry and marker editing remain intact

#### Scenario: Retain a name draft

- **WHEN** the operator edits a name and switches sections or hides and restores the panel
- **THEN** the unsaved name remains available without a write
- **AND** import controls and other future tools are absent

#### Scenario: Reject or skip a save

- **WHEN** the name is blank or exceeds the limit, a save fails, or both trimmed name and slug are unchanged
- **THEN** invalid input blocks submission, failure retains input with an accessible error, and unchanged input sends no PATCH

## ADDED Requirements

### Requirement: Validate and save map slugs in metadata forms

Map creation, the list editing modal and Settings & Tools SHALL expose labelled slug inputs. Slugs SHALL match backend trimming, length and lowercase ASCII single-hyphen syntax. Forms SHALL NOT generate slugs or change them on name edits. Saves SHALL send only changed fields for PATCH and accept authoritative responses. Invalid input SHALL block submission; failed saves SHALL retain drafts and show accessible errors.

#### Scenario: Create a map with an explicit slug

- **WHEN** an operator supplies a valid name and slug in Add map
- **THEN** the create request includes both fields and the returned map appears after success
- **AND** missing or invalid slug blocks submission without deriving a value from name

#### Scenario: Edit a slug without changing identity

- **WHEN** an operator changes only slug in either existing editing surface
- **THEN** the PATCH includes slug without unchanged name or presentation fields and the returned slug survives reload
- **AND** ID-based navigation, query identities, geometry, style selection and marker editing remain intact

#### Scenario: Preserve drafts and unchanged values

- **WHEN** a slug edit is rejected with a conflict, a panel is hidden, or unchanged trimmed values are submitted
- **THEN** errors retain entered values, hiding preserves unsaved drafts, and unchanged submission sends no PATCH
- **AND** pending saves prevent duplicate submissions
