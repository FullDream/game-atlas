# category-groups Specification

## Purpose

Maintain game-owned category groups with validated unique names, complete CRUD, and a positive integer display order editable on individual groups.

## Requirements

### Requirement: Scope category groups to an existing game
The system SHALL expose unauthenticated category-group operations under `/games/{gameId}/category-groups` for both draft and published games. Each group SHALL belong to exactly one game. A valid request for a missing game, missing group, or group owned by another game SHALL return HTTP 404 ProblemDetails without changing storage.

#### Scenario: Manage either game status
- **WHEN** a client manages groups of an existing draft or published game without authentication
- **THEN** the game status does not prevent the requested operation

#### Scenario: Missing parent
- **WHEN** an otherwise valid create, list, update, or delete request targets a nonexistent game
- **THEN** the response is HTTP 404 ProblemDetails rather than a successful empty list or an orphaned group

#### Scenario: Missing or foreign group
- **WHEN** an otherwise valid rename or delete request identifies a missing group or a group belonging to another game
- **THEN** the response is HTTP 404 ProblemDetails and no group's metadata changes

### Requirement: Create a category group at the end
POST to the group collection SHALL accept name and optional order and return HTTP 200 with id, gameId, name, order, createdAt, and updatedAt. Order SHALL be a positive 32-bit integer; repeated values SHALL be allowed. If omitted, it SHALL default to the current maximum plus one, or 1 for an empty collection. Explicit null or nonpositive order SHALL return 422. The server SHALL generate UUID v7, derive gameId from the route, and set equal UTC timestamps.

#### Scenario: First group
- **WHEN** a client creates a valid group in a game with no groups
- **THEN** the group has order 1 and all six server-generated or validated fields are returned
- **AND** the stored group survives reopening the database

#### Scenario: Append after existing groups
- **WHEN** a client creates a group in a game whose greatest stored order is 4
- **THEN** the new group has order 5 without changing other groups or their timestamps

#### Scenario: Concurrent appends
- **WHEN** concurrent valid requests create differently named groups in the same game
- **THEN** successfully created groups receiving default orders have distinct positions at the end and neither replaces another group

#### Scenario: Server-owned creation fields
- **WHEN** a valid creation body also supplies id, gameId, createdAt, or updatedAt
- **THEN** those additional fields do not control the stored identity, route ownership, order, or timestamps

### Requirement: Validate and normalize group names
Creation and renaming SHALL trim surrounding name whitespace, preserve the remaining spelling for display, and require 1 through 200 characters after trimming. Missing, null, empty, whitespace-only, or overlength names SHALL produce HTTP 422 validation ProblemDetails without persisting changes.

#### Scenario: Trim and preserve spelling
- **WHEN** a valid name is `  Resources  ` or `  Ресурсы  `
- **THEN** the stored and returned name is respectively `Resources` or `Ресурсы`

#### Scenario: Invalid names and exact boundary
- **WHEN** creation or renaming supplies an absent required name, null, an empty or whitespace-only string, or more than 200 trimmed characters
- **THEN** the response is HTTP 422 with name field errors and storage is unchanged
- **AND** an otherwise valid name of exactly 200 trimmed characters is accepted

### Requirement: Enforce group name uniqueness within a game
Groups within one game SHALL have distinct names after surrounding whitespace trimming and culture-independent case normalization, including Latin and Cyrillic letter case. A conflict on creation or renaming SHALL return HTTP 409 ProblemDetails with name errors and no partial changes, including conflicts detected during saving. The same name SHALL remain permitted in different games.

#### Scenario: Latin and Cyrillic case conflicts
- **WHEN** a game has `Resources` or `Ресурсы` and a request creates another group named ` resources ` or ` ресурсы ` respectively
- **THEN** the response is HTTP 409 ProblemDetails and only the original group remains

#### Scenario: Rename to another group's name
- **WHEN** a rename would give a group the normalized name of another group in the same game
- **THEN** the response is HTTP 409 and both groups retain all prior fields

#### Scenario: Keep own name or change its displayed case
- **WHEN** a group is renamed to its own normalized name with different displayed letter case
- **THEN** the request succeeds unless another group owns that name
- **AND** a real displayed-name change updates updatedAt while mere surrounding whitespace does not

#### Scenario: Other game and concurrent conflict
- **WHEN** the same normalized name is created in different games
- **THEN** both creations are permitted
- **AND** concurrent same-name creations within one game cannot both succeed; the uniqueness failure returns 409

### Requirement: Read groups in display order without pagination
GET on the collection SHALL return HTTP 200 with a JSON array of all groups belonging to that game, ordered by order, then createdAt, then id, all ascending. No individual-group GET, batch update, or dedicated reorder endpoint SHALL be exposed. No pagination envelope, pagination controls, search, or alternate sorting SHALL be introduced.

#### Scenario: Complete ordered list
- **WHEN** the game has multiple groups and a client reads its collection
- **THEN** every group is returned exactly once in persisted display order with all six fields
- **AND** groups belonging to other games are absent

#### Scenario: Existing game without groups
- **WHEN** a client lists an existing game's empty group collection
- **THEN** the response is HTTP 200 with `[]`

### Requirement: Update category group metadata and order through merge patch
PATCH `/{id}` SHALL allow name and order to change and return HTTP 200 with the complete group DTO. Both fields SHALL be optional; omission preserves the stored value. Order SHALL be a positive 32-bit integer and need not be unique. Empty objects or null/nonpositive order SHALL return 422. Unknown or immutable fields SHALL return 400. Updates SHALL be atomic for the target group, preserve id, gameId, and createdAt, and change updatedAt only for effective changes.

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
- **WHEN** the body supplies id, gameId, timestamps, or unknown properties
- **THEN** the response is HTTP 400 and storage remains unchanged

### Requirement: Permanently delete groups and cascade game deletion
DELETE `/{id}` SHALL physically remove the scoped group and return HTTP 204 without a body. Repeated deletion SHALL return 404. Surviving groups SHALL keep their order, stored positions, and timestamps; gaps in positions SHALL be allowed. Deleted names SHALL become reusable. Deleting a game SHALL physically delete all its groups while preserving other games and their groups.

#### Scenario: Delete a middle group
- **WHEN** a group at order 2 is deleted between groups at positions 1 and 3
- **THEN** the response is an empty HTTP 204 and that group is absent after reopening storage
- **AND** positions 1 and 3 and their timestamps remain unchanged, and the deleted name can be reused

#### Scenario: Repeat group deletion
- **WHEN** a client repeats deletion of an already removed group
- **THEN** the response is HTTP 404 ProblemDetails

#### Scenario: Delete owning game
- **WHEN** the existing game DELETE succeeds for a game with groups
- **THEN** it still returns HTTP 204 and all groups of that game are physically removed
- **AND** another game's groups and metadata remain unchanged

### Requirement: Apply the shared PATCH media and shape contract
Updates SHALL accept application/merge-patch+json and application/json with identical JSON Merge Patch semantics, including charset parameters. Arrays SHALL be replaced whole. Other PATCH media types SHALL return 415. Invalid JSON, non-object bodies, invalid GUID representations, incompatible field types, and unknown fields SHALL return 400. PUT SHALL NOT be exposed.

#### Scenario: Equivalent media types
- **WHEN** equivalent valid update bodies use either supported media type, with or without charset=utf-8
- **THEN** they apply the same operation and validation rules
- **AND** omitted name or order fields retain their current values

#### Scenario: Unsupported media type
- **WHEN** the group PATCH operation uses text/plain or application/json-patch+json
- **THEN** the response is HTTP 415 ProblemDetails without storage changes

#### Scenario: Invalid shape or representation
- **WHEN** a PATCH body is malformed JSON, null, a scalar, an array, or contains an invalid field type, invalid GUID, or unknown property
- **THEN** the response is HTTP 400 ProblemDetails without storage changes

### Requirement: Persist group mutations safely
Successful group mutations SHALL be durable. Default order allocation SHALL be serialized, and a group update SHALL NOT persist partial metadata changes. Expected validation and conflict failures SHALL use their specified ProblemDetails responses. Unexpected storage failures SHALL return safe HTTP 500 without SQL, connection, or exception details; unrelated failures SHALL NOT become name conflicts.

#### Scenario: Failure during group update persistence
- **WHEN** saving a group update fails before its transaction commits
- **THEN** the response is safe HTTP 500 ProblemDetails and the group retains its prior name, position, and timestamps

#### Scenario: Other persistence failure
- **WHEN** a group operation encounters an unrelated library or storage failure
- **THEN** the response is HTTP 500 without internal details rather than 409 for a duplicate name

### Requirement: Document the category group API
OpenAPI SHALL describe all four operations, complete DTOs, GUID path arguments, name bounds, unpaginated arrays, both supported PATCH media types, optional-field semantics, and operation-specific failures. Operation IDs SHALL be CreateCategoryGroup, ListCategoryGroups, UpdateCategoryGroup, DeleteCategoryGroup. Existing Games contracts SHALL remain compatible.

#### Scenario: Discover CRUD
- **WHEN** a client reads OpenAPI
- **THEN** it exposes the four operation IDs, 200 DTO or array responses, empty DELETE 204, and ProblemDetails for relevant 400, 404, 409, 415, 422, and 500 responses
- **AND** name constraints and positive integer order bounds are discoverable without manual string descriptions replacing available validation metadata
- **AND** list has no pagination parameters or pagination envelope and PUT is absent

### Requirement: Support explicit duplicate orders and deterministic ties
Creation and updates SHALL allow the same positive order for multiple groups. Listing SHALL resolve ties by createdAt and then id ascending. Changing one group SHALL NOT shift or renumber other groups. Independently issued group updates SHALL commit independently; the API SHALL NOT promise atomic multi-group movement.

#### Scenario: Explicit order and duplicates
- **WHEN** a group is created or updated with an order already used in its game
- **THEN** the request succeeds subject to the other metadata rules and other groups remain unchanged

#### Scenario: Stable tie breaking
- **WHEN** groups have equal order and creation timestamps
- **THEN** their ID ascending determines their list sequence

#### Scenario: No room for automatic append
- **WHEN** order is omitted at creation and the maximum stored order is the greatest positive 32-bit integer
- **THEN** the response is HTTP 422 without creating a group; the client can supply an explicit positive order
