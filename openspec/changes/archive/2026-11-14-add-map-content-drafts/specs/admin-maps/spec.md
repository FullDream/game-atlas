# Spec Delta

## ADDED Requirements

### Requirement: Publish a map and its draft changes from the map editor header

The map editor workspace header SHALL replace `Back to maps` with a button targeting `POST /games/{gameId}/maps/{mapId}/publish`. It SHALL show enabled `Publish` when the map is `draft`, and `Publish changes` when the map is `published` (enabled only while at least one group, category, or marker is `draft`). Activating it SHALL optimistically set `status: 'published'` in map, group, category, and marker caches without refetching, and roll back on error.

#### Scenario: Publish a draft map from the header

- **WHEN** an operator opens a map with `status === 'draft'` and activates `Publish` in the workspace header
- **THEN** `POST /games/{gameId}/maps/{mapId}/publish` is sent, the map and all of its groups, categories, and markers optimistically reflect `published` status in cache, and upon error their previous cached statuses are restored

#### Scenario: Publish draft content changes on a published map

- **WHEN** a map has `status === 'published'` and at least one group, category, or marker on the map has `status === 'draft'`
- **THEN** the header button shows `Publish changes` and is enabled
- **AND** activating it optimistically marks all draft content on the map as `published` in cache without refetching and disables `Publish changes`

#### Scenario: Disable publish action when nothing is in draft

- **WHEN** a map and all of its category groups, categories, and markers have `status === 'published'`, or required map/category/marker data is loading, failed, or a publish request is pending
- **THEN** the header button is disabled

## MODIFIED Requirements

### Requirement: Browse game maps

The Maps page SHALL display every map of the routed game in server order, showing name, publication status badge (`Published` or `Draft`), initialized logical dimensions or a pending-first-style label, preview, style count and previews, createdAt and updatedAt. Missing previews SHALL have neutral fallbacks. No Main badge or fabricated category/marker counts SHALL appear.

#### Scenario: Open and reload the list

- **WHEN** an operator opens or reloads /games/{gameId}/maps
- **THEN** that game's full list loads with each map's `Published` or `Draft` status badge, dates and available style previews
- **AND** empty, loading and request-failure states are distinct, with creation and error retry actions as applicable

### Requirement: Create maps in a modal

The page SHALL offer Add map in the workspace header. Its modal SHALL require a trimmed name and client-supplied slug of 1..200 characters each, matching backend validation, and offer a `Published` switch (`hlm-switch`) that defaults to unchecked (`draft`), without width or height inputs. Successful creation SHALL close the modal and display the persisted blank map with the chosen status.

#### Scenario: Create a draft or published map

- **WHEN** an operator submits valid name and slug with the `Published` switch left unchecked (or toggled on)
- **THEN** the create request sends `status: 'draft'` (or `'published'`) and the created map appears in the list with the corresponding status badge

#### Scenario: Failed or cancelled creation

- **WHEN** creation fails validation, conflicts with an existing name or game-scoped slug, or encounters a transport error
- **THEN** the modal preserves input (including the switch state) and explains the failure
- **AND** cancellation sends no mutation and duplicate submission is prevented while pending

### Requirement: Rename maps in a modal

Each map SHALL offer editing in a modal with editable name, slug, and `Published` switch (`hlm-switch`) fields and read-only logical dimensions or a pending-first-style label. Saving SHALL send only changed mutable values (including `status` when toggled) and display authoritative returned data. An unchanged form SHALL NOT send an empty PATCH.

#### Scenario: Save and reject renames

- **WHEN** an operator saves a valid changed name, slug, or publication status
- **THEN** the modal closes after success and the list shows the returned name, status badge, and updatedAt
- **AND** a rejected save preserves input and explains the error without losing another successful change

### Requirement: Present the map editing workspace

The editor SHALL show the routed map with styles and a category tree in the existing left panel and a central interactive viewer. It SHALL collect each marker's active draft levels in order (`marker`, `category`, `group`) when the marker, its category, or its category group has `status === 'draft'`, and pass them to the viewer for corner-dot (`#F59E0B` > `#0EA5E9` > `#8B5CF6`) and multi-level tooltip rendering. It SHALL retain shell navigation and breadcrumbs, distinguish map loading/missing/failure and style empty/not-ready states, and keep category and marker loading/errors local. Marker editing SHALL use a bottom drawer on narrow screens and a right-side sheet on wide screens.

#### Scenario: Open and resize the workspace

- **WHEN** the operator opens a map on desktop
- **THEN** styles and categories appear in the same left panel beside the map and draft markers/categories/groups reflect their draft state on the canvas
- **WHEN** the viewport becomes narrow
- **THEN** a named panel toggle exposes both sections and all controls and dialogs remain reachable

#### Scenario: Recover from a request failure

- **WHEN** the map request fails
- **THEN** the appropriate error or missing-resource state appears with explicit retry when recoverable
- **WHEN** only category or marker retrieval fails
- **THEN** its local error offers retry while loaded styles and the viewer remain usable

#### Scenario: Resize an open marker editor

- **WHEN** the screen crosses the editor breakpoint with unsaved marker values
- **THEN** the appropriate drawer or sheet presents the same draft without data loss or duplicate submission

### Requirement: Browse a two-level category tree

The panel SHALL place Categories below styles and show groups with their category children, optional image icons, a `Draft` badge on any group or category whose `status === 'draft'` (omitting any status badge when `published`), and persisted marker counts. Group rows SHALL contain drag handle, expansion arrow, optional icon, name, optional `Draft` badge, count and actions menu. Child rows SHALL contain an indented drag handle, optional icon, name, optional `Draft` badge, count and actions menu. Missing icons SHALL NOT create broken-image indicators.

#### Scenario: Expand groups and inspect counts

- **WHEN** an operator expands or collapses a group
- **THEN** its children show or hide without a mutation or another category request
- **AND** groups and categories in `draft` status display a `Draft` badge while `published` items display no status badge, counts remain visible on collapsed groups, and names truncate without hiding badges, actions or counts

#### Scenario: Loading empty and error states

- **WHEN** categories initially load, the map has no groups, or a request fails
- **THEN** the section respectively displays matching row skeletons, an actionable empty state, or an error with retry
- **AND** an empty existing group remains a valid creation and drop destination

### Requirement: Manage category and group metadata

The Categories header SHALL offer creation of a group or category. Group menus SHALL offer add category, edit and delete; category menus SHALL offer edit and delete. Forms SHALL support names, optional library-image icons, color for both kinds, a `Published` switch (`hlm-switch`, defaulting to unchecked/`draft` on creation and reflecting current status on edit), and category destination group. In the category destination group select dropdown, groups with `status === 'draft'` SHALL display a `Draft` badge next to their name (and no badge when `published`). Save SHALL submit changed fields only; unchanged editing SHALL send no PATCH.

#### Scenario: Create a category without a group

- **WHEN** a map has no groups and the operator chooses Add category
- **THEN** the interface explains that a group is required and provides a group-creation action
- **AND** it does not invent or silently create a default group

#### Scenario: Create edit and clear an icon

- **WHEN** valid creation or editing (including setting or toggling the `Published` switch) is saved
- **THEN** the server result updates the tree (including any `Draft` badge) and the dialog closes after success
- **AND** icons can be omitted on creation or explicitly cleared during editing

#### Scenario: Reject or cancel a form

- **WHEN** submission fails validation, uniqueness checks or transport
- **THEN** entered values remain available and the failure is explained
- **WHEN** the operator cancels
- **THEN** no mutation is sent
- **AND** pending submission prevents duplicate requests

### Requirement: Select and edit a marker

Selecting a marker in Pan mode SHALL open its form with name, plain-text description, category, a `Published` switch (`hlm-switch`), ordered media and X/Y coordinates. The `Published` switch SHALL also be available when creating a single marker or configuring the repeated-placement marker template, defaulting to unchecked (`draft`). Name SHALL require 1..200 trimmed characters, description at most 5000 trimmed characters, and coordinates finite numbers. Category choices SHALL be grouped and display category icons in options and the selected value, with a colored pin fallback, and SHALL display a `Draft` badge next to draft group labels and draft category options (omitting badges for `published` items).

#### Scenario: Open and validate

- **WHEN** an operator selects an existing marker
- **THEN** its persisted values (including its `Published` switch state) appear, its category is identifiable by name and icon, draft groups/categories in the category select show `Draft` badges, and the marker is highlighted
- **AND** invalid fields prevent submission with accessible field errors
- **AND** no slug, notes or formatting toolbar is shown

### Requirement: Browse markers in a separate left accordion

The left panel SHALL include a separate Markers accordion with an independently scrolling list using the available content height while it is the sole expanded section. Rows SHALL display the category icon or colored fallback, marker name, category name, and a `Draft` badge when the marker, its category, or its category group has `status === 'draft'` (showing no badge when all three are `published`), and offer keyboard-accessible editing. Loading, failure/retry and empty states SHALL remain local to this section.

#### Scenario: Select a marker from the list

- **WHEN** an operator activates a marker row with pointer or keyboard
- **THEN** the existing marker editor opens for that point
- **AND** on narrow screens the content panel closes so the map remains visible above the Drawer

#### Scenario: Collapse and recover the list

- **WHEN** the Markers section is collapsed or expanded
- **THEN** at most one section is open and its content uses the available height and scrolls independently
- **AND** loading, empty and failed requests have distinct local presentations with retry on failure

### Requirement: Rename the map from Settings and Tools

Settings & Tools SHALL offer a unified map metadata form with labelled `Map name` and `Map slug` inputs, a `Published` switch (`hlm-switch`), and a single form-level `Save changes` submit button (without per-input inline save buttons). Names SHALL contain 1..200 trimmed characters. Only changed valid metadata (`name`, `slug`, `status`) SHALL be sent in a single PATCH request on submit; unchanged submission SHALL send no PATCH and the submit button SHALL be disabled when the form is pristine or pending. Failures SHALL preserve input and explain the error; pending saves SHALL prevent duplicate submission.

#### Scenario: Save a map name in the editor

- **WHEN** the operator edits map name, slug, and/or the `Published` switch in Settings & Tools and submits `Save changes`
- **THEN** a single PATCH with only the changed fields is sent and the returned values appear in the form, breadcrumbs, header publish button state, and affected map views and survive reload
- **AND** style selection, map geometry and marker editing remain intact

#### Scenario: Retain a name draft

- **WHEN** the operator edits fields in Settings & Tools and switches sections or hides and restores the panel
- **THEN** the unsaved form values remain available without a write
- **AND** import controls and other future tools are absent

#### Scenario: Reject or skip a save

- **WHEN** the name or slug is invalid, a save fails, or trimmed name, trimmed slug, and status are all unchanged
- **THEN** invalid input blocks submission, failure retains input with an accessible error, and unchanged input sends no PATCH
