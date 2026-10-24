# Markers Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Scope marker operations to the route game`
- TO: `### Requirement: Scope marker operations to the route map`
- FROM: `### Requirement: Return the complete game-wide marker list`
- TO: `### Requirement: Return the complete map-wide marker list`
- FROM: `### Requirement: Validate category membership and same-game transfers`
- TO: `### Requirement: Validate category membership and same-map transfers`

## MODIFIED Requirements

### Requirement: Scope marker operations to the route map
Unauthenticated GET/POST SHALL use /games/{gameId}/maps/{mapId}/markers and PATCH/DELETE its /{id} resource for draft or published games. Every marker SHALL belong to a category whose group belongs to that map. Missing/foreign game/map/marker scope SHALL return HTTP 404 without changes, including sibling maps of the same game. Legacy game-wide marker routes SHALL NOT remain active.

#### Scenario: No image prerequisite and strict map scope
- **WHEN** a valid marker operation targets a map without styles
- **THEN** it is permitted normally
- **AND** supplying another map's marker under this route returns 404 even within the same game

#### Scenario: Game status and missing scope
- **WHEN** an existing map belongs to a draft or published game
- **THEN** marker operations do not require authentication or styles
- **AND** missing or foreign game/map/marker scope returns 404 without changes

### Requirement: Create a complete marker
POST SHALL accept required categoryId, name, and position with optional description/media and return HTTP 200 with the existing eight marker fields. The server SHALL assign UUID v7 and equal UTC timestamps. The route map SHALL exist, but no uploaded/ready style SHALL be required. Server-owned input SHALL NOT override identity, ownership, or timestamps.

#### Scenario: Create on a blank coordinate space
- **WHEN** a valid marker is created in a category of a map with styles=[]
- **THEN** it persists with unchanged position/media semantics, description=null and media=[] when omitted
- **AND** adding or deleting styles later does not alter the marker

#### Scenario: Successful creation and defaults
- **WHEN** a valid marker is created in a category of the route map without description/media
- **THEN** all eight fields return, description=null, media=[], UUID v7, and equal UTC timestamps
- **AND** the marker survives reopening storage even with styles=[]

#### Scenario: Server-owned creation fields
- **WHEN** otherwise valid creation input also supplies id, gameId, createdAt, or updatedAt
- **THEN** those values do not override the server-generated identity, ownership, or timestamps

### Requirement: Return the complete map-wide marker list
GET SHALL return a plain array of every marker of the route map, including complete descriptions and ordered media, sorted by createdAt then id ascending. Existing maps without markers SHALL return []; missing/foreign maps SHALL return 404. Individual GET, filters, pagination, search, spatial controls, alternate sorting, and marker order SHALL NOT be introduced.

#### Scenario: Isolate sibling maps
- **WHEN** multiple maps of one game contain markers across several categories
- **THEN** each list contains only that map's complete markers in deterministic order
- **AND** an empty sibling map returns [] without depending on styles

#### Scenario: Complete deterministic list
- **WHEN** a map contains markers in multiple groups and categories, including equal creation timestamps
- **THEN** every marker is returned exactly once with its complete representation, ordered by createdAt and then id ascending
- **AND** another map's markers are absent and no query controls are advertised

#### Scenario: Empty and missing game
- **WHEN** an existing map has no markers
- **THEN** GET returns HTTP 200 with []
- **AND** a missing game or map returns HTTP 404 instead

### Requirement: Validate category membership and same-map transfers
Creation SHALL require categoryId. PATCH SHALL allow categoryId changes only to categories of the route map. Null/missing required creation categoryId SHALL return 422; malformed GUIDs 400; valid missing/foreign destinations 404. Same-map transfers SHALL preserve marker identity/createdAt and commit accompanying changes atomically. Cross-map transfer SHALL NOT be supported.

#### Scenario: Transfer and category counts within one map
- **WHEN** a marker moves to another category of its map
- **THEN** it retains its ID, coordinates, omitted metadata/media, and creation time, and source/destination counts reflect the move

#### Scenario: Refuse another map's category
- **WHEN** creation or PATCH identifies a category of a sibling map, even in the same game
- **THEN** HTTP 404 preserves marker state, source/destination counts, and all accompanying values

#### Scenario: Transfer within one game
- **WHEN** PATCH changes categoryId to another category of the same map
- **THEN** the marker retains its ID and creation time, remains addressable at the same map route, and reflects the new category
- **AND** omitted name, description, position, and media are preserved

#### Scenario: Invalid destination or creation category
- **WHEN** an otherwise valid creation or transfer identifies a missing category, an all-zero GUID, or a category belonging to another map
- **THEN** HTTP 404 is returned without partial writes
- **AND** null or missing required creation categoryId returns HTTP 422, and malformed GUID input returns HTTP 400

### Requirement: Apply atomic marker merge patches and preserve no-ops
PATCH SHALL accept optional categoryId, name, description, position, and media and return the complete marker. Empty objects SHALL return 422; unknown/read-only fields SHALL return 400. Identity/createdAt SHALL remain unchanged. Only effective marker state SHALL update its updatedAt; no-ops SHALL preserve it. Parent map/game/group/category metadata and timestamps SHALL remain unchanged. Failed updates SHALL be atomic.

#### Scenario: Preserve parents and recursive position behavior
- **WHEN** a marker is created, changed, transferred, or deleted
- **THEN** parent metadata remains unchanged
- **AND** recursive position merge, full-array media replacement, normalized equality, and read-only mapId/gameId rejection retain their existing behavior

#### Scenario: Combined update and effective equality
- **WHEN** a valid patch changes any combination of mutable fields
- **THEN** all effective changes commit together and only that marker's updatedAt changes
- **AND** supplied values equal to current normalized state, including equal ordered media content, succeed without changing updatedAt or rewriting media

#### Scenario: Parent timestamps and read-only fields
- **WHEN** a marker is created, edited, transferred, or deleted
- **THEN** parent game, map, group, and category timestamps and metadata remain unchanged
- **AND** PATCH supplying id, gameId, createdAt, updatedAt, icon, color, or another unknown root field returns HTTP 400

### Requirement: Physically delete markers and cascade current ownership
DELETE SHALL physically remove the scoped marker/media records and return empty HTTP 204. Repeated deletion SHALL return 404. Category/group/map/game deletion SHALL cascade through currently owned markers/media while preserving unrelated data. Referenced files SHALL NOT be deleted. Style deletion, including the last style, SHALL NOT delete markers or their media records.

#### Scenario: Map deletion and style deletion have distinct ownership
- **WHEN** the map is deleted
- **THEN** its current markers/media records disappear while sibling maps and external media references remain intact
- **AND** deleting only a style leaves every marker, media record, coordinate, and category count unchanged

#### Scenario: Delete and repeat
- **WHEN** an existing marker with mixed media is deleted
- **THEN** its marker and media records are absent after reopening storage and the response is empty HTTP 204
- **AND** another marker remains unchanged and repeated deletion returns HTTP 404

#### Scenario: Parent deletion after transfers
- **WHEN** a category, group, map, or game is deleted using its existing API, including direct game deletion
- **THEN** its currently owned markers and media are physically removed
- **AND** a marker transferred out of a deleted category survives, and unrelated game data is preserved

### Requirement: Document marker operations and discriminated media
OpenAPI SHALL document the four existing marker operation IDs on game/map-scoped routes in GET/POST/PATCH/DELETE order. It SHALL preserve complete eight-field DTOs, finite/nested position rules, required-discriminator media anyOf unions, shared PATCH semantics, validation, and applicable ProblemDetails. Legacy game-wide marker routes SHALL be absent; no redundant mapId/gameId DTO field SHALL be added.

#### Scenario: Discover scoped operations without media regression
- **WHEN** a client reads OpenAPI
- **THEN** ListMarkers/CreateMarker/UpdateMarker/DeleteMarker require gameId/mapId paths and retain image/youtubeVideo/video request/response schemas
- **AND** lists remain unpaginated and unfiltered, and PUT and individual-marker GET remain absent

#### Scenario: Discover the contract
- **WHEN** a client reads OpenAPI
- **THEN** GET documents a plain full-marker array, POST/PATCH document 200 DTOs, DELETE documents empty 204, and relevant 400/404/415/422/500 errors are present
- **AND** both request and response media arrays expose the three distinct schemas, required discriminator values, subtype-specific fields, image alt default, and nullable video preview
- **AND** PATCH fields and nested coordinates are optional, creation coordinates are required, and internal media IDs, optional wrappers, filters, pagination, PUT, and individual GET are absent
