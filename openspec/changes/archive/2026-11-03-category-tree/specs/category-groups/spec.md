# Spec Delta

## MODIFIED Requirements

### Requirement: Create a category group at the end

POST SHALL accept name, optional nullable iconAssetId and optional nullable beforeId. Omitted/null beforeId SHALL append; a valid sibling anchor SHALL insert before it. HTTP 200 SHALL return the complete group with empty categories, markersCount 0 and nullable icon. UUID v7, mapId, positive order and equal UTC timestamps SHALL be server-owned. Numeric order input SHALL return 400.

#### Scenario: Allocate order independently per map

- **WHEN** groups are created on two sibling maps
- **THEN** each receives a valid position only in its own map
- **AND** competing structural writes serialize without duplicate placement or lost groups

#### Scenario: First group

- **WHEN** a group is created on an empty map without an icon
- **THEN** order is 1, icon is null, markersCount is 0 and categories is []
- **AND** it survives reopening storage

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

GET /games/{gameId}/maps/{mapId}/category-groups SHALL return a plain ordered array of all groups with id, mapId, name, order, createdAt, updatedAt, nullable shared-image icon, markersCount and categories. Children SHALL contain complete category representations in display order. Empty collections SHALL be []; missing/foreign maps SHALL return 404. No pagination, alternate sorting, individual-group GET or dedicated reorder endpoint SHALL be introduced.

#### Scenario: Complete map-specific list

- **WHEN** sibling maps contain groups with equal names
- **THEN** one map's tree includes only its groups and categories, with mapId ownership

#### Scenario: Complete ordered list

- **WHEN** the collection is read
- **THEN** every group and child appears exactly once in server display order
- **AND** each category carries id, categoryGroupId, name, nullable icon, color, order, markersCount, createdAt and updatedAt
- **AND** current image metadata and computed counts are read without per-node database queries

#### Scenario: Existing game without groups

- **WHEN** an existing map has no groups
- **THEN** HTTP 200 returns []
- **AND** an existing empty group has categories [] and markersCount 0

### Requirement: Update category group metadata and order through merge patch

PATCH SHALL accept optional name, nullable iconAssetId and nullable beforeId and return the complete group with children and computed count. Omitted fields SHALL preserve state; null icon detaches, null beforeId appends, and null name returns 422. Empty patches return 422; unknown/read-only fields, including numeric order and mapId, return 400. Changes SHALL be atomic and preserve id/mapId/createdAt.

#### Scenario: Immutable map membership and atomic failure

- **WHEN** a patch supplies mapId alongside a valid rename
- **THEN** HTTP 400 preserves all fields
- **AND** valid metadata or placement changes do not alter map/game timestamps

#### Scenario: Update one field or both

- **WHEN** valid metadata and placement are supplied
- **THEN** all changes commit together and HTTP 200 returns the complete group
- **AND** other groups preserve metadata and timestamps even when their numeric order changes

#### Scenario: Preserve omitted fields and no-ops

- **WHEN** only beforeId is supplied
- **THEN** name and icon remain unchanged
- **AND** unchanged normalized metadata and final sequence preserve updatedAt without writing

#### Scenario: Invalid patches are atomic

- **WHEN** the body is empty, name is null/invalid, or beforeId references self
- **THEN** HTTP 422 preserves every field and position

#### Scenario: Reject server-owned fields

- **WHEN** the body supplies id, mapId, order, categories, markersCount, timestamps or unknown fields
- **THEN** HTTP 400 preserves storage

### Requirement: Document the category group API

OpenAPI SHALL expose ListCategoryGroups, CreateCategoryGroup, UpdateCategoryGroup and DeleteCategoryGroup under Categories on existing map-scoped routes. It SHALL describe nested categories, nullable shared-image icons, computed markersCount, relative beforeId placement, read-only order and shared PATCH semantics. Category groups SHALL NOT remain a separate tag.

#### Scenario: Discover map-scoped CRUD

- **WHEN** a client reads OpenAPI
- **THEN** all four group operations require gameId/mapId and share the Categories tag with category mutations
- **AND** no group transfer, PUT, batch, pagination, dedicated reorder or individual GET is advertised

#### Scenario: Discover CRUD

- **WHEN** a client reads OpenAPI
- **THEN** list exposes a complete tree array, POST/PATCH a complete group and DELETE empty 204
- **AND** relevant 400, 404, 409, 415, 422 and 500 ProblemDetails and name/placement/icon contracts are discoverable

### Requirement: Support explicit duplicate orders and deterministic ties

Group order SHALL be server-owned and positive. beforeId SHALL place a group before a sibling of the same map; null SHALL append. Missing/foreign anchors return 404, malformed GUIDs 400 and self anchors 422. Structural mutations SHALL serialize and atomically maintain sibling order. Existing duplicate/gapped orders SHALL migrate in order/createdAt/id ascending sequence. Metadata-only updates SHALL preserve position.

#### Scenario: Ties overflow and map independence

- **WHEN** legacy orders contain duplicates, gaps or Int32.MaxValue
- **THEN** migration normalizes each map independently without changing visible sequence or identities
- **AND** later appends do not depend on the previous legacy maximum

#### Scenario: Explicit order and duplicates

- **WHEN** numeric order is supplied to creation or PATCH
- **THEN** HTTP 400 preserves data
- **AND** valid relative placement can shift sibling order but not sibling metadata timestamps

#### Scenario: Stable tie breaking

- **WHEN** legacy groups have equal order and createdAt
- **THEN** ID ascending determines their migration sequence

#### Scenario: No room for automatic append

- **WHEN** a migrated map previously had an Int32.MaxValue order
- **THEN** creating a group without beforeId appends normally using normalized server order

#### Scenario: Concurrent moves and no-ops

- **WHEN** competing moves target one map
- **THEN** each successful operation applies atomically to the committed sequence without lost or duplicated groups
- **AND** a move preserving the final sequence does not change timestamps

## ADDED Requirements

### Requirement: Attach optional group library icons

Groups SHALL support optional committed library images through iconAssetId. Creation omission/null SHALL yield icon null; PATCH omission SHALL preserve and null SHALL detach. Malformed GUIDs return 400, missing images 404 and incompatible assets 422. Image references SHALL participate in media deletion guards. Detachment or deletion of groups SHALL retain media bytes.

#### Scenario: Resolve and protect a group icon

- **WHEN** a group references a raster or accepted SVG image
- **THEN** tree and mutation responses include its current canonical image metadata
- **AND** deleting the asset returns 409 while the group or another consumer references it

#### Scenario: Remove or reject an icon

- **WHEN** the icon is cleared
- **THEN** the response contains icon null and other consumers retain their image
- **WHEN** a replacement is missing or incompatible alongside a rename
- **THEN** the complete mutation fails without partial changes

### Requirement: Return aggregate group marker counts

Every group representation SHALL include read-only markersCount equal to the current persisted marker counts of all its categories. Empty groups SHALL return 0. Counts SHALL be derived from marker ownership rather than stored counters.

#### Scenario: Transfer a populated category

- **WHEN** a category with N markers moves between groups of one map
- **THEN** the next tree read decreases the source count by N and increases the destination by N
- **AND** the category marker count and marker ownership remain unchanged

#### Scenario: Marker and category mutations

- **WHEN** markers are created, transferred or deleted, or categories are deleted
- **THEN** subsequent tree and group responses reflect current counts
- **AND** failed operations do not alter counts and attempts to PATCH markersCount return 400

### Requirement: Persist group colors

Groups SHALL expose a non-null color in tree and mutation responses. Color SHALL accept #RGB or #RRGGBB and normalize to uppercase #RRGGBB. Omitted creation color SHALL default to #F59E0B for compatibility. PATCH omission SHALL preserve color; explicit null or invalid color SHALL return 422 atomically. Existing groups SHALL receive #F59E0B through an additive migration without changing identity, ownership or timestamps.

#### Scenario: Set and edit group color

- **WHEN** a group is created with #abc and later patched to #123456
- **THEN** responses and reloaded tree return #AABBCC followed by #123456
- **AND** rejected color changes preserve the entire previous group
