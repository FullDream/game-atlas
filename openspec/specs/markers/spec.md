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

### Requirement: Expose an ordered media union without public child identities
Media SHALL be an ordered array containing image, youtubeVideo, or video objects identified by the required case-sensitive type property. Each variant SHALL expose only its own fields. Media IDs and storage order fields SHALL NOT be exposed or accepted. Missing/unknown discriminators, incompatible types, and unknown variant properties SHALL return HTTP 400; null array elements SHALL return HTTP 422.

#### Scenario: Mixed media round trip
- **WHEN** media contains an image, a YouTube reference, and a hosted video in that order
- **THEN** all three objects round-trip in the same order with their canonical type values and variant-specific data
- **AND** repeated equal attachments remain separate ordered elements without public IDs

#### Scenario: Reject ambiguous or unknown variants
- **WHEN** an element omits type, supplies an unknown type such as fileVideo, adds name/caption/internal IDs, or combines fields belonging to different variants
- **THEN** HTTP 400 is returned without changes
- **AND** null elements return HTTP 422 without partial persistence

### Requirement: Validate image attachment metadata
An image SHALL contain type=image, required url, and required positive 32-bit integer width and height. Alt SHALL default to an empty string when omitted and SHALL allow empty text but reject explicit null with HTTP 422. Missing/null/blank url or missing/null/nonpositive dimensions SHALL return HTTP 422; incompatible or out-of-range dimension representations SHALL return HTTP 400.

#### Scenario: Image metadata and default alt
- **WHEN** an image supplies a valid url and dimensions 1920 by 1080 without alt
- **THEN** the complete returned image includes alt="" and the supplied dimensions
- **AND** a supplied alt string is preserved, while explicit null alt returns HTTP 422

#### Scenario: Required image dimensions
- **WHEN** width or height is missing, null, zero, or negative
- **THEN** HTTP 422 with the affected nested media field error is returned
- **AND** fractional numbers, strings, and integer overflow return HTTP 400

### Requirement: Accept YouTube references by video identifier
A youtubeVideo attachment SHALL contain type=youtubeVideo and a required case-sensitive videoId of 11 ASCII letters, digits, hyphens, or underscores. Missing, null, blank, or invalid IDs SHALL return HTTP 422 with videoId errors. Full YouTube URLs, embed HTML, provider lookups, and remote existence checks SHALL NOT be supported.

#### Scenario: Identifier-only YouTube reference
- **WHEN** an attachment supplies `{"type":"youtubeVideo","videoId":"dQw4w9WgXcQ"}`
- **THEN** the identifier is stored and returned unchanged without contacting YouTube
- **AND** a full URL or an identifier with the wrong length or alphabet returns HTTP 422

### Requirement: Accept hosted video references and optional previews
A video attachment SHALL contain type=video and required url, with optional preview as an image-reference string. Omitted/null/blank preview SHALL normalize to null. A supplied nonblank preview SHALL be trimmed and returned. Preview dimensions and alt, HTML embeds, playback settings, and processing state SHALL NOT be introduced.

#### Scenario: Hosted video with and without preview
- **WHEN** media contains `{"type":"video","url":"/videos/chest.mp4","preview":"/images/chest.webp"}`
- **THEN** both references round-trip
- **AND** omission, null, or whitespace-only preview returns preview=null, while missing/null/blank required url returns HTTP 422

### Requirement: Validate media references without remote inspection
Image/video url and nonblank preview SHALL be trimmed opaque strings of 1 through 2048 characters. Relative paths and absolute links SHALL be accepted without extension, resource availability, or file-content checks. Overlength values SHALL return HTTP 422 with nested field errors. This feature SHALL NOT upload, fetch, transcode, or delete referenced files.

#### Scenario: Reference bounds and formats
- **WHEN** a reference uses a relative path, an absolute link, or an extensionless resource within the length limit
- **THEN** it is stored without remote access
- **AND** 2048 trimmed characters are accepted while 2049 are rejected with HTTP 422 without partial changes

### Requirement: Replace media arrays atomically through marker updates
Media omission at creation SHALL produce []; omission in PATCH SHALL preserve current media. A supplied array SHALL replace the whole list in client order, including complete replacement objects for every element. [] SHALL clear media; null SHALL return HTTP 422. An invalid element SHALL reject the entire marker mutation. Separate media update/reorder endpoints SHALL NOT be exposed.

#### Scenario: Replace and clear the list
- **WHEN** PATCH supplies a valid media array, including a different order or variant composition
- **THEN** the supplied array becomes the complete ordered list and omitted old attachments are removed from storage
- **AND** [] clears the list while omitted media leaves it unchanged

#### Scenario: Reject partial element patches
- **WHEN** a supplied replacement image contains only alt, or any replacement element lacks its required fields
- **THEN** the operation fails with the appropriate 400 discriminator/type or 422 validation response
- **AND** no scalar, position, category, or attachment change is persisted

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

### Requirement: Persist marker mutations safely
Successful marker mutations SHALL be durable. Category checks, position changes, and replacement media SHALL commit together. Unexpected storage/library failures SHALL return safe HTTP 500 ProblemDetails without SQL, connection, or exception details and SHALL NOT be mislabeled as name conflicts. Failed writes SHALL NOT leave orphaned media or partial category-count changes.

#### Scenario: Failure after a write but before commit
- **WHEN** creating, updating, transferring, deleting, or replacing media fails after database commands execute but before commit
- **THEN** the prior marker state, media, and observable category counts are restored
- **AND** the response is safe HTTP 500 without internal details

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
