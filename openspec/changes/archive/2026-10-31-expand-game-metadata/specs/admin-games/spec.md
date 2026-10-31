# Spec Delta

## MODIFIED Requirements

### Requirement: Create and edit game metadata

Editors SHALL validate required name, slug and library cover selection according to the API contract and support optional release date, developer, plain-text description and banner. Creation SHALL produce a draft and navigate to its persisted page. Editing SHALL send only changed mutable fields and preserve unsaved input on failure.

#### Scenario: Invalid or conflicting slug

- **WHEN** a slug is invalid or the server reports a slug conflict
- **THEN** the editor explains the failure and preserves entered values for correction

#### Scenario: Save metadata

- **WHEN** valid changed metadata is saved
- **THEN** authoritative values are displayed and the form becomes pristine
- **AND** unchanged editors do not send empty patches

#### Scenario: Edit description without formatting

- **WHEN** the operator edits a multiline description
- **THEN** plain text can be entered and saved, preserving line breaks
- **AND** every formatting toolbar button remains disabled and stored text is not interpreted as HTML

#### Scenario: Clear optional values

- **WHEN** the operator clears release date, developer, description or banner and saves
- **THEN** the corresponding changed fields are sent as null and remain cleared after reload
- **AND** omitted fields are preserved and a required cover cannot be cleared

### Requirement: Choose a library image

Game editors SHALL offer folder-aware selection of an existing library image with a preview for cover and banner. Videos and folders SHALL NOT be selectable. Banner SHALL be optional and removable; cover SHALL remain required. Inputs SHALL use coverId and bannerId.

#### Scenario: Cancel selection

- **WHEN** the operator cancels the image picker
- **THEN** the current editor selection remains unchanged

#### Scenario: Choose a banner

- **WHEN** the operator chooses a library image as banner
- **THEN** its preview appears and it is saved independently of the cover
- **AND** removing the banner does not delete its library asset

## ADDED Requirements

### Requirement: Display actual game content in cards

Game cards SHALL display API cover, nullable banner and description, marker/category counts, and map summaries. Map counts SHALL derive from maps.length. Missing optional images SHALL have neutral fallbacks; missing description SHALL not show fabricated game text. Empty map collections SHALL show an empty state. Loading skeletons SHALL remain distinct from loaded zero counts and null previews.

#### Scenario: Show persisted content

- **WHEN** a game is loaded in grid or list layout
- **THEN** its banner, cover, description, numeric counts and owned map names and previews replace demonstration placeholders
- **AND** null images and empty maps render without broken image elements

### Requirement: Display game maps from summaries

The game editor Maps tab SHALL show the number of returned maps and list their names and nullable previews from game data. Empty games SHALL show a clear empty state. Map creation, editing, deletion and viewer implementation SHALL remain outside this change; controls for unimplemented actions SHALL remain disabled.

#### Scenario: Open a game maps tab

- **WHEN** the operator opens or reloads an existing game's Maps tab
- **THEN** the current summaries and accurate map count appear, including maps without previews
- **AND** a game with no maps shows zero and an empty state
