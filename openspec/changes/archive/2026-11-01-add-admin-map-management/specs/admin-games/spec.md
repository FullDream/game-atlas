## MODIFIED Requirements

### Requirement: Display game maps from summaries

The game editor Maps tab SHALL display the independent map-management page composed by application routing. Its map count SHALL remain accurate after successful map creation and deletion. Games and Maps feature libraries SHALL remain independent of one another. Viewer implementation SHALL remain outside this change; controls for unimplemented actions SHALL remain disabled.

#### Scenario: Open a game maps tab

- **WHEN** the operator opens or reloads an existing game's Maps tab
- **THEN** the current map-management list and accurate map count appear, including maps without previews
- **AND** a game with no maps shows zero and an empty state

#### Scenario: Reconcile mutations across game views

- **WHEN** map creation, rename or deletion succeeds
- **THEN** only the corresponding game-detail query is invalidated and its active view refreshes map summaries and counts without discarding unsaved game form fields
- **AND** Games list caches are neither modified nor invalidated by the map mutation

## ADDED Requirements

### Requirement: Show settings-specific header actions

Save changes SHALL appear only on General. Maps SHALL show Add map in its place. SEO and Danger zone SHALL NOT show the game Save changes action. Game breadcrumbs SHALL remain present on every tab and restore correctly when navigating between them.

#### Scenario: Change settings tabs

- **WHEN** the operator navigates from General to Maps and back
- **THEN** the workspace header switches between Save changes and Add map while retaining game breadcrumbs
- **AND** unsaved General input is preserved and opening a map modal does not submit the game form
