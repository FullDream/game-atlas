# Spec Delta

## MODIFIED Requirements

### Requirement: Present the map editing workspace

The editor SHALL show the routed map with styles and a category tree in the existing left panel and a central interactive viewer. It SHALL retain shell navigation and breadcrumbs, distinguish map loading/missing/failure and style empty/not-ready states, and keep category loading/errors local to their section. Marker editing and the right details editor SHALL remain outside this stage.

#### Scenario: Open and resize the workspace

- **WHEN** the operator opens a map on desktop
- **THEN** styles and categories appear in the same left panel beside the map
- **WHEN** the viewport becomes narrow
- **THEN** a named panel toggle exposes both sections and all controls and dialogs remain reachable

#### Scenario: Recover from a request failure

- **WHEN** the map request fails
- **THEN** the appropriate error or missing-resource state appears with explicit retry when recoverable
- **WHEN** only category retrieval fails
- **THEN** its section offers retry while loaded styles and the viewer remain usable

## ADDED Requirements

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
