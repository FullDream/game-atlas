# Category Groups Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Scope category groups to an existing game`
- TO: `### Requirement: Scope category groups to an existing map`
- FROM: `### Requirement: Enforce group name uniqueness within a game`
- TO: `### Requirement: Enforce group name uniqueness within a map`
- FROM: `### Requirement: Permanently delete groups and cascade game deletion`
- TO: `### Requirement: Permanently delete groups and cascade parent deletion`

## MODIFIED Requirements

### Requirement: Scope category groups to an existing map
Group operations SHALL use /games/{gameId}/maps/{mapId}/category-groups without authentication for draft or published games. Each group SHALL belong to exactly one map through immutable mapId. Valid requests for a missing game/map/group, a map outside the game, or a group outside the map SHALL return HTTP 404. A map SHALL NOT need any style before group operations.

#### Scenario: Empty map and isolated ownership
- **WHEN** a client creates or lists groups on an existing map with no styles
- **THEN** normal group behavior applies
- **AND** a group from another map returns 404, including a sibling map of the same game

#### Scenario: Manage either game status
- **WHEN** an existing map belongs to a draft or published game
- **THEN** the group operation is permitted without authentication or a style prerequisite

#### Scenario: Missing parent
- **WHEN** an otherwise valid create, list, update, or delete request targets a nonexistent map
- **THEN** the response is HTTP 404 ProblemDetails rather than a successful empty list or an orphaned group

#### Scenario: Missing or foreign group
- **WHEN** an otherwise valid rename or delete request identifies a missing group or a group belonging to another map
- **THEN** the response is HTTP 404 ProblemDetails and no group's metadata changes

### Requirement: Create a category group at the end
POST SHALL accept name and optional positive 32-bit order and return HTTP 200 with id, mapId, name, order, createdAt, and updatedAt. The server SHALL generate UUID v7 and equal UTC timestamps and derive mapId from the route. Omitted order SHALL use that map's maximum plus one or 1; duplicate positive values SHALL be allowed. Null/nonpositive order SHALL return 422. Additional creation properties SHALL NOT override ownership or server metadata.

#### Scenario: Allocate order independently per map
- **WHEN** one map has maximum order 4 and a sibling map has no groups
- **THEN** their next default orders are 5 and 1 respectively without changing existing data
- **AND** serialized successful concurrent default appends to one map receive distinct appended orders

#### Scenario: First group
- **WHEN** a client creates a valid group in a map with no groups
- **THEN** the group has order 1 and all six server-generated or validated fields are returned
- **AND** the stored group survives reopening the database

#### Scenario: Append after existing groups
- **WHEN** a client creates a group in a map whose greatest stored order is 4
- **THEN** the new group has order 5 without changing other groups or their timestamps

#### Scenario: Concurrent appends
- **WHEN** concurrent valid requests create differently named groups in the same map
- **THEN** successfully created groups receiving default orders have distinct positions at the end and neither replaces another group

#### Scenario: Server-owned creation fields
- **WHEN** a valid creation body also supplies id, mapId, createdAt, or updatedAt
- **THEN** those additional fields do not control the stored identity, route ownership, order, or timestamps

### Requirement: Enforce group name uniqueness within a map
Group names SHALL be unique within their map after trimming and invariant case normalization, including Latin/Cyrillic case. Creation/renaming conflicts, including recognized save-time conflicts, SHALL return HTTP 409 with name errors and no partial changes. The same normalized name SHALL be allowed in different maps, including maps of the same game.

#### Scenario: Same map conflict and sibling map independence
- **WHEN** a map contains Resources or Ресурсы and receives ' resources ' or ' ресурсы '
- **THEN** HTTP 409 preserves existing data
- **AND** creating the same name in another map succeeds

#### Scenario: Rename and self-equality
- **WHEN** a group is renamed to its own normalized name
- **THEN** no self-conflict occurs; effective display-case changes update its timestamp while whitespace-only no-ops do not

#### Scenario: Latin and Cyrillic case conflicts
- **WHEN** a map has `Resources` or `Ресурсы` and a request creates another group named ` resources ` or ` ресурсы ` respectively
- **THEN** the response is HTTP 409 ProblemDetails and only the original group remains

#### Scenario: Rename to another group's name
- **WHEN** a rename would give a group the normalized name of another group in the same map
- **THEN** the response is HTTP 409 and both groups retain all prior fields

#### Scenario: Keep own name or change its displayed case
- **WHEN** a group is renamed to its own normalized name with different displayed letter case
- **THEN** the request succeeds unless another group owns that name
- **AND** a real displayed-name change updates updatedAt while mere surrounding whitespace does not

#### Scenario: Other game and concurrent conflict
- **WHEN** the same normalized name is created in different maps
- **THEN** both creations are permitted
- **AND** concurrent same-name creations within one map cannot both succeed; the uniqueness failure returns 409

### Requirement: Read groups in display order without pagination
GET SHALL return all groups of the route map as a plain array of six-field DTOs ordered by order, createdAt, then id ascending. An existing map without groups SHALL return []; missing/foreign maps SHALL return 404. No pagination, search, alternate sorting, individual-group GET, batch update, or reorder endpoint SHALL be introduced.

#### Scenario: Complete map-specific list
- **WHEN** sibling maps contain groups with equal names and orders
- **THEN** listing one map returns only its groups in deterministic order with mapId, never gameId as group ownership

#### Scenario: Complete ordered list
- **WHEN** the map has multiple groups and a client reads its collection
- **THEN** every group is returned exactly once in persisted display order with all six fields
- **AND** groups belonging to other maps are absent

#### Scenario: Existing game without groups
- **WHEN** a client lists an existing map's empty group collection
- **THEN** the response is HTTP 200 with `[]`

### Requirement: Update category group metadata and order through merge patch
PATCH SHALL allow only optional name and order and return HTTP 200 with the six-field group DTO. Omission SHALL preserve values; null/invalid name or null/nonpositive order and an empty object SHALL return 422. Unknown/read-only properties, including mapId/gameId, SHALL return 400. Updates SHALL be atomic, preserve identity/mapId/createdAt, and change only effective group state.

#### Scenario: Immutable map membership and atomic failure
- **WHEN** a patch supplies mapId to move a group, even alongside a valid name change
- **THEN** HTTP 400 preserves every field and ownership
- **AND** a valid name/order-only patch affects neither other groups nor parent map/game timestamps

#### Scenario: Update one field or both
- **WHEN** a client supplies a valid name, a positive integer order, or both for an existing group
- **THEN** the supplied changes are persisted together and the complete DTO is returned with HTTP 200
- **AND** other groups retain all their fields, including positions and timestamps

#### Scenario: Preserve omitted fields and no-ops
- **WHEN** only order is supplied
- **THEN** the name remains unchanged
- **AND** when supplied values equal the stored values after name trimming, updatedAt is preserved without writing

#### Scenario: Invalid patches are atomic
- **WHEN** the body is empty or explicitly supplies null, zero, or a negative order
- **THEN** the response is HTTP 422 and all fields remain unchanged even alongside a valid name change
- **AND** null or invalid name also returns 422 without persisting a supplied order change

#### Scenario: Reject server-owned fields
- **WHEN** the body supplies id, mapId, timestamps, or unknown properties
- **THEN** the response is HTTP 400 and storage remains unchanged

### Requirement: Permanently delete groups and cascade parent deletion
DELETE SHALL physically remove the scoped group and current categories, markers, and marker-media records and return empty HTTP 204. Repeated deletion SHALL return 404. Surviving groups SHALL retain order/timestamps and deleted names SHALL become reusable. Map deletion SHALL remove its groups, and game deletion SHALL remove groups through all its maps. Referenced media files SHALL NOT be deleted.

#### Scenario: Delete one map hierarchy
- **WHEN** a map or its game is deleted
- **THEN** owned groups and descendants disappear through current ownership
- **AND** unrelated maps/games and their groups remain unchanged

#### Scenario: Delete a middle group
- **WHEN** a group at order 2 is deleted between groups at positions 1 and 3
- **THEN** the response is an empty HTTP 204 and that group is absent after reopening storage
- **AND** positions 1 and 3 and their timestamps remain unchanged, and the deleted name can be reused

#### Scenario: Repeat group deletion
- **WHEN** a client repeats deletion of an already removed group
- **THEN** the response is HTTP 404 ProblemDetails

#### Scenario: Delete owning game
- **WHEN** game deletion succeeds for a game owning several maps with groups
- **THEN** empty HTTP 204 follows deletion of every owned group through its map
- **AND** other games and their content remain unchanged

### Requirement: Document the category group API
OpenAPI SHALL describe the four existing group operation IDs on map-scoped routes, all GUID parents, six-field DTOs containing mapId instead of gameId, name/order constraints, unpaginated arrays, shared PATCH semantics, and applicable ProblemDetails. Legacy game-wide group routes SHALL NOT be exposed.

#### Scenario: Discover map-scoped CRUD
- **WHEN** a client reads OpenAPI
- **THEN** ListCategoryGroups/CreateCategoryGroup/UpdateCategoryGroup/DeleteCategoryGroup require gameId and mapId paths and expose the revised group representation
- **AND** no group transfer, PUT, batch, pagination, or individual GET is advertised

#### Scenario: Discover CRUD
- **WHEN** a client reads OpenAPI
- **THEN** it exposes the four operation IDs, 200 DTO or array responses, empty DELETE 204, and ProblemDetails for relevant 400, 404, 409, 415, 422, and 500 responses
- **AND** name constraints and positive integer order bounds are discoverable without manual string descriptions replacing available validation metadata
- **AND** list has no pagination parameters or pagination envelope and PUT is absent

### Requirement: Support explicit duplicate orders and deterministic ties
Groups of one map SHALL allow duplicate positive orders. Listing SHALL break ties by createdAt then id ascending. Changing one group SHALL NOT renumber others. Independent updates SHALL commit independently. Omitted creation order at Int32.MaxValue SHALL return 422; explicit valid order SHALL remain permitted. Allocation SHALL NOT read the groups of other maps.

#### Scenario: Ties overflow and map independence
- **WHEN** one map's maximum order is Int32.MaxValue while a sibling map is empty
- **THEN** omitted order fails only for the first map and the sibling map can create order 1
- **AND** explicit duplicate orders within a map retain deterministic ordering

#### Scenario: Explicit order and duplicates
- **WHEN** a group is created or updated with an order already used in its map
- **THEN** the request succeeds subject to the other metadata rules and other groups remain unchanged

#### Scenario: Stable tie breaking
- **WHEN** groups have equal order and creation timestamps
- **THEN** their ID ascending determines their list sequence

#### Scenario: No room for automatic append
- **WHEN** order is omitted at creation and the maximum stored order is the greatest positive 32-bit integer
- **THEN** the response is HTTP 422 without creating a group; the client can supply an explicit positive order
