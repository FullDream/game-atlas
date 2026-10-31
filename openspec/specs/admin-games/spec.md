# admin-games Specification

## Purpose

Let administrators browse and manage game metadata through directly addressable creation and editing pages within the existing administration shell.

## Requirements

### Requirement: Browse games

The Games page SHALL show game covers, names, slugs and statuses with server-side name search, sorting by name/creation/update time, and pagination. Search, sort and page state SHALL be reflected in the URL and restored by browser navigation.

#### Scenario: Search and return to a list

- **WHEN** an operator filters and pages the list, opens a game and returns
- **THEN** the list restores its URL state and displays matching results

#### Scenario: Empty and failed requests

- **WHEN** the catalog is empty, search has no results, or loading fails
- **THEN** the page distinguishes these states, offers creation or clearing search as applicable, and offers retry for failures

### Requirement: Dedicated game editors

Administration SHALL expose /games/new and /games/{id} as dedicated pages. Editing SHALL work on direct navigation and reload. Missing games SHALL show a not-found state; request failures SHALL offer retry without presenting a blank editor.

#### Scenario: Reload a game

- **WHEN** an operator opens or reloads a game URL without a cached list
- **THEN** its current metadata is loaded by ID and can be edited

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

### Requirement: Publish and unpublish games

An existing game's editor SHALL allow changing status between draft and published through game updates and clearly display the resulting status.

#### Scenario: Publication fails

- **WHEN** a status update fails
- **THEN** the authoritative prior status is restored and the error is presented

### Requirement: Confirm game deletion

Deleting a game SHALL require explicit confirmation describing removal of its maps and owned content while retaining library media. Navigation back to the list and cache removal SHALL follow server success. Failure SHALL preserve the game and offer an error message.

#### Scenario: Cancel or fail deletion

- **WHEN** deletion is cancelled or rejected
- **THEN** the game remains available and cancellation sends no delete request

### Requirement: Protect editor changes

Editors SHALL confirm navigation that would discard unsaved changes and prevent leaving during active submission. Loading, errors, controls and confirmation dialogs SHALL remain keyboard accessible and usable on narrow screens.

#### Scenario: Reject navigation

- **WHEN** the operator declines discarding unsaved changes
- **THEN** the editor retains its current values and route

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
