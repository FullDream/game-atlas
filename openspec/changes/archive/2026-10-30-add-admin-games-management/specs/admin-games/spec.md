## Purpose

Let administrators browse and manage game metadata through directly addressable creation and editing pages within the existing administration shell.

## ADDED Requirements

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

Editors SHALL validate required name, slug and library image selection according to the API contract. Creation SHALL produce a draft and navigate to its persisted game page. Editing SHALL send only changed mutable fields and preserve unsaved input on request failure.

#### Scenario: Invalid or conflicting slug

- **WHEN** a slug is invalid or the server reports a slug conflict
- **THEN** the editor explains the failure and preserves entered values for correction

#### Scenario: Save metadata

- **WHEN** valid changed metadata is saved
- **THEN** authoritative values are displayed and the form becomes pristine
- **AND** unchanged editors do not send empty patches

### Requirement: Choose a library image

Game editors SHALL offer folder-aware selection of an existing library image with a preview. Videos and folders SHALL NOT be selectable as game covers.

#### Scenario: Cancel selection

- **WHEN** the operator cancels the image picker
- **THEN** the current editor selection remains unchanged

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
