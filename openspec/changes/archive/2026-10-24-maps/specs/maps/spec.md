# Maps Spec Delta

## Purpose

Manage independent game-owned coordinate spaces with map-owned content, optional aligned styles, durable originals, recoverable tile generation, and per-map progress.

## ADDED Requirements

### Requirement: Refuse unplanned legacy map data loss
The schema-only MapStyle upgrade SHALL refuse a database containing legacy maps, image versions, admission owners, or cleanup intents before dropping tables. Existing data and files SHALL remain unchanged on refusal. Transitioning a nonempty legacy store SHALL require a separately agreed data-and-file migration or explicit authorization to remove test maps. The already applied Maps migration SHALL NOT be rewritten or automatically applied at startup.

#### Scenario: Preserve an existing legacy upload
- **WHEN** the schema-only upgrade encounters a previously uploaded map
- **THEN** it fails before schema changes and keeps the map, image metadata, original files, and parent resources intact
- **AND** operators cannot silently upgrade by discarding image history or resetting processing state

#### Scenario: Initialize a fresh store
- **WHEN** an operator explicitly applies migrations to a fresh database or an agreed prepared empty legacy map store
- **THEN** the schema creates MapStyles with one original and processing state per style
- **AND** no version tables are used by the current application model

### Requirement: Scope multiple maps and their styles to a game
A game SHALL own zero or more independent maps. Map operations SHALL use /games/{gameId}/maps and /games/{gameId}/maps/{mapId}, without authentication for draft or published games. Styles SHALL belong to exactly one map. Valid requests for missing parents or resources outside the route game/map SHALL return HTTP 404 without changes. The former singular map routes SHALL NOT remain active aliases.

#### Scenario: Independent maps in one game
- **WHEN** a game already has a map and a valid request creates another with a different normalized name
- **THEN** both maps exist with independent identities, dimensions, styles, and content

#### Scenario: Reject foreign resources even within one game
- **WHEN** a route identifies a map of another game or a style of another map, including a sibling map of the same game
- **THEN** HTTP 404 preserves all resources and files

### Requirement: Create a named coordinate space without an image
POST /games/{gameId}/maps SHALL accept application/json with required name, width, and height and return HTTP 200 with a persisted map. Dimensions SHALL be integers from 1 through 16384. The server SHALL assign UUID v7 and UTC timestamps. A new map SHALL have styles=[] and defaultStyleId=null. Creation SHALL NOT require or retain a file, enqueue processing, or create upload work.

#### Scenario: Create a blank map
- **WHEN** a valid JSON body contains name=World, width=1025, and height=513
- **THEN** HTTP 200 returns that named map with centered geometry, no styles, and no default style
- **AND** the map survives restart without any original file, tile generation, or viewer subscription

#### Scenario: Reject invalid creation
- **WHEN** name or either dimension is missing/null, name is blank/overlength, or a dimension is outside 1..16384
- **THEN** HTTP 422 returns field errors without creating a map or changing the game
- **AND** malformed JSON, incompatible types, fractional/out-of-range integer representations return 400; multipart or other unsupported creation media return 415

#### Scenario: Create content before the first style
- **WHEN** groups, categories, and markers are added to a map with no styles
- **THEN** they are persisted normally using the map's existing coordinate space without requiring an image

### Requirement: Add one style through a flat typed multipart contract
POST /games/{gameId}/maps/{mapId}/styles SHALL accept exactly one original in multipart field file and plain text fields name and backgroundColor. It SHALL append one queued style and return HTTP 200 with complete map metadata after durable admission. It SHALL NOT accept a metadata JSON array or dynamic file keys. Duplicate, unknown, or missing file parts SHALL return 400; invalid or missing name/color 422. A rejected request SHALL leave existing styles unchanged.

#### Scenario: Append one style independently of wire order
- **WHEN** an existing map receives valid name, backgroundColor, and file parts in any order
- **THEN** exactly one style is appended, its unchanged original and queued state are durable before HTTP 200, and existing styles remain unchanged
- **AND** a typed request schema documents three required properties, including one binary file

#### Scenario: Reject a conflicting name
- **WHEN** the supplied name conflicts with an existing style
- **THEN** HTTP 409 rejects the addition without changing original bytes, style metadata, order, or defaults

#### Scenario: Reject batch or ambiguous input
- **WHEN** the form contains metadata JSON, multiple files, repeated name/color, or unknown parts
- **THEN** HTTP 400 rejects the request without adding a style
- **AND** validation errors for name/color use flat field names

### Requirement: Enforce configurable single-file upload limits
Default admission limits SHALL be 209715200 bytes per file and width and height each from 1 through 16384 pixels. Limits SHALL be configurable. Byte-limit violations SHALL return HTTP 413. Dimension violations SHALL return HTTP 422. Limits SHALL NOT trigger silent image resizing.

#### Scenario: Accept configured boundaries
- **WHEN** a file is exactly within configured byte and dimension limits
- **THEN** it passes those size checks without downscaling the original

#### Scenario: Reject oversize and dimension violations
- **WHEN** a file exceeds its byte limit
- **THEN** HTTP 413 rejects the entire upload
- **AND** an image exceeding either dimension bound returns HTTP 422 instead

#### Scenario: Apply changed configuration
- **WHEN** an operator changes supported admission limits and restarts the application
- **THEN** new uploads use those configured limits
- **AND** existing map dimensions, published images, and tile geometry are not rewritten

### Requirement: Accept supported image originals without modifying them
Uploads SHALL accept still PNG and JPEG image content, identified from the file rather than trusting its extension or declared media type. Unsupported content SHALL return HTTP 422. Original bytes SHALL be preserved unchanged. Admission SHALL check readable image metadata and dimensions; failures only discoverable during background decoding SHALL follow style processing failure rules.

#### Scenario: Preserve an accepted original
- **WHEN** a supported image is admitted
- **THEN** its retained original is byte-for-byte equal to the uploaded file
- **AND** a misleading filename cannot bypass content validation or choose a storage path

#### Scenario: Reject unsupported images
- **WHEN** a binary part contains SVG, an animated image, or unsupported or unreadable image headers
- **THEN** HTTP 422 rejects the complete upload without visible state changes

### Requirement: Keep all styles aligned in the map's immutable geometry
JSON map creation SHALL establish width, height, and centered coordinates before any style exists. Every uploaded style SHALL exactly match those dimensions. Mismatches SHALL return HTTP 422 before changes. Adding, retrying, or removing styles SHALL NOT resize the map or reposition markers. No resizing endpoint SHALL be exposed.

#### Scenario: Validate the first and later styles
- **WHEN** any uploaded style, including the first one, differs from the map's width or height
- **THEN** HTTP 422 leaves the map, existing styles, and markers unchanged
- **AND** a matching original can be admitted without detecting semantic image alignment

#### Scenario: Keep geometry after removing all images
- **WHEN** the last style is deleted and a later matching style is uploaded
- **THEN** map identity, dimensions, coordinates, and existing content remain valid throughout

### Requirement: Validate unique user-defined style names
Styles SHALL support user-defined names rather than a fixed enum of styles. Names SHALL be trimmed, preserve display spelling, contain 1 through 200 characters, and be unique within their map after invariant case normalization. Invalid names SHALL return HTTP 422; conflicting names SHALL return HTTP 409, including recognized save-time uniqueness conflicts.

#### Scenario: Normalize and distinguish names
- **WHEN** a style is named '  Atlas  '
- **THEN** it is returned as Atlas and an attempted ' atlas ' in the same map returns HTTP 409
- **AND** the same names are allowed on another game's map

#### Scenario: Validate uploaded and renamed names
- **WHEN** a new or patched name is null, blank, or longer than 200 trimmed characters
- **THEN** the operation returns HTTP 422 without partial state changes
- **AND** an added style conflicting with an existing normalized name returns HTTP 409

### Requirement: Keep background color as viewer metadata
Each style SHALL require backgroundColor in #RGB or #RRGGBB notation with ASCII hexadecimal digits. Short notation SHALL expand and responses SHALL use uppercase #RRGGBB. Invalid or null values SHALL return HTTP 422. The color SHALL apply only to the viewer background; changing it SHALL NOT recolor originals, tiles, or transparency or enqueue image processing.

#### Scenario: Normalize the viewer background
- **WHEN** a style supplies #abc or #aabbcc
- **THEN** its response contains #AABBCC

#### Scenario: Update color without regenerating tiles
- **WHEN** a valid PATCH changes only backgroundColor
- **THEN** the new metadata is returned and the style's image, tile URLs, processing state, and original bytes remain unchanged

### Requirement: Return complete map metadata
GET /games/{gameId}/maps/{mapId} SHALL return HTTP 200 with id, gameId, name, width, height, extent, tileOrigin, tileSize, minZoom, maxZoom, resolutions, defaultStyleId, styles, createdAt, and updatedAt. Styles SHALL be in creation order and may be empty. IDs SHALL use UUID v7 and timestamps UTC. Filesystem paths and internal normalized names SHALL NOT be exposed.

#### Scenario: Read a blank or styled map
- **WHEN** a client reads an existing map with no styles or with processing/ready styles
- **THEN** the same fifteen-field representation returns its complete geometry and ordered styles
- **AND** defaultStyleId is null whenever no surviving style is ready

### Requirement: Expose processing state directly on each style
Every style representation SHALL include id, name, backgroundColor, status, progress, error, tilesUrlTemplate, createdAt, and updatedAt. Status SHALL be queued, processing, ready, or failed as a string; progress SHALL be an integer from 0 through 100. There SHALL be no sourceVersionId or activeVersionId fields. The tile template SHALL be null until successful publication and SHALL identify that style's immutable image.

#### Scenario: Report queued and ready styles
- **WHEN** an accepted style has not completed processing
- **THEN** its status and progress describe its own processing attempt and tilesUrlTemplate is null
- **AND** after publication it becomes ready with progress 100 and a style-addressed tile template

#### Scenario: Report processing failure safely
- **WHEN** a style's processing fails
- **THEN** tilesUrlTemplate remains null and status is failed
- **AND** error exposes only a safe code and message, never paths, stack traces, or library diagnostics

### Requirement: Use centered original-pixel coordinates
Map coordinates SHALL use the original image center as (0,0), X increasing right, and Y increasing up. Extent SHALL be [-width/2,-height/2,width/2,height/2] and tileOrigin SHALL be [-width/2,height/2]. Coordinates SHALL remain independent of zoom and style selection. Existing marker finite-coordinate rules SHALL remain unchanged, including acceptance outside the map extent.

#### Scenario: Place the same marker on all styles
- **WHEN** a 10000 by 8000 map contains a marker at (0,0)
- **THEN** it lies at the center of every aligned style and the extent is [-5000,-4000,5000,4000]

#### Scenario: Preserve fractional geometry and existing markers
- **WHEN** a map has odd dimensions or a marker has negative or fractional coordinates
- **THEN** half-pixel boundaries and existing marker coordinates are preserved without rounding or migration
- **AND** no map-bound validation is added to marker creation or PATCH

### Requirement: Generate a transparent power-of-two tile pyramid
Each ready style SHALL provide lossless WebP tiles of 512 by 512 pixels without overlap. Zoom 0 SHALL fit the image within one tile and maxZoom SHALL represent original-pixel resolution. Adjacent zoom resolutions SHALL differ by a factor of two. Partial edge tiles SHALL be padded transparently; viewer backgroundColor SHALL NOT be baked into them.

#### Scenario: Render original and reduced levels
- **WHEN** a map is larger than one tile
- **THEN** the tile pyramid exposes all levels from minZoom 0 through maxZoom with matching resolutions
- **AND** the last resolution is one map unit per source pixel

#### Scenario: Keep edge and source transparency
- **WHEN** a non-power-of-two image or transparent PNG is tiled
- **THEN** edge padding and source alpha remain transparent and tile dimensions remain 512 by 512
- **AND** opaque source pixels are not changed by lossy encoding

#### Scenario: Handle a small map
- **WHEN** both image dimensions are at most 512
- **THEN** minZoom and maxZoom are 0 and the sole level uses resolution 1 without upscaling the original

### Requirement: Deliver immutable style-addressed tiles
The system SHALL serve complete WebP tiles statically at /map-tiles/{mapId}/{styleId}/{z}/{x}/{y}.webp without Mediator or database access. IDs SHALL use UUID N format. GET/HEAD and cache revalidation SHALL be supported. Missing files or malformed paths SHALL return HTTP 404. Retained files MAY remain available after metadata changes or deletion until cleanup; caches MAY retain them longer.

#### Scenario: Locate and cache a published tile
- **WHEN** a valid tile URL is requested for a complete retained style
- **THEN** the correct WebP bytes and a cache validator are returned
- **AND** immutable style URLs address columns increasing right and rows down from tileOrigin
- **AND** revalidating unchanged content can return HTTP 304 without changing processing state or querying the database
- **AND** HEAD returns the same headers without a response body

#### Scenario: Keep original and incomplete files private
- **WHEN** a client requests an original, staging file, working tile, manifest, missing style, or nonexistent tile coordinate
- **THEN** HTTP 404 is returned and no working directory or original becomes accessible
- **AND** only a complete validated pyramid is moved into the public tile root

#### Scenario: Serve complete files independently of metadata
- **WHEN** a validated pyramid has moved into the public tile root but its ready commit has not completed, or its metadata was deleted before cleanup
- **THEN** its existing WebP files may be served without checking database state
- **AND** static delivery does not recheck current ownership, processing state, or database existence
- **AND** metadata and SSE expose its tile template only after a successful ready commit
- **AND** recovery or cleanup eventually rebuilds or removes unreferenced files

### Requirement: Process admitted styles independently and sequentially
At most one style SHALL be tiled at a time in the initial deployment. Admitted work SHALL transition from queued to processing and then ready or failed. Work SHALL be discoverable after HTTP 200 regardless of whether a viewer connects. Failure of one style SHALL NOT roll back admitted siblings or prevent their processing.

#### Scenario: Continue after one style fails
- **WHEN** Atlas and Roads are admitted independently to a map and Atlas fails during processing
- **THEN** Atlas becomes failed and Roads can still become ready
- **AND** the original successful upload response and saved originals remain valid

#### Scenario: Avoid concurrent image processing
- **WHEN** several uploads are queued for one or more maps and games
- **THEN** only one tiling operation runs at once while other work remains queued

### Requirement: Keep exactly one original per style
Each style SHALL own one immutable PNG/JPEG original and one tile pyramid. Image replacement, source-version history, and image-version identifiers SHALL NOT be exposed. A different original SHALL require a new style. Metadata PATCH SHALL NOT change original bytes or enqueue processing. Removing an original's style SHALL NOT remove its map or content.

#### Scenario: Refuse image replacement
- **WHEN** a file is posted to a style image-replacement path
- **THEN** no replacement operation exists and the style's original remains unchanged

#### Scenario: Change visual styles without changing map content
- **WHEN** a new style is added or any old style is removed
- **THEN** every surviving style retains its own identity and original, and map markers remain unchanged

### Requirement: Retry failed processing from the retained original
POST /games/{gameId}/maps/{mapId}/styles/{styleId}/retry SHALL requeue only a failed style and return HTTP 200 with the complete style. It SHALL reuse retained original bytes without a new upload, clear the previous error, and reset progress to 0. Retrying queued, processing, or ready work SHALL return HTTP 409 without duplication.

#### Scenario: Retry failed style processing
- **WHEN** a client retries a failed style
- **THEN** the same style ID is queued again and its saved original is reused
- **AND** a fresh internal attempt token fences callbacks from the previous attempt

#### Scenario: Reject redundant retry
- **WHEN** a client retries work not in failed state
- **THEN** HTTP 409 is returned without creating another processing attempt

### Requirement: Recover interrupted processing after restart
Accepted queued work SHALL survive application restart. Styles left processing by interruption SHALL be retried automatically from their retained original beginning at progress 0. Ready styles SHALL remain available. Recovery SHALL NOT require an SSE client, another upload, or restoration of in-memory queue contents.

#### Scenario: Restart with queued and interrupted work
- **WHEN** the application stops with queued and processing styles and later starts again
- **THEN** both become eligible for sequential processing and interrupted generation starts from the beginning
- **AND** other ready styles and originals are preserved

### Requirement: Stream current per-style progress through one map subscription
GET /games/{gameId}/maps/{mapId}/events SHALL return text/event-stream for an existing map. It SHALL immediately emit a snapshot containing mapId, defaultStyleId, and all complete style representations, then progress events with the same shape when state changes. Reconnection SHALL start with current state rather than replaying historical events. Every subscriber SHALL independently observe the map.

#### Scenario: Connect during or after processing
- **WHEN** a client opens the map event stream
- **THEN** the first snapshot contains current queued, processing, ready, or failed style state
- **AND** later progress events identify styles by ID and include error and tile-template changes

#### Scenario: Reconnect without interrupting work
- **WHEN** a viewer disconnects and reconnects while processing continues
- **THEN** the new stream starts with a current snapshot and no event-history replay is required
- **AND** disconnecting or never connecting a viewer does not cancel processing

#### Scenario: Observe independently
- **WHEN** two clients subscribe to the same map
- **THEN** each receives its own initial snapshot and subsequent state changes
- **AND** one client's reads cannot consume updates intended for the other

### Requirement: Report progress without promising unavailable exact work counts
Progress SHALL reset to 0 for a newly queued or restarted attempt and SHALL reach 100 only after complete publication. In-attempt progress SHALL be nondecreasing and may be approximate while processing. Failed styles SHALL retain their last reported progress and a safe error. Heartbeats SHALL keep idle SSE subscriptions open without fabricating progress.

#### Scenario: Finish and restart progress accurately
- **WHEN** processing approaches completion but publication is not committed
- **THEN** reported progress remains below 100 until publication succeeds
- **AND** a recovery or manual retry may start a new attempt at 0

#### Scenario: Keep an idle subscription valid
- **WHEN** all styles are terminal and no new work is submitted
- **THEN** the SSE connection can remain open with heartbeat events and no false processing updates

### Requirement: Patch style metadata with shared merge-patch semantics
PATCH /games/{gameId}/maps/{mapId}/styles/{styleId} SHALL accept optional name and backgroundColor and return HTTP 200 with the complete style. Both supported JSON Merge Patch media types and charset parameters SHALL work. Empty objects and null required values SHALL return HTTP 422; unknown/read-only fields or invalid JSON shapes HTTP 400; unsupported media types HTTP 415. No PUT route SHALL exist.

#### Scenario: Update metadata atomically without retiling
- **WHEN** a patch supplies a valid name and backgroundColor
- **THEN** both changes commit together without changing image source or processing status
- **AND** an invalid or conflicting supplied value rejects the complete patch

#### Scenario: Preserve omission and metadata no-ops
- **WHEN** a nonempty patch omits one field or supplies only current normalized values
- **THEN** omitted fields remain unchanged and a metadata no-op preserves updatedAt without a write
- **AND** metadata may be edited while image processing runs

### Requirement: Select the earliest displayable style by creation order
defaultStyleId SHALL identify the earliest-created surviving ready style. Persisted creation sequence SHALL break creation-time ties, later styles SHALL append, and renaming SHALL NOT reorder styles. Queued, processing, and failed styles SHALL be skipped. There SHALL be no editable default or order field.

#### Scenario: Skip an unfinished first style
- **WHEN** the first-created style has never become ready but a later style is displayable
- **THEN** the later style is the default
- **AND** when the earlier style becomes ready it becomes the default

#### Scenario: Choose a new default after deletion
- **WHEN** the default style is deleted while other styles remain
- **THEN** the earliest surviving displayable style becomes default
- **AND** defaultStyleId is null if no remaining style is displayable

### Requirement: Allow deletion of every style independently of the map
DELETE /games/{gameId}/maps/{mapId}/styles/{styleId} SHALL physically remove the scoped style and return empty HTTP 204 after committed removal and durable cleanup scheduling. The last style SHALL be removable regardless of processing state. Map identity, geometry, groups, categories, markers, media, and coordinates SHALL remain unchanged. Missing/repeated deletion SHALL return HTTP 404.

#### Scenario: Remove the final queued ready or failed style
- **WHEN** the only style of a map is deleted in any processing state
- **THEN** the map remains readable with styles=[] and defaultStyleId=null, and its owned content remains available
- **AND** old processing cannot restore deleted style metadata

#### Scenario: Delete remaining styles concurrently
- **WHEN** distinct final styles are deleted concurrently from one map
- **THEN** both successful deletions may leave zero styles without deleting or resizing the map

### Requirement: Delete a map and its complete owned content
DELETE /games/{gameId}/maps/{mapId} SHALL return empty HTTP 204 after physically deleting the map, styles, groups, categories, markers, and marker-media records and durably scheduling map artifact cleanup. Other maps and the game SHALL remain unchanged. Referenced marker-media files SHALL NOT be deleted. Missing/repeated deletion SHALL return HTTP 404.

#### Scenario: Delete one of several maps
- **WHEN** a map containing groups, categories, markers, media, and processing styles is deleted
- **THEN** only that ownership hierarchy disappears and its SSE stream emits mapDeleted and closes
- **AND** sibling maps, their content/files/counts/coordinates, and game timestamps remain unchanged

#### Scenario: Delete an empty map or recreate a name
- **WHEN** a map without styles is deleted
- **THEN** deletion succeeds and frees its game-scoped name
- **AND** a later map using that name receives a new identity and does not inherit deleted content

### Requirement: Remove map files durably and prevent stale publication
Map or style deletion SHALL durably schedule physical cleanup of owned original, tile, staging, and working files. Temporary cleanup failures SHALL be retried after restart without restoring deleted resources. A deleted game SHALL also lose all its maps and their artifacts while retaining existing game-deletion cascades. Background work SHALL NOT resurrect deleted metadata or advertise stale tile output.

#### Scenario: Delete during image processing
- **WHEN** a style, map, or owning game is deleted while tiling is in progress
- **THEN** its metadata ceases being publicly accessible after commit
- **AND** complete tile files may remain accessible until cleanup
- **AND** late processing output is discarded and cleanup eventually removes its owned files

#### Scenario: Retry filesystem cleanup
- **WHEN** committed deletion is followed by a temporary filesystem failure and application restart
- **THEN** cleanup is resumed until the owned artifacts are removed
- **AND** unrelated map storage and marker-media references are never deleted

### Requirement: Keep storage durable configurable and outside the repository history
Originals, tiles, and work files SHALL use a configured local storage root. The default root and its generated descendants SHALL be ignored by Git. Storage paths SHALL be generated by the server and remain inside that root. Accepted originals, style metadata, processing state, and ready tile metadata SHALL survive restart without public exposure of filesystem paths.

#### Scenario: Use safe owned storage
- **WHEN** an uploaded filename or multipart key contains path-like text
- **THEN** it cannot select an absolute or relative server path or access another map
- **AND** local original, tile, and staging folders remain untracked

#### Scenario: Recover admission interruption
- **WHEN** an upload is rejected or interrupted before durable admission
- **THEN** no queued visible map mutation is left behind
- **AND** unreferenced staging or promoted files are cleaned without deleting another upload's files

### Requirement: Return safe errors across HTTP and SSE phases
Expected HTTP failures SHALL use ProblemDetails and unexpected failures before response start SHALL return safe HTTP 500. Processing failures SHALL appear as failed style state and SHALL NOT terminate the worker. Stream failures after headers SHALL send a safe error event and close; deleted maps SHALL emit mapDeleted and close. Internal diagnostics SHALL be logged without being exposed.

#### Scenario: Fail before and after response start
- **WHEN** a missing map is requested for SSE before headers are sent
- **THEN** HTTP 404 ProblemDetails is returned
- **AND** a later stream failure uses an error event instead of appending ProblemDetails to an active event stream

#### Scenario: Surface a native processing failure safely
- **WHEN** an image decoder or tile write fails after admission
- **THEN** the style becomes failed and its original remains available for retry
- **AND** subscribers receive a safe code and message without exception details or physical paths

### Requirement: Document maps content ownership and rendering metadata
OpenAPI SHALL document JSON map creation, complete map listing, explicit map routes, map/group DTO changes, style upload forms, SSE, geometry, retries, PATCH, and errors. Game responses SHALL have no singular mapId. Tile documentation SHALL describe static WebP GET/HEAD, cache validators, no database checks, and eventual cleanup without a tile API operation.

#### Scenario: Discover independent map and style creation
- **WHEN** a client reads OpenAPI
- **THEN** CreateMap documents required name/width/height JSON and AddMapStyle documents required name/backgroundColor/file multipart fields
- **AND** ListMaps returns a plain array; map and content routes require their game/map identifiers, and legacy singular/game-wide content routes are absent

### Requirement: Validate map names within their game
Map names SHALL be trimmed, preserve display spelling, contain 1..200 characters, and be unique per game after invariant case normalization. Duplicate names SHALL return HTTP 409 with name errors, including recognized save-time conflicts. Different games SHALL allow equal names. No editable map order, selected-map field, or map metadata PATCH SHALL be introduced in this scope.

#### Scenario: Name uniqueness and scope
- **WHEN** a game already has World and another creation supplies ' world '
- **THEN** HTTP 409 preserves the existing map
- **AND** World is allowed in another game, and deleting the original frees its name

### Requirement: List all maps of a game
GET /games/{gameId}/maps SHALL return HTTP 200 with a plain array of complete map representations ordered by createdAt then id ascending. Existing games with no maps SHALL return []. Missing games SHALL return HTTP 404. Pagination, search, alternate sorting, and automatic selected-map state SHALL NOT be introduced.

#### Scenario: Complete deterministic isolated list
- **WHEN** a game owns blank and styled maps, including equal creation timestamps
- **THEN** each map appears once with full geometry and styles, ordered by createdAt then id
- **AND** maps of other games never appear

### Requirement: Preserve existing data when adopting map ownership
Ownership migration SHALL preserve existing IDs, dimensions, metadata, coordinates, media, and file identities. Existing groups SHALL be assigned to an explicitly determined map of their game. Missing or ambiguous assignments, missing names/dimensions, or destination conflicts SHALL stop the upgrade before destructive changes. The upgrade SHALL NOT invent dimensions, silently delete content, or modify applied migrations.

#### Scenario: Upgrade a prepared legacy game
- **WHEN** legacy maps have supplied valid names and every legacy group has one valid destination map
- **THEN** the upgrade preserves all descendant IDs, values, timestamps, and original/tile paths while establishing map ownership

#### Scenario: Refuse an unresolved legacy assignment
- **WHEN** a game has legacy groups but no prepared map or an assignment is ambiguous
- **THEN** the upgrade fails without discarding or partially moving any data
- **AND** an operator must explicitly supply the missing map/name/dimensions or mapping before retrying
