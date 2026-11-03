# Spec Delta

## ADDED Requirements

### Requirement: Read categories through one map tree

Maps data access SHALL expose one map-scoped category tree query, retrieving groups and nested categories together. Groups and categories SHALL share the Categories API tag and generated transport. Consumers SHALL NOT issue per-group category collection reads. Existing shared platform/domain boundaries and explicit HTTP generation SHALL remain in force.

#### Scenario: Open an editor with multiple groups

- **WHEN** the category section loads
- **THEN** one tree request retrieves all groups, categories, icons and counts for that game/map
- **AND** expanding a group requires no additional read

#### Scenario: Regenerate the unified API

- **WHEN** maps API generation runs against the revised backend document
- **THEN** one Categories transport contains group CRUD and category mutations
- **AND** a separate Category groups service and ListCategories operation are absent
- **AND** normal builds still require no running backend

### Requirement: Reconcile optimistic category tree edits

Known category trees SHALL update optimistically for rename and placement, including membership and affected group counts. Data access SHALL cancel obsolete reads, reconcile server responses and safely recover from failure. Missing baselines SHALL NOT fabricate resources. Older reads, failures or responses SHALL NOT discard newer successful changes. Mutations within one map SHALL share serialization scope.

#### Scenario: Move and persist a category

- **WHEN** a category moves within or between groups
- **THEN** its cached location updates immediately and categoryGroupId and group counts remain consistent
- **AND** success accepts authoritative metadata and reconciles normalized sibling orders

#### Scenario: Fail one write after another succeeded

- **WHEN** a failed mutation overlaps another successful change
- **THEN** recovery preserves the accepted change
- **AND** ambiguous state triggers only affected tree reconciliation

#### Scenario: No cached baseline

- **WHEN** a mutation starts without a known tree
- **THEN** no optimistic group or category is invented
- **AND** success reconciles or invalidates the affected map tree

#### Scenario: Cancel stale reads and leave the editor

- **WHEN** a read predates a tree mutation or the editor switches maps
- **THEN** obsolete results cannot overwrite the changed tree or another map's state

### Requirement: Reconcile category creation and deletion after persistence

Creation SHALL use server-owned IDs and authoritative returned resources. Deletion SHALL retain resources until success. Successful CRUD SHALL update known tree membership, counts and icons when safely reconstructable; otherwise only the affected tree SHALL be invalidated. Unrelated map and style caches SHALL remain intact.

#### Scenario: Create and delete a group or category

- **WHEN** creation succeeds
- **THEN** its authoritative returned item is added once to the known tree without a fabricated server ID
- **WHEN** deletion succeeds
- **THEN** its subtree is removed and counts are reconciled
- **AND** failed creation/deletion leaves persisted tree membership unchanged

#### Scenario: Apply server normalization

- **WHEN** a successful mutation changes other siblings' numeric positions
- **THEN** the cached affected sibling arrays agree with the server's returned item placement
- **AND** if that cannot be derived safely, a scoped tree refresh reconciles them

### Requirement: Keep category transport independent of UI

Category data access SHALL expose typed query/mutation options and preserve HTTP errors and cancellation without opening dialogs, showing toasts or navigating. Features SHALL own those reactions without replacing data-access reconciliation callbacks.

#### Scenario: Handle rejected updates

- **WHEN** the server returns validation, name-conflict or transport errors
- **THEN** data access preserves the original failure while recovering affected caches
- **AND** the feature decides how to present the error and preserve form input
