# categories Specification

## Purpose

Manage categories within game-owned category groups, including recognizable icons and colors, display order, same-game transfers, and a read-only marker count.

## Requirements

### Requirement: Scope category operations to a game and current group
The system SHALL expose unauthenticated category operations under `/games/{gameId}/category-groups/{groupId}/categories` for draft and published games. Each category SHALL belong to exactly one group. Valid requests for missing or foreign parents or categories SHALL return HTTP 404 ProblemDetails without changes.

#### Scenario: Existing parent in either game status
- **WHEN** a client creates or lists categories in an existing group of the route game, whether draft or published
- **THEN** the operation is permitted without authentication

#### Scenario: Missing or foreign resource
- **WHEN** an otherwise valid request targets a missing game, missing group, group of another game, missing category, or category outside the route group
- **THEN** it returns HTTP 404 and changes no storage

### Requirement: Create categories with server-owned identity and timestamps
POST on the collection SHALL accept required name, icon, and color with optional order, returning HTTP 200 with the complete category representation. The server SHALL generate UUID v7, derive categoryGroupId from the route, and set equal UTC createdAt and updatedAt. Additional creation properties SHALL NOT override server-owned values.

#### Scenario: Successful creation
- **WHEN** a valid category is created in an existing group
- **THEN** its complete stored representation is returned and survives reopening storage
- **AND** id is UUID v7, categoryGroupId matches the route, and the timestamps are equal UTC instants

#### Scenario: Server-owned creation fields
- **WHEN** valid creation input additionally supplies id, categoryGroupId, gameId, timestamps, or markersCount
- **THEN** those fields do not override server-derived identity, membership, timestamps, or count

### Requirement: Validate and normalize category names
Creation and updates SHALL trim name whitespace, preserve display spelling, and require 1 through 200 characters after trimming. Missing required creation names, null, empty, whitespace-only, or overlength names SHALL return HTTP 422 validation ProblemDetails without changes.

#### Scenario: Name spelling and length boundaries
- **WHEN** a name is `  Resources  ` or `  Ресурсы  `
- **THEN** it is stored and returned as `Resources` or `Ресурсы`, respectively
- **AND** exactly 200 trimmed characters are accepted while 201 are rejected with HTTP 422 and name errors

#### Scenario: Required name
- **WHEN** creation omits name or creation or update supplies null, empty, or whitespace-only name
- **THEN** the response is HTTP 422 with name errors and storage remains unchanged

### Requirement: Enforce name uniqueness within the resulting group
Category names SHALL be unique within their group after trimming and culture-independent case normalization, including Latin and Cyrillic case. Creation, renaming, or transfer conflicts SHALL return HTTP 409 ProblemDetails with name errors, including conflicts detected while saving. Equal names SHALL be permitted in different groups.

#### Scenario: Case-insensitive conflicts and group scope
- **WHEN** a group contains `Resources` or `Ресурсы` and another category would receive ` resources ` or ` ресурсы ` respectively
- **THEN** the request returns HTTP 409 with no changes
- **AND** the same names remain permitted in other groups, including groups of the same game

#### Scenario: Own name and concurrent duplicate creation
- **WHEN** a category keeps its own normalized name or changes only its display case
- **THEN** no self-conflict occurs
- **AND** concurrent same-name creations in one group cannot both succeed; the duplicate returns HTTP 409

### Requirement: Accept icon references without restricting image formats
Icon SHALL be a required string of 1 through 2048 characters after trimming surrounding whitespace. Missing creation input, null, empty, whitespace-only, or overlength values SHALL return HTTP 422 with icon errors. The system SHALL accept absolute links and relative paths without URL, extension, image format, or remote content validation.

#### Scenario: Icon references
- **WHEN** valid icon values use an SVG, PNG, WebP, relative path, or a link without a file extension
- **THEN** the trimmed reference is stored and returned without fetching the resource

#### Scenario: Icon bounds
- **WHEN** a supplied icon has exactly 2048 characters after trimming
- **THEN** it is accepted
- **AND** missing required creation input, null, blank, or 2049 trimmed characters return HTTP 422 without changes

### Requirement: Validate and normalize icon color
Color SHALL be a required string in `#RGB` or `#RRGGBB` format, accepting ASCII hexadecimal digits in either case. Short values SHALL expand by duplicating each digit; storage and responses SHALL use uppercase `#RRGGBB`. Missing creation input, null, or invalid formats SHALL return HTTP 422 with color errors. Transparency and named colors SHALL NOT be supported.

#### Scenario: Normalize hexadecimal case
- **WHEN** a client supplies color `#ff8800`
- **THEN** the response and storage contain `#FF8800`

#### Scenario: Expand short colors and preserve equivalent no-ops
- **WHEN** creation or update supplies `#fff`, `#F80`, or `#aBc`
- **THEN** the stored and returned values are respectively `#FFFFFF`, `#FF8800`, or `#AABBCC`
- **AND** an update supplying an equivalent short or full color preserves updatedAt without an update write

#### Scenario: Reject other color representations
- **WHEN** a client omits color at creation or supplies null, blank, `red`, `#12`, `#1234`, `#12345`, `#FF880080`, whitespace around the value, a trailing newline, or non-hexadecimal characters
- **THEN** the response is HTTP 422 and storage remains unchanged

### Requirement: Support positive display order within each group
Order SHALL be a positive 32-bit integer and permit duplicates. Creation with omitted order SHALL use the group's maximum plus one, or 1 for an empty group. Null or nonpositive order SHALL return HTTP 422. Automatic allocation SHALL be serialized. An exhausted maximum SHALL require explicit order and return HTTP 422 when omitted. Changing one category SHALL NOT renumber others.

#### Scenario: Default append and concurrency
- **WHEN** order is omitted at creation
- **THEN** the first category receives 1 or the category receives the group's current maximum plus one
- **AND** concurrent successful default appends receive distinct orders without changing existing categories

#### Scenario: Explicit duplicates and overflow
- **WHEN** a positive order already used in the group is supplied
- **THEN** the request succeeds subject to other validation
- **AND** when the group's maximum is 2147483647, omitted creation order returns HTTP 422 while explicit positive order remains permitted

#### Scenario: Invalid order values
- **WHEN** null, zero, or negative order is supplied
- **THEN** the response is HTTP 422
- **AND** fractional numbers, strings, and values outside the 32-bit integer range return HTTP 400 without changes

### Requirement: List complete categories without pagination
GET on the collection SHALL return HTTP 200 with every category of the route group as a JSON array, ordered by order, createdAt, and id ascending. Each item SHALL contain id, categoryGroupId, name, icon, color, order, markersCount, createdAt, and updatedAt. No individual GET, pagination, search, alternate sorting, batch update, or reorder endpoint SHALL be exposed.

#### Scenario: Ordered complete collection
- **WHEN** an existing group contains categories, including duplicate orders and equal creation timestamps
- **THEN** every category is returned exactly once with all nine fields, ordered by order, then createdAt, then id ascending
- **AND** categories of other groups are absent

#### Scenario: Empty group
- **WHEN** an existing group has no categories
- **THEN** its list response is HTTP 200 with `[]`
- **AND** a missing or foreign group returns HTTP 404 instead

### Requirement: Update category state atomically through merge patch
PATCH `/{id}` SHALL accept optional name, icon, color, order, and categoryGroupId and return HTTP 200 with the complete resulting representation. Omitted fields SHALL remain unchanged; null for any mutable field SHALL return HTTP 422. Empty patches SHALL return HTTP 422. Failed updates SHALL persist no partial changes. Id and createdAt SHALL be immutable; updatedAt SHALL change only for effective changes.

#### Scenario: Single or combined changes
- **WHEN** a client supplies one or more valid mutable values
- **THEN** they are applied together, with omitted values preserved and only the target category's updatedAt changed
- **AND** game, group, and other category metadata and timestamps remain unchanged

#### Scenario: No effective change
- **WHEN** supplied values equal stored values after name and icon trimming and color normalization, including the same group ID
- **THEN** HTTP 200 returns the current representation without changing updatedAt or writing a category update

#### Scenario: Atomic rejection
- **WHEN** a patch is empty or contains an invalid value alongside otherwise valid changes
- **THEN** the response is the relevant validation failure and no field or membership changes

### Requirement: Transfer categories only between groups of the same game
A transfer SHALL use the current group in the PATCH route and the destination in categoryGroupId. The destination SHALL belong to the route game; missing or foreign destination groups SHALL return HTTP 404. Uniqueness SHALL be checked using the resulting name in the destination. Transfers SHALL preserve id and createdAt and preserve order unless supplied. All supplied changes SHALL commit atomically.

#### Scenario: Successful transfer and subsequent routes
- **WHEN** a valid patch transfers a category to another group of the same game
- **THEN** HTTP 200 returns the destination categoryGroupId, the old list excludes the category, and the new list includes it
- **AND** later PATCH or DELETE under the old group returns HTTP 404 while the new group route addresses it
- **AND** an omitted order is preserved even when it duplicates a destination order

#### Scenario: Transfer conflict and combined rename
- **WHEN** the resulting name conflicts with a destination category
- **THEN** HTTP 409 preserves the source membership and all fields
- **AND** a transfer with a simultaneous valid rename succeeds when the resulting name is unique in the destination

#### Scenario: Missing or other-game destination
- **WHEN** categoryGroupId identifies a missing group or a group of another game
- **THEN** HTTP 404 leaves all source and destination data unchanged

### Requirement: Apply the shared PATCH media and input contract
PATCH SHALL accept application/merge-patch+json and application/json with identical JSON Merge Patch semantics, allowing charset parameters. Other media types SHALL return HTTP 415. Malformed JSON, non-object bodies, incompatible field types, malformed GUIDs, and unknown or read-only fields SHALL return HTTP 400. PUT SHALL NOT be exposed.

#### Scenario: Supported and unsupported media
- **WHEN** equivalent patches use either supported media type with or without charset=utf-8
- **THEN** the same updates and validation rules apply
- **AND** application/json-patch+json and text/plain return HTTP 415 without changes

#### Scenario: Invalid shape or read-only fields
- **WHEN** PATCH input is malformed JSON, null, an array, a scalar, has incompatible types or malformed categoryGroupId, or supplies id, gameId, markersCount, timestamps, or another unknown field
- **THEN** HTTP 400 ProblemDetails is returned without changes
- **AND** a syntactically valid GUID identifying no destination, including the all-zero GUID, receives HTTP 404 on an otherwise valid transfer

### Requirement: Return a read-only marker count
Every category representation SHALL include integer markersCount with value 0 until marker functionality is introduced. Clients SHALL NOT control the count. This change SHALL NOT introduce marker storage or a persisted category counter.

#### Scenario: Count across operations
- **WHEN** a category is created, listed, updated, or transferred
- **THEN** its representation includes markersCount equal to 0
- **AND** PATCH attempts to change that field return HTTP 400

### Requirement: Physically delete categories and cascade parent deletion
DELETE `/{id}` SHALL physically remove the scoped category and return HTTP 204 without a body. Repeated deletion SHALL return HTTP 404. Surviving categories SHALL retain order and timestamps, and deleted names SHALL become reusable. Deleting a group SHALL remove its current categories; deleting a game SHALL remove categories through its groups. Unrelated data SHALL be preserved.

#### Scenario: Delete a category and reuse its name
- **WHEN** a middle category is deleted
- **THEN** it is absent after reopening storage, HTTP 204 has no body, its name can be reused, and surviving categories keep their fields and any order gaps
- **AND** deleting it again returns HTTP 404

#### Scenario: Cascade group and game deletion
- **WHEN** an existing group or game is physically deleted through its existing API
- **THEN** categories currently belonging to that group or the game's groups are also physically removed
- **AND** unrelated groups and games retain their categories and metadata
- **AND** a transferred category follows its current group and survives deletion of its former group

### Requirement: Persist mutations safely and classify conflicts precisely
Successful category mutations SHALL be durable and atomic. Recognized category name uniqueness failures during saving SHALL return HTTP 409 with name errors and no partial changes. Unexpected library or storage failures SHALL return safe HTTP 500 ProblemDetails without internal details; unrelated failures SHALL NOT become name conflicts.

#### Scenario: Saving a transfer or metadata change fails
- **WHEN** persistence fails before a category mutation commits
- **THEN** stored membership, metadata, order, and timestamps retain their prior values
- **AND** unexpected failures return HTTP 500 without SQL, connection, or exception details

#### Scenario: Uniqueness violation while saving
- **WHEN** a category create, rename, or transfer encounters its recognized name uniqueness constraint during saving
- **THEN** the response is HTTP 409 with name errors and the mutation rolls back completely
- **AND** unrelated constraint failures are handled as safe HTTP 500

### Requirement: Document the category API and preserve existing contracts
OpenAPI SHALL describe CreateCategory, ListCategories, UpdateCategory, and DeleteCategory, their complete representations, GUID paths, metadata constraints, optional update fields, read-only count, both PATCH media types, and applicable ProblemDetails responses. Existing game and category-group contracts SHALL remain compatible.

#### Scenario: Discover all four operations
- **WHEN** a client reads OpenAPI
- **THEN** POST and PATCH document HTTP 200 category responses, GET documents a plain array, DELETE documents empty HTTP 204, and relevant 400, 404, 409, 415, 422, and 500 errors are documented
- **AND** schemas expose name and icon bounds, hexadecimal color format, positive order, and optional PATCH fields without exposing internal normalization or optional-wrapper fields
- **AND** no individual-category GET, PUT, batch, pagination, or reorder operation is advertised
