# markers Specification

## Purpose

Manage category-owned points on game maps with finite positions, descriptions, ordered image and video media, atomic updates, and consistent physical deletion.

## Requirements

### Requirement: Scope marker operations to the route game
The system SHALL expose unauthenticated GET and POST at `/games/{gameId}/markers` and PATCH and DELETE at `/{id}` for draft and published games. Every marker SHALL belong to exactly one category of that game. Valid requests targeting a missing game, missing marker, or another game's marker SHALL return HTTP 404 ProblemDetails without changes.

#### Scenario: Game status and missing scope
- **WHEN** an otherwise valid marker operation targets an existing draft or published game
- **THEN** authentication and game status do not prevent the operation
- **AND** a missing game or a missing/foreign marker returns HTTP 404 without changing data

### Requirement: Create a complete marker
POST SHALL accept required categoryId, name, and position, with optional description and media, and return HTTP 200 with id, categoryId, name, description, position, media, createdAt, and updatedAt. The server SHALL generate UUID v7 and equal UTC timestamps. Server-owned creation properties SHALL NOT control stored metadata. No mapId prerequisite SHALL be introduced.

#### Scenario: Successful creation and defaults
- **WHEN** a valid marker is created in a category of the route game without description or media
- **THEN** the response includes all eight fields, description is null, media is [], identity is UUID v7, and createdAt equals updatedAt
- **AND** the marker survives reopening storage even when the game's mapId is null

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

### Requirement: Return the complete game-wide marker list
GET SHALL return HTTP 200 with a plain array of all markers of the route game, ordered by createdAt and then id ascending, including complete descriptions and ordered media. An existing game with no markers SHALL return []. No individual-marker GET, category filter, pagination, search, spatial filter, alternate sorting, or marker order field SHALL be exposed.

#### Scenario: Complete deterministic list
- **WHEN** a game contains markers in multiple groups and categories, including equal creation timestamps
- **THEN** every marker is returned exactly once with its complete representation, ordered by createdAt and then id ascending
- **AND** another game's markers are absent and no query controls are advertised

#### Scenario: Empty and missing game
- **WHEN** an existing game has no markers
- **THEN** GET returns HTTP 200 with []
- **AND** a nonexistent game returns HTTP 404 instead

### Requirement: Validate category membership and same-game transfers
Creation SHALL require categoryId. PATCH SHALL permit changing categoryId only to a category belonging to the route game. Missing/null required categoryId SHALL return HTTP 422, malformed GUIDs HTTP 400, and syntactically valid missing or foreign categories HTTP 404. Successful transfers SHALL preserve marker identity and createdAt and commit all accompanying valid changes atomically.

#### Scenario: Transfer within one game
- **WHEN** PATCH changes categoryId to another category of the same game
- **THEN** the marker retains its ID and creation time, remains addressable at the same game route, and reflects the new category
- **AND** omitted name, description, position, and media are preserved

#### Scenario: Invalid destination or creation category
- **WHEN** an otherwise valid creation or transfer identifies a missing category, an all-zero GUID, or a category belonging to another game
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
PATCH SHALL accept optional categoryId, name, description, position, and media and return HTTP 200 with the complete marker. Root {} SHALL return HTTP 422. Unknown/read-only root fields SHALL return HTTP 400. Identity and createdAt SHALL be preserved; updatedAt SHALL change only for effective state changes. Failed updates SHALL leave the entire aggregate unchanged.

#### Scenario: Combined update and effective equality
- **WHEN** a valid patch changes any combination of mutable fields
- **THEN** all effective changes commit together and only that marker's updatedAt changes
- **AND** supplied values equal to current normalized state, including equal ordered media content, succeed without changing updatedAt or rewriting media

#### Scenario: Parent timestamps and read-only fields
- **WHEN** a marker is created, edited, transferred, or deleted
- **THEN** parent game, group, and category timestamps and metadata remain unchanged
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
DELETE SHALL physically remove the scoped marker and its media records, returning HTTP 204 without a body. Repeated deletion SHALL return HTTP 404. Deleting a category, group, or game SHALL cascade through currently owned markers and media while preserving unrelated data. Referenced files SHALL NOT be deleted.

#### Scenario: Delete and repeat
- **WHEN** an existing marker with mixed media is deleted
- **THEN** its marker and media records are absent after reopening storage and the response is empty HTTP 204
- **AND** another marker remains unchanged and repeated deletion returns HTTP 404

#### Scenario: Parent deletion after transfers
- **WHEN** a category, group, or game is deleted using its existing API, including direct game deletion
- **THEN** its currently owned markers and media are physically removed
- **AND** a marker transferred out of a deleted category survives, and unrelated game data is preserved

### Requirement: Persist marker mutations safely
Successful marker mutations SHALL be durable. Category checks, position changes, and replacement media SHALL commit together. Unexpected storage/library failures SHALL return safe HTTP 500 ProblemDetails without SQL, connection, or exception details and SHALL NOT be mislabeled as name conflicts. Failed writes SHALL NOT leave orphaned media or partial category-count changes.

#### Scenario: Failure after a write but before commit
- **WHEN** creating, updating, transferring, deleting, or replacing media fails after database commands execute but before commit
- **THEN** the prior marker state, media, and observable category counts are restored
- **AND** the response is safe HTTP 500 without internal details

### Requirement: Document marker operations and discriminated media
OpenAPI SHALL expose ListMarkers, CreateMarker, UpdateMarker, and DeleteMarker in GET/POST/PATCH/DELETE order. It SHALL describe complete DTOs, GUID paths, position semantics, validation bounds, media items as oneOf with a type discriminator mapping to exactly image/youtubeVideo/video, both PATCH media types, and applicable ProblemDetails failures. Existing API contracts SHALL remain compatible except actual marker counts.

#### Scenario: Discover the contract
- **WHEN** a client reads OpenAPI
- **THEN** GET documents a plain full-marker array, POST/PATCH document 200 DTOs, DELETE documents empty 204, and relevant 400/404/415/422/500 errors are present
- **AND** both request and response media arrays expose the three distinct schemas, required discriminator values, subtype-specific fields, image alt default, and nullable video preview
- **AND** PATCH fields and nested coordinates are optional, creation coordinates are required, and internal media IDs, optional wrappers, filters, pagination, PUT, and individual GET are absent
