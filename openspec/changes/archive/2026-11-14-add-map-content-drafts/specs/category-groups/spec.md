# Spec Delta

## MODIFIED Requirements

### Requirement: Create a category group at the end

POST SHALL accept name, optional color, optional status (`draft` or `published`, defaulting to `draft` when omitted), optional nullable iconAssetId and optional nullable beforeId. Omitted/null beforeId SHALL append; a valid sibling anchor SHALL insert before it. HTTP 200 SHALL return the complete group with string status, empty categories, markersCount 0 and nullable icon. UUID v7, mapId, positive order and equal UTC timestamps SHALL be server-owned. Numeric order input SHALL return 400; null status SHALL return 422.

#### Scenario: Allocate order independently per map

- **WHEN** groups are created on two sibling maps
- **THEN** each receives a valid position only in its own map
- **AND** competing structural writes serialize without duplicate placement or lost groups

#### Scenario: First group

- **WHEN** a group is created on an empty map without an icon or status
- **THEN** status is `draft`, order is 1, icon is null, markersCount is 0 and categories is []
- **AND** it survives reopening storage

#### Scenario: Create group with explicit status

- **WHEN** a group is created with status `published` (or `draft`)
- **THEN** the persisted and returned group has the supplied status

#### Scenario: Append after existing groups

- **WHEN** beforeId is omitted or null
- **THEN** the group appends while preserving existing relative order

#### Scenario: Concurrent appends

- **WHEN** differently named groups are created concurrently on the same map
- **THEN** each successful group persists in a distinct appended position

#### Scenario: Server-owned creation fields

- **WHEN** valid input additionally supplies id, mapId, createdAt or updatedAt
- **THEN** server identity, scope and timestamps remain authoritative
- **AND** numeric order input returns 400

### Requirement: Read groups in display order without pagination

GET /games/{gameId}/maps/{mapId}/category-groups SHALL return a plain ordered array of all groups with id, mapId, name, color, status, order, createdAt, updatedAt, nullable shared-image icon, markersCount and categories. Children SHALL contain complete category representations (including status) in display order. Empty collections SHALL be []; missing/foreign maps SHALL return 404. No pagination, alternate sorting, individual-group GET or dedicated reorder endpoint SHALL be introduced.

#### Scenario: Complete map-specific list

- **WHEN** sibling maps contain groups with equal names
- **THEN** one map's tree includes only its groups and categories, with mapId ownership

#### Scenario: Complete ordered list

- **WHEN** the collection is read
- **THEN** every group and child appears exactly once in server display order
- **AND** each group carries status (`draft` or `published`) and each category carries id, categoryGroupId, name, nullable icon, color, status (`draft` or `published`), order, markersCount, createdAt and updatedAt
- **AND** current image metadata and computed counts are read without per-node database queries

#### Scenario: Existing game without groups

- **WHEN** an existing map has no groups
- **THEN** HTTP 200 returns []
- **AND** an existing empty group has categories [] and markersCount 0

### Requirement: Update category group metadata and order through merge patch

PATCH SHALL accept optional name, color, status (`draft` or `published`), nullable iconAssetId and nullable beforeId and return the complete group with children and computed count. Omitted fields SHALL preserve state; null icon detaches, null beforeId appends, and null name, color, or status returns 422. Updating a group's status SHALL change only that group without cascading to its child categories or markers. Empty patches return 422; unknown/read-only fields, including numeric order and mapId, return 400. Changes SHALL be atomic and preserve id/mapId/createdAt.

#### Scenario: Immutable map membership and atomic failure

- **WHEN** a patch supplies mapId alongside a valid rename
- **THEN** HTTP 400 preserves all fields
- **AND** valid metadata or placement changes do not alter map/game timestamps

#### Scenario: Update one field or both

- **WHEN** valid metadata (including status) and placement are supplied
- **THEN** all changes commit together and HTTP 200 returns the complete group
- **AND** child categories and markers retain their existing statuses and timestamps, and other groups preserve metadata and timestamps even when their numeric order changes

#### Scenario: Preserve omitted fields and no-ops

- **WHEN** only beforeId is supplied
- **THEN** name, color, status and icon remain unchanged
- **AND** unchanged normalized metadata and final sequence preserve updatedAt without writing

#### Scenario: Invalid patches are atomic

- **WHEN** the body is empty, name, color, or status is null/invalid, or beforeId references self
- **THEN** HTTP 422 preserves every field and position

#### Scenario: Reject server-owned fields

- **WHEN** the body supplies id, mapId, order, categories, markersCount, timestamps or unknown fields
- **THEN** HTTP 400 preserves storage

### Requirement: Document the category group API

OpenAPI SHALL expose ListCategoryGroups, CreateCategoryGroup, UpdateCategoryGroup and DeleteCategoryGroup under Categories on existing map-scoped routes. It SHALL describe group and nested category `status` (`draft` | `published`), nested categories, nullable shared-image icons, computed markersCount, relative beforeId placement, read-only order and shared PATCH semantics. Category groups SHALL NOT remain a separate tag.

#### Scenario: Discover map-scoped CRUD

- **WHEN** a client reads OpenAPI
- **THEN** all four group operations require gameId/mapId and share the Categories tag with category mutations
- **AND** no group transfer, PUT, batch, pagination, dedicated reorder or individual GET is advertised

#### Scenario: Discover CRUD

- **WHEN** a client reads OpenAPI
- **THEN** list exposes a complete tree array, POST/PATCH a complete group with required string `status`, and DELETE empty 204
- **AND** CreateCategoryGroup and UpdateCategoryGroup expose optional `status` and relevant 400, 404, 409, 415, 422 and 500 ProblemDetails and name/placement/icon contracts are discoverable
