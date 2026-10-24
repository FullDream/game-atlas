# Categories Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Scope category operations to a game and current group`
- TO: `### Requirement: Scope category operations to a game map and current group`
- FROM: `### Requirement: Transfer categories only between groups of the same game`
- TO: `### Requirement: Transfer categories only between groups of the same map`

## MODIFIED Requirements

### Requirement: Scope category operations to a game map and current group
Category operations SHALL use /games/{gameId}/maps/{mapId}/category-groups/{groupId}/categories without authentication for draft or published games. Each category SHALL belong to one group of that map. Missing or foreign game/map/group/category scope SHALL return HTTP 404 without changes. Maps with zero styles SHALL support all category operations. Legacy game-wide category routes SHALL NOT remain active.

#### Scenario: Validate every ownership boundary
- **WHEN** an otherwise valid operation targets a group outside the route map or a category outside the route group
- **THEN** HTTP 404 is returned even if all resources belong to the same game
- **AND** a valid operation on a map without styles succeeds normally

#### Scenario: Existing parent in either game status
- **WHEN** categories are managed in an existing group of the route map, whose game is draft or published
- **THEN** the operation is permitted without authentication or requiring styles

#### Scenario: Missing or foreign resource
- **WHEN** an otherwise valid request targets a missing game, missing group, group of another map, missing category, or category outside the route group
- **THEN** it returns HTTP 404 and changes no storage

### Requirement: Transfer categories only between groups of the same map
PATCH SHALL identify the current group in the route and the destination through categoryGroupId. The destination SHALL belong to the same route map. Missing/foreign destination groups SHALL return 404, including groups of sibling maps in the same game. Name uniqueness SHALL use the resulting destination/name. Transfers SHALL preserve identity, createdAt, and omitted order and commit all changes atomically.

#### Scenario: Transfer within one map
- **WHEN** a category moves to another group of the same map
- **THEN** it appears only under the destination route and retains its markers, ID, creation time, and omitted order
- **AND** destination name conflicts return 409 without partial changes; a simultaneous unique rename may succeed

#### Scenario: Refuse a sibling map destination
- **WHEN** categoryGroupId identifies a group belonging to another map of the same game
- **THEN** HTTP 404 preserves source/destination membership, markers, counts, coordinates, media, and timestamps

#### Scenario: Successful transfer and subsequent routes
- **WHEN** a valid patch transfers a category to another group of the same map
- **THEN** HTTP 200 returns the destination categoryGroupId, the old list excludes the category, and the new list includes it
- **AND** later PATCH or DELETE under the old group returns HTTP 404 while the new group route addresses it
- **AND** an omitted order is preserved even when it duplicates a destination order

#### Scenario: Transfer conflict and combined rename
- **WHEN** the resulting name conflicts with a destination category
- **THEN** HTTP 409 preserves the source membership and all fields
- **AND** a transfer with a simultaneous valid rename succeeds when the resulting name is unique in the destination

#### Scenario: Missing or other-game destination
- **WHEN** categoryGroupId identifies a missing group or a group of another map
- **THEN** HTTP 404 leaves all source and destination data unchanged

### Requirement: Update category state atomically through merge patch
PATCH SHALL accept optional name, icon, color, order, and categoryGroupId and return HTTP 200 with the unchanged nine-field category representation. Omission SHALL preserve values; null mutable values and an empty patch SHALL return 422. Failed updates SHALL persist no partial changes. Identity and createdAt SHALL remain immutable; updatedAt SHALL change only for effective category changes. Parent map/game/group timestamps SHALL remain unchanged.

#### Scenario: Preserve parent state and no-ops
- **WHEN** a valid patch changes category metadata or transfers within its map
- **THEN** only effective category state and its own updatedAt change
- **AND** normalized no-ops preserve its timestamp without affecting either parent map or game

#### Scenario: Single or combined changes
- **WHEN** a client supplies one or more valid mutable values
- **THEN** they are applied together, with omitted values preserved and only the target category's updatedAt changed
- **AND** game, map, group, and other category metadata and timestamps remain unchanged

#### Scenario: No effective change
- **WHEN** supplied values equal stored values after name and icon trimming and color normalization, including the same group ID
- **THEN** HTTP 200 returns the current representation without changing updatedAt or writing a category update

#### Scenario: Atomic rejection
- **WHEN** a patch is empty or contains an invalid value alongside otherwise valid changes
- **THEN** the response is the relevant validation failure and no field or membership changes

### Requirement: Physically delete categories and cascade parent deletion
DELETE SHALL physically remove the scoped category and its current markers/media records and return empty HTTP 204. Repeated deletion SHALL return 404. Surviving categories SHALL retain order/timestamps and deleted names SHALL be reusable. Group, map, and game deletion SHALL cascade through current ownership. Unrelated maps and referenced marker-media files SHALL remain unchanged.

#### Scenario: Parent deletion and transferred content
- **WHEN** a map is deleted after a category was transferred between its groups
- **THEN** its current categories, markers, and media records are removed
- **AND** deleting only the old source group does not remove a transferred-out category, and another map's data survives

#### Scenario: Delete a category and reuse its name
- **WHEN** a middle category is deleted
- **THEN** it is absent after reopening storage, HTTP 204 has no body, its name can be reused, and surviving categories keep their fields and any order gaps
- **AND** deleting it again returns HTTP 404

#### Scenario: Cascade group and game deletion
- **WHEN** an existing group or game is physically deleted through its existing API
- **THEN** categories currently belonging to that group or the game's groups are also physically removed
- **AND** unrelated groups and games retain their categories and metadata
- **AND** a transferred category follows its current group and survives deletion of its former group

### Requirement: Document the category API and preserve existing contracts
OpenAPI SHALL describe CreateCategory/ListCategories/UpdateCategory/DeleteCategory on map-scoped routes, all GUID parents, nine-field DTOs, existing validation/count/order rules, same-map transfers, both PATCH media types, and ProblemDetails. Category DTOs SHALL NOT acquire redundant mapId/gameId fields. Legacy game-wide routes SHALL be absent; unrelated category behavior SHALL remain unchanged.

#### Scenario: Discover map-scoped categories
- **WHEN** a client reads OpenAPI
- **THEN** category routes require gameId/mapId/groupId, categoryGroupId remains the ownership field, and markersCount remains read-only
- **AND** icons/colors/names, array ordering, merge-patch behavior, and lack of individual GET/pagination/batch/reorder remain unchanged

#### Scenario: Discover all four operations
- **WHEN** a client reads OpenAPI
- **THEN** POST and PATCH document HTTP 200 category responses, GET documents a plain array, DELETE documents empty HTTP 204, and relevant 400, 404, 409, 415, 422, and 500 errors are documented
- **AND** schemas expose name and icon bounds, hexadecimal color format, positive order, and optional PATCH fields without exposing internal normalization or optional-wrapper fields
- **AND** no individual-category GET, PUT, batch, pagination, or reorder operation is advertised
