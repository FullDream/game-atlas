# markers Specification

## Purpose

Manage category-owned points on game maps with finite positions, descriptions, ordered image and video media, atomic updates, and consistent physical deletion.

## Requirements

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
POST SHALL accept required categoryId, name, and position with optional description and an ordered media array of library asset IDs. Omitted media SHALL default to []. Success SHALL return the existing eight root fields with media resolved as shared image/video assets. Server-generated identity/timestamps, route ownership, and zero-style map support SHALL remain unchanged.

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

### Requirement: Validate marker names while permitting duplicates
Names SHALL be trimmed, preserve display spelling, and contain 1 through 200 characters after trimming. Missing required creation input, null, blank, or overlength names SHALL return HTTP 422 with name errors. Duplicate names SHALL be allowed within the same category and game, including equal normalized spellings.

#### Scenario: Name validation and duplicate names
- **WHEN** valid marker names include ` Chest `, `Chest`, or `Сундук` in one category
- **THEN** creation succeeds and display names are trimmed without enforcing uniqueness
- **AND** exactly 200 trimmed characters are accepted while missing/null/blank names or 201 characters return HTTP 422 without changes

### Requirement: Normalize optional plain-text descriptions
Description SHALL be optional plain text of at most 5000 characters after trimming surrounding whitespace. Omission at creation SHALL produce null; null, empty, and whitespace-only values SHALL normalize to null. A supplied nonblank description SHALL preserve its trimmed text without interpreting it as HTML. Overlength values SHALL return HTTP 422 with description errors.

#### Scenario: Set and clear a description
- **WHEN** creation or update supplies `  Behind the waterfall  `
- **THEN** the stored and returned description is `Behind the waterfall`
- **AND** PATCH null, empty, or whitespace-only description clears it, while omission preserves it

#### Scenario: Description boundary
- **WHEN** a supplied description has 5000 characters after trimming
- **THEN** it is accepted
- **AND** 5001 trimmed characters return HTTP 422 without persisting any supplied changes

### Requirement: Require finite two-dimensional positions
Position SHALL be an object with required finite double-precision x and y coordinates at creation. Negative, zero, and fractional coordinates SHALL be allowed without geographic or map-bound ranges. Missing/null position or required coordinates SHALL return HTTP 422. Incompatible JSON shapes and field types SHALL return HTTP 400. Non-finite numeric state SHALL NOT be persisted.

#### Scenario: Coordinates and required creation values
- **WHEN** a marker is created with position `{"x":123.45,"y":-67.89}` or zero coordinates
- **THEN** both coordinates round-trip in a nested position object
- **AND** omitted/null position or either omitted/null coordinate returns HTTP 422 without creating a marker

#### Scenario: Invalid numeric representations
- **WHEN** position is an array/scalar or a coordinate has a string, boolean, or object type, including string representations of NaN or infinity
- **THEN** the response is HTTP 400
- **AND** non-finite numbers reaching state validation are rejected with HTTP 422 rather than being persisted

### Requirement: Merge supplied position members recursively
Marker PATCH SHALL merge position members recursively according to RFC 7396. Supplied x or y SHALL replace only that coordinate; omitted members SHALL retain their current values. The complete resulting pair SHALL be validated and saved atomically. Null position or null coordinates SHALL return HTTP 422; unknown position members SHALL return HTTP 400.

#### Scenario: Update one coordinate
- **WHEN** position is `{"x":123.45,"y":-67.89}` and PATCH supplies `{"position":{"x":200}}`
- **THEN** the resulting position is `{"x":200,"y":-67.89}`
- **AND** supplying only y analogously preserves x

#### Scenario: Empty nested object and invalid members
- **WHEN** PATCH supplies `{"position":{}}`
- **THEN** the position is unchanged and an otherwise unchanged patch succeeds without changing updatedAt
- **AND** null position, null x/y, or an unknown position member fails without applying any accompanying name, category, or media change

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

### Requirement: Attach ordered library assets without independent marker media models
Marker media SHALL reference existing library images/videos through ordered UUID arrays. Duplicate IDs and null elements SHALL return 422, malformed GUIDs or legacy media objects 400, missing assets 404, and folder/incompatible references 422. Responses SHALL reuse shared library image/video representations including public asset IDs and required type. Separate per-marker media identities/metadata SHALL NOT be exposed.

#### Scenario: Mixed media round trip
- **WHEN** media contains an image asset ID followed by a video asset ID
- **THEN** both resolve in exactly that order as shared library representations with id, type, URL, and metadata
- **AND** one asset may be reused by other markers but cannot occur twice within the same marker

#### Scenario: Reject ambiguous or unknown variants
- **WHEN** a media element is an old image/video/YouTube object, a URL, or a malformed GUID
- **THEN** HTTP 400 rejects the complete request
- **AND** null elements or duplicate IDs return 422 without partial persistence

#### Scenario: Shared metadata is resolved rather than copied
- **WHEN** attached image alt/name or a video's preview changes in the library
- **THEN** subsequent marker responses expose current shared metadata without updating marker timestamps or attachment IDs/order

### Requirement: Replace media arrays atomically through marker updates
Omitted creation media SHALL produce []; omitted PATCH media SHALL preserve attachments. A supplied asset-ID array SHALL replace the whole ordered attachment list. [] SHALL clear attachments; null SHALL return 422. Every ID SHALL be validated before any marker state is saved. Detachment SHALL NOT remove library assets. No separate marker-media update/reorder endpoint SHALL exist.

#### Scenario: Replace and clear the list
- **WHEN** PATCH supplies a valid ordered array of distinct asset IDs
- **THEN** it becomes the complete attachment list and omitted former attachments are removed
- **AND** [] clears attachments, omitted media preserves them, and all underlying assets remain in the library

#### Scenario: Reject partial element patches
- **WHEN** an array contains a partial media object or an invalid, duplicated, missing, or incompatible asset reference
- **THEN** the operation fails with the applicable 400/404/422 response
- **AND** no scalar, position, category, or attachment change is persisted

### Requirement: Apply atomic marker merge patches and preserve no-ops
PATCH SHALL retain optional categoryId, name, description, position, and media, with media now an asset-ID array. Attachment/reference checks and scalar/position changes SHALL commit atomically. Empty objects SHALL return 422 and unknown/read-only fields 400. Identity/createdAt and parent timestamps SHALL remain unchanged. Equal ordered IDs SHALL be a media no-op; effective changes SHALL update only the marker.

#### Scenario: Preserve parents and recursive position behavior
- **WHEN** a marker is created, changed, transferred, or deleted
- **THEN** parent metadata remains unchanged
- **AND** position merging, ID-array replacement, read-only-field rejection, and resource-level atomicity remain in force

#### Scenario: Combined update and effective equality
- **WHEN** valid changes combine scalar fields, recursive position members, same-map category transfer, and ordered asset IDs
- **THEN** all effective changes commit together and only marker updatedAt changes
- **AND** equal normalized fields and equal ordered asset IDs preserve marker timestamps and attachment rows

#### Scenario: Parent timestamps and read-only fields
- **WHEN** a marker is created, edited, transferred, or deleted
- **THEN** parent game, map, group, and category timestamps and metadata remain unchanged
- **AND** PATCH supplying id, gameId, createdAt, updatedAt, icon, color, or another unknown root field returns HTTP 400

### Requirement: Accept the shared PATCH media types and shapes
Updates SHALL accept application/merge-patch+json and application/json with identical RFC 7396 semantics, including charset parameters. Other PATCH media types SHALL return HTTP 415. Malformed JSON, non-object bodies, incompatible types, and unknown nested fields SHALL return HTTP 400. PUT SHALL NOT be exposed. Expected failures SHALL use ProblemDetails.

#### Scenario: Equivalent media types and unsupported input
- **WHEN** valid equivalent patches use either supported media type with or without charset=utf-8
- **THEN** they produce equivalent state changes
- **AND** text/plain or application/json-patch+json returns HTTP 415 without changes

#### Scenario: Invalid JSON shape
- **WHEN** a PATCH body is malformed, null, an array, a scalar, or contains incompatible/unknown nested fields
- **THEN** it returns HTTP 400 ProblemDetails without storage changes

### Requirement: Physically delete markers and cascade current ownership
DELETE SHALL physically remove the scoped marker and its attachment rows and return empty 204. Repeated deletion SHALL return 404. Category/group/map/game deletion SHALL cascade through current marker ownership without deleting shared assets or their files. Other markers' attachments SHALL remain unchanged. Removing a style SHALL NOT delete marker attachments.

#### Scenario: Map deletion and style deletion have distinct ownership
- **WHEN** a map is deleted
- **THEN** its marker/attachment rows disappear while sibling maps and every shared asset remain intact
- **AND** style deletion alone preserves every marker, attachment, position, and category count

#### Scenario: Delete and repeat
- **WHEN** a marker referencing shared image/video assets is deleted
- **THEN** its marker and attachment rows are absent after reopening storage and the response is empty 204
- **AND** referenced assets and another marker's references remain; repeated deletion returns 404

#### Scenario: Parent deletion after transfers
- **WHEN** a category, group, map, or game is deleted through its existing API
- **THEN** current marker and attachment rows cascade while assets remain in the library
- **AND** moved markers survive former-parent deletion, and detached assets are not automatically expired

### Requirement: Persist marker mutations safely
Marker state, category checks, and ordered asset attachments SHALL be durable and atomic. Concurrent asset deletion SHALL NOT create dangling references. Unexpected storage/library failures SHALL return safe 500 without being classified as name conflicts. Failed writes SHALL restore prior attachments and category counts, leaving every shared asset unchanged.

#### Scenario: Failure after a write but before commit
- **WHEN** creation, transfer, deletion, or attachment replacement fails after commands execute but before commit
- **THEN** prior marker fields, attachment order, and observable counts are restored
- **AND** assets/bytes remain intact and the safe 500 response reveals no internal details

### Requirement: Document marker operations and discriminated media
OpenAPI SHALL retain four map-scoped marker operations in GET/POST/PATCH/DELETE order and eight root response fields. Creation/PATCH media SHALL be UUID arrays; response media SHALL reuse the library image/video union. YouTube, arbitrary URLs, per-marker media metadata, and old media-object input schemas SHALL be absent. Existing position, category, counts, PATCH, and safe-error contracts SHALL remain.

#### Scenario: Discover scoped operations without media regression
- **WHEN** a client reads OpenAPI
- **THEN** existing marker operation IDs/routes remain, requests accept ordered UUID media arrays, and responses use shared image/video schemas
- **AND** lists stay unpaginated/unfiltered while YouTube, PUT, and individual-marker GET are absent

#### Scenario: Discover the contract
- **WHEN** a client reads OpenAPI
- **THEN** GET returns complete marker arrays, POST/PATCH return 200 DTOs, DELETE returns empty 204, and relevant 400/404/415/422/500 responses are documented
- **AND** responses expose image/video discriminator mappings with shared alt/preview metadata, while input uses IDs without type discriminators
- **AND** optional patch fields, recursive position semantics, creation requirements, and duplicate-ID validation are discoverable
