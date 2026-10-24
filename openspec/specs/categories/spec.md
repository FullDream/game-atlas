# categories Specification

## Purpose

Manage categories within game-owned category groups, including recognizable icons and colors, display order, same-game transfers, and a read-only marker count.

## Requirements

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

### Requirement: Create categories with server-owned identity and timestamps
POST SHALL accept required name, iconAssetId, and color with optional order and return the complete category. iconAssetId SHALL identify a committed library image. The server SHALL generate UUID v7, route-derived categoryGroupId, and equal UTC timestamps. Additional creation metadata SHALL NOT override server values. Category creation SHALL NOT upload files.

#### Scenario: Successful creation
- **WHEN** a valid category references an existing library image in an existing group
- **THEN** its complete representation survives reopening storage and icon resolves the shared image DTO
- **AND** id is UUID v7, categoryGroupId matches the route, and timestamps are equal UTC instants

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

### Requirement: Attach library image assets as category icons
Category icons SHALL reference library images through iconAssetId rather than arbitrary URLs. Missing/null required IDs SHALL return 422, malformed GUIDs 400, missing assets 404, and non-image assets 422. JPEG, PNG, WebP, AVIF, and accepted SVG SHALL be permitted. Attachments SHALL participate in the library deletion guard; changing or deleting a category SHALL NOT delete its asset.

#### Scenario: Icon references
- **WHEN** a category attaches a valid raster or SVG image asset from any folder or game usage
- **THEN** it resolves shared image metadata without copying bytes or requiring a URL
- **AND** video assets and folder IDs cannot be used as icons

#### Scenario: Icon bounds
- **WHEN** iconAssetId is missing/null, malformed, absent from the library, or identifies a video
- **THEN** the corresponding 422, 400, 404, or 422 response preserves all category data
- **AND** legacy icon strings are no longer accepted as category mutation input

#### Scenario: Reuse an icon and preserve it after detachment
- **WHEN** two categories share one image and one is deleted or changes its icon
- **THEN** the other category retains its reference and the asset remains in the library
- **AND** explicit asset deletion returns 409 until every category and other consumer detaches it

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
GET SHALL retain the complete ordered category array and its nine fields: id, categoryGroupId, name, icon, color, order, markersCount, createdAt, and updatedAt. icon SHALL be the shared image-asset representation rather than a string. Ordering SHALL remain order/createdAt/id ascending. No individual GET, pagination, search, alternate sorting, batch update, or reorder endpoint SHALL be introduced.

#### Scenario: Ordered complete collection
- **WHEN** an existing group contains categories, including duplicate orders and equal creation timestamps
- **THEN** every category is returned exactly once with all nine fields, ordered by order, then createdAt, then id ascending
- **AND** categories of other groups are absent

#### Scenario: Empty group
- **WHEN** an existing group has no categories
- **THEN** its list response is HTTP 200 with `[]`
- **AND** a missing or foreign group returns HTTP 404 instead

#### Scenario: Resolve current shared icon metadata
- **WHEN** an icon asset is renamed, moved, or given different alt text through the library
- **THEN** subsequent category responses include that current shared image metadata without changing category timestamps
- **AND** collection queries load icons without one query per category

### Requirement: Update category state atomically through merge patch
PATCH SHALL accept optional name, iconAssetId, color, order, and categoryGroupId and return the nine-field category with resolved icon. Omission SHALL preserve fields; null required mutable values and empty objects SHALL return 422. Asset checks, parent transfers, metadata, and attachment updates SHALL commit atomically. Identity/createdAt and parent timestamps SHALL remain unchanged; only effective changes update the category.

#### Scenario: Preserve parent state and no-ops
- **WHEN** a valid patch changes category metadata or transfers within its map
- **THEN** only effective category state and its own updatedAt change
- **AND** normalized no-ops preserve its timestamp without affecting either parent map or game

#### Scenario: Single or combined changes
- **WHEN** a client supplies one or more valid mutable values
- **THEN** they are applied together, with omitted values preserved and only the target category's updatedAt changed
- **AND** game, map, group, and other category metadata and timestamps remain unchanged

#### Scenario: No effective change
- **WHEN** supplied values equal current state after name trimming/color normalization, including the same iconAssetId and group
- **THEN** HTTP 200 returns the current representation without changing category updatedAt or rewriting its attachment

#### Scenario: Atomic rejection
- **WHEN** a patch is empty or contains an invalid value alongside otherwise valid changes
- **THEN** the response is the relevant validation failure and no field or membership changes

#### Scenario: Reject a failed icon replacement atomically
- **WHEN** a valid name or group change accompanies a missing or incompatible iconAssetId
- **THEN** the complete patch fails and prior ownership, icon reference, metadata, and timestamps remain unchanged

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
Every category representation SHALL include integer markersCount equal to the number of currently persisted markers belonging to that category. Clients SHALL NOT control the count. Counts SHALL be computed from marker records without a persisted category counter. Categories without markers SHALL return 0. The existing category representation shape SHALL remain unchanged.

#### Scenario: Count across operations
- **WHEN** a category is created, listed, updated, or transferred
- **THEN** its representation includes markersCount equal to its current number of markers, including 0 when none exist
- **AND** PATCH attempts to change that field return HTTP 400

#### Scenario: Create delete and transfer markers
- **WHEN** a marker is created or deleted
- **THEN** the next category representation reflects the resulting count
- **AND** transferring a marker within its game decreases the source category's count and increases the destination's count, while same-category no-ops do not change either count

#### Scenario: Retain counts on category changes and failures
- **WHEN** a populated category is renamed, reordered, or transferred to another group of the same game
- **THEN** its response retains the actual marker count and its marker ownership remains unchanged
- **AND** a failed marker mutation leaves counts unchanged without writing category metadata or timestamps

#### Scenario: Preserve unrelated counts during cascade deletion
- **WHEN** a category, group, or game is deleted with its markers
- **THEN** the related marker rows are physically removed and surviving categories report their actual remaining counts
- **AND** unrelated categories and their timestamps remain unchanged

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
OpenAPI SHALL retain the four map-scoped category operations, GUID parents, counts, ordering, colors, and PATCH errors. Mutations SHALL expose required/optional iconAssetId as appropriate, and the nine-field response SHALL expose icon as the shared image schema. Arbitrary icon URLs and old icon mutation properties SHALL NOT be advertised. No redundant mapId/gameId response fields SHALL be added.

#### Scenario: Discover map-scoped categories
- **WHEN** a client reads OpenAPI
- **THEN** routes require gameId/mapId/groupId and categoryGroupId remains category ownership
- **AND** iconAssetId selects a shared library image while markersCount stays read-only

#### Scenario: Discover all four operations
- **WHEN** a client reads OpenAPI
- **THEN** POST/PATCH document 200 category responses, GET a plain array, DELETE empty 204, and relevant ProblemDetails
- **AND** name/color/order constraints, GUID iconAssetId, shared image icon metadata, and optional PATCH presence are documented
- **AND** individual GET, PUT, batch, pagination, and reorder remain absent
