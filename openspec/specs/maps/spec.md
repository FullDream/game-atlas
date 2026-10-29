# maps Specification

## Purpose

Manage independent game-owned coordinate spaces with map-owned content, optional aligned styles, durable originals, recoverable tile generation, and per-map progress.

## Requirements

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

### Requirement: Attach one library image as a map style

POST /games/{gameId}/maps/{mapId}/styles SHALL accept application/json with required name, backgroundColor, and sourceAssetId. It SHALL atomically attach one compatible committed image, enqueue one style, and return 200 with the complete map. Missing sources SHALL return 404; invalid names/colors or incompatible images 422; name conflicts 409; multipart or unsupported request media 415. No file upload SHALL occur here.

#### Scenario: Append one style independently of wire order

- **WHEN** valid JSON identifies an existing image asset with name and backgroundColor in any property order
- **THEN** exactly one queued style and its asset relationship commit before 200
- **AND** source bytes are not copied or moved and existing styles remain unchanged

#### Scenario: Reject a conflicting name

- **WHEN** the supplied style name conflicts with an existing style in that map
- **THEN** 409 rejects attachment without changing the source asset, files, existing styles, order, or defaults

#### Scenario: Reject batch or ambiguous input

- **WHEN** style creation receives multipart/file input or an array instead of the JSON object contract
- **THEN** unsupported media returns 415 and incompatible JSON shapes return 400
- **AND** field validation uses name, backgroundColor, and sourceAssetId rather than file or multipart metadata

### Requirement: Enforce configurable single-file upload limits

Original file admission SHALL occur only through the media library under its one configurable default 314572800-byte file limit. Style attachment SHALL NOT introduce a separate byte limit or upload path. Raster dimensions SHALL remain 1..16384 and exactly match the map; incompatible dimensions SHALL return 422 without resizing bytes or changing map geometry.

#### Scenario: Accept configured boundaries

- **WHEN** a valid library image is within the configured shared byte/dimension bounds and exactly matches its map
- **THEN** it can be attached without downscaling or copying

#### Scenario: Reject oversize and dimension violations

- **WHEN** a file exceeds the shared library byte limit
- **THEN** the library rejects upload with 413 before style creation
- **AND** attaching an image with incompatible map dimensions returns 422

#### Scenario: Apply changed configuration

- **WHEN** an operator changes the shared admission limit and restarts the application
- **THEN** later library uploads use that limit without rewriting existing assets or map geometry

### Requirement: Accept supported image originals without modifying them

Style sources SHALL reference verified static JPEG, PNG, WebP, or AVIF library images. SVG, videos, folders, and unsupported images SHALL return 422. Byte preservation and content identification SHALL be owned by the library; map attachment SHALL check current compatibility. Decode-only failures SHALL produce safe failed style state. Supporting a format SHALL require a working decoder on the deployment runtime.

#### Scenario: Preserve an accepted original

- **WHEN** a supported library image is attached to one or several styles
- **THEN** every style reads the same unchanged committed bytes through its asset reference
- **AND** neither filename nor MIME input selects a filesystem path

#### Scenario: Reject unsupported images

- **WHEN** a sourceAssetId identifies SVG, a video, or an incompatible image
- **THEN** 422 rejects the entire style creation without visible map changes

#### Scenario: Verify expanded raster formats

- **WHEN** matching static WebP or AVIF assets are attached
- **THEN** they generate the same geometry and transparent lossless WebP tile contract as JPEG/PNG
- **AND** animation or an unavailable decoder cannot be silently treated as a supported static original

### Requirement: Keep all styles aligned in the map's immutable geometry

Map creation SHALL establish immutable dimensions before any style. Each source asset SHALL exactly match those dimensions and be an eligible raster image. Mismatches SHALL return 422 before attachment. Adding, retrying, or removing styles or changing library metadata SHALL NOT resize maps, alter source pixels, or reposition markers. No resizing endpoint SHALL be exposed.

#### Scenario: Validate the first and later styles

- **WHEN** any source image, including the first, differs from the map width or height
- **THEN** 422 preserves the map, existing styles, source asset, and markers
- **AND** a compatible image can be attached without detecting semantic image alignment

#### Scenario: Keep geometry after removing all images

- **WHEN** the last style is deleted and a later matching library image is attached
- **THEN** map identity, dimensions, coordinates, and content remain valid throughout
- **AND** the previously used source asset remains in the library

### Requirement: Validate unique user-defined style names

Styles SHALL support user-defined names rather than a fixed enum of styles. Names SHALL be trimmed, preserve display spelling, contain 1 through 200 characters, and be unique within their map after invariant case normalization. Invalid names SHALL return HTTP 422; conflicting names SHALL return HTTP 409, including recognized save-time uniqueness conflicts.

#### Scenario: Normalize and distinguish names

- **WHEN** a style is named ' Atlas '
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

Style representations SHALL retain id, name, backgroundColor, source, status, progress, error, tilesUrlTemplate, createdAt, and updatedAt. Status and safe error information SHALL be durable; intermediate progress SHALL be transient. source SHALL resolve the referenced shared image asset. Tile templates SHALL remain null until successful publication. Percentage changes alone SHALL NOT change persisted timestamps.

#### Scenario: Report queued and ready styles

- **WHEN** an accepted style has not completed processing
- **THEN** its status and transient progress describe its own processing attempt and tilesUrlTemplate is null
- **AND** after publication it becomes ready with progress 100 and a style-addressed tile template

#### Scenario: Report processing failure safely

- **WHEN** a style's processing fails
- **THEN** tilesUrlTemplate remains null and status is failed
- **AND** error exposes only a safe code and message, never paths, stack traces, or library diagnostics
- **AND** the last percentage is available only while its transient state is retained; absent transient progress is represented as 0

#### Scenario: Resolve source metadata without retiling

- **WHEN** a source asset is renamed, moved, or given new alt text
- **THEN** subsequent HTTP map/style representations resolve that metadata without changing style timestamps, attempts, pixels, or tile URLs
- **AND** processing notifications do not synchronize source metadata

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

- **WHEN** a client requests a legacy original path, staging file, work tile, manifest, directory, or nonexistent tile coordinate
- **THEN** 404 preserves private working paths and only complete pyramids exist under the public tile root
- **AND** committed originals are intentionally available through their separate shared /media/files/{id} endpoint

#### Scenario: Serve complete files independently of metadata

- **WHEN** a validated pyramid has moved into the public tile root but its ready commit has not completed, or its metadata was deleted before cleanup
- **THEN** its existing WebP files may be served without checking database state
- **AND** static delivery does not recheck current ownership, processing state, or database existence
- **AND** metadata advertises its tile template and notifications advertise ready only after a successful ready commit
- **AND** startup recovery or cleanup eventually removes failed or unreferenced derived output without deleting ready output

### Requirement: Process admitted styles independently and sequentially

At most one style SHALL be tiled at a time in the initial deployment. Admitted work SHALL transition from queued to processing and then ready or failed. Committed admission and manual retry SHALL trigger processing without periodic database discovery or a connected viewer. Failure of one style SHALL NOT roll back admitted siblings or prevent their processing. Duplicate dispatch SHALL NOT duplicate a processing attempt.

#### Scenario: Continue after one style fails

- **WHEN** Atlas and Roads are admitted independently to a map and Atlas fails during processing
- **THEN** Atlas becomes failed and Roads can still become ready
- **AND** the original successful upload response and saved originals remain valid

#### Scenario: Avoid concurrent image processing

- **WHEN** several styles are queued for one or more maps and games
- **THEN** only one tiling operation runs at once while other work remains queued

#### Scenario: Dispatch after a successful commit

- **WHEN** style admission or retry commits successfully
- **THEN** its current attempt becomes eligible for processing without a periodic database scan
- **AND** a rolled-back transaction produces no runnable work
- **AND** request cancellation after commit does not discard committed work

### Requirement: Keep exactly one original per style

Each style SHALL reference exactly one immutable library image for its lifetime and own its derived tile pyramid. Several styles MAY reference the same asset without byte duplication. Image replacement/version history SHALL NOT be exposed. A different source SHALL require a new style. Metadata PATCH SHALL NOT change sourceAssetId or image bytes. Removing a style SHALL remove only its relationship and derived output.

#### Scenario: Refuse image replacement

- **WHEN** a client attempts an image replacement path or patches sourceAssetId on an existing style
- **THEN** no replacement occurs and the original reference remains unchanged

#### Scenario: Change visual styles without changing map content

- **WHEN** a style is added or removed
- **THEN** surviving styles retain their own references/pyramids, markers remain unchanged, and every source asset remains in the library

### Requirement: Retry failed processing from the retained original

POST /games/{gameId}/maps/{mapId}/styles/{styleId}/retry SHALL requeue only a failed style and return HTTP 200 with the complete style. It SHALL reuse retained original bytes without a new upload, clear the previous error, and reset progress to 0. Retrying queued, processing, or ready work SHALL return HTTP 409 without duplication.

#### Scenario: Retry failed style processing

- **WHEN** a client retries a failed style
- **THEN** the same style ID is queued again and its saved original is reused
- **AND** a fresh internal attempt token fences callbacks from the previous attempt

#### Scenario: Reject redundant retry

- **WHEN** a client retries work not in failed state
- **THEN** HTTP 409 is returned without creating another processing attempt

#### Scenario: Handle an out-of-band missing source

- **WHEN** retry encounters a library source whose physical file was removed manually
- **THEN** it fails safely under the existing processing-state contract without removing the asset or source relationship
- **AND** the failure does not block processing of unrelated styles

### Requirement: Recover interrupted processing after restart

Accepted queued work SHALL survive application restart and be restored once at startup. Styles left processing SHALL become failed with a safe interruption error and durable derived-output cleanup, without automatic retry. Ready styles and shared originals SHALL be preserved. Recovery SHALL finish before normal processing and subscriptions start and SHALL NOT require a viewer or recurring database scans.

#### Scenario: Restart with queued and interrupted work

- **WHEN** the application stops with queued and processing styles and later starts again
- **THEN** queued styles become eligible for sequential processing
- **AND** interrupted styles become failed with progress 0 and require explicit manual retry
- **AND** their work and tile files are removed, with durable retry if filesystem cleanup fails
- **AND** other ready styles and all shared originals are preserved

#### Scenario: Crash after moving a complete pyramid

- **WHEN** the server stops after moving output into public storage but before committing ready
- **THEN** startup treats the persisted processing style as interrupted and removes that output instead of completing or requeuing it

#### Scenario: Crash between admission and dispatch

- **WHEN** a queued style commits but the process stops before dispatching it in memory
- **THEN** startup discovers and dispatches that queued attempt once without requiring a client retry

### Requirement: Stream current per-style progress through one map subscription

The system SHALL replace the map SSE route with a typed MapHub using JoinMap and LeaveMap to manage SignalR group membership by mapId. Handlers and the worker SHALL explicitly publish StyleUpdated with full committed styles and ProcessingUpdated with percentage/deletion deltas containing mapId, styleId, attemptId, status, progress, error, and deleted. Queue admission, SQLite mutation coordination, and file cleanup SHALL NOT implicitly publish resource notifications. Lifecycle transitions SHALL be sent through StyleUpdated without a duplicate ProcessingUpdated event. The Hub SHALL NOT retain application-level subscriptions, snapshots or connection registries. Joining SHALL NOT read the database, validate resource existence, or send initial state. No streaming-query client is required.

#### Scenario: Connect during or after processing

- **WHEN** a client joins a map group
- **THEN** it obtains the current map and styles through the existing HTTP read after joining
- **AND** subsequent committed style changes and transient progress are sent directly to the group without database polling
- **AND** HTTP state and buffered events require client reconciliation rather than an atomic Hub snapshot guarantee

#### Scenario: Reconnect without interrupting work

- **WHEN** a viewer disconnects and reconnects while processing continues
- **THEN** it explicitly rejoins and refetches current HTTP state rather than replaying historical events
- **AND** disconnecting or never connecting does not cancel processing
- **AND** stale attempts, deleted resources, and delayed metadata require reconciliation without regressing cached state

#### Scenario: Observe independently

- **WHEN** two clients join the same map
- **THEN** SignalR sends the same change to both group members
- **AND** leaving one map preserves a connection's other memberships
- **AND** notification failures are bounded and cannot turn committed work into a failure

### Requirement: Report progress without promising unavailable exact work counts

Progress SHALL be transient, approximate, and nondecreasing within an attempt, reset to 0 on manual retry, and remain below 100 until ready commits. Intermediate changes SHALL NOT trigger database writes. Changed percentages SHALL be notified at most about once per second per map; committed lifecycle transitions SHALL bypass percentage throttling. Ready SHALL report 100; other states without retained transient progress SHALL report 0.

#### Scenario: Finish and restart progress accurately

- **WHEN** processing approaches completion but publication is not committed
- **THEN** reported progress remains below 100 until publication succeeds
- **AND** manual retry starts a new attempt at 0 and stale callbacks cannot update it
- **AND** failed progress is not promised to survive process restart

#### Scenario: Keep an idle subscription valid

- **WHEN** a subscribed map has no queued or processing styles
- **THEN** the client derives completion from style statuses while membership remains active until LeaveMap or disconnect
- **AND** no application-level idle heartbeat or recurring database read is retained, and later style changes are delivered to the same group

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

Style DELETE SHALL remove the scoped style/source relationship and durably schedule only its derived tile/work cleanup before returning empty 204. The last style SHALL remain removable in any processing state. Source assets SHALL remain in the library even without other uses. Missing/repeated deletion SHALL return 404. Existing map geometry/content/default rules SHALL remain unchanged.

#### Scenario: Remove the final queued ready or failed style

- **WHEN** the only style of a map is deleted in any processing state
- **THEN** the map remains readable with styles=[] and defaultStyleId=null, and its owned content remains available
- **AND** old processing cannot restore deleted style metadata

#### Scenario: Delete remaining styles concurrently

- **WHEN** distinct final styles are deleted concurrently from one map
- **THEN** both successful deletions may leave zero styles without deleting or resizing the map

#### Scenario: Preserve a source shared by several styles

- **WHEN** two styles share an asset and one or both styles are deleted
- **THEN** only their derived output and references are removed; original bytes remain until explicit eligible asset deletion

### Requirement: Delete a map and its complete owned content

Map DELETE SHALL retain empty 204, ownership cascades, sibling isolation, and durable derived-output cleanup. Active processing subscribers SHALL receive ProcessingUpdated with deleted:true and let clients leave the group. Its style source links, category icon links, and marker attachment rows SHALL disappear with their resources, but shared assets, preview links owned by surviving videos, and original bytes SHALL remain. No asset deletion SHALL follow automatically from deleting a map or game.

#### Scenario: Delete one of several maps

- **WHEN** a map containing groups, categories, markers, media, and processing styles is deleted
- **THEN** only that ownership hierarchy disappears and its active processing subscribers receive ProcessingUpdated with deleted:true and let clients leave the groups
- **AND** sibling maps, their content/files/counts/coordinates, and game timestamps remain unchanged

#### Scenario: Delete an empty map or recreate a name

- **WHEN** a map without styles is deleted
- **THEN** deletion succeeds and frees its game-scoped name
- **AND** a later map using that name receives a new identity and does not inherit deleted content or subscriptions

### Requirement: Remove map files durably and prevent stale publication

Map/style/game deletion and processing failure SHALL durably clean only map-owned tiles and work, never shared originals. Cleanup SHALL start from committed operations, retry known failures at their due time, and resume after restart without periodic table scans. Stale processing SHALL NOT restore deleted metadata or advertise output. No source-file leases SHALL be introduced; removing a style SHALL NOT independently block eligible asset deletion.

#### Scenario: Delete during image processing

- **WHEN** a style, map, or game is deleted during tiling
- **THEN** resource metadata disappears after commit and stale output cannot be advertised
- **AND** source assets remain unless explicitly deleted through the library, and only derived files are eventually removed
- **AND** native writing stops before its output is removed

#### Scenario: Retry filesystem cleanup

- **WHEN** derived-output deletion fails and the application restarts
- **THEN** cleanup resumes without deleting shared originals or unrelated outputs
- **AND** source assets remain discoverable even after their final style is gone
- **AND** once known cleanup completes, the idle worker does not repeatedly query for more cleanup

#### Scenario: Retry while prior cleanup remains pending

- **WHEN** a failed style is retried before its previous cleanup succeeds
- **THEN** old cleanup cannot delete output of the new attempt
- **AND** processing unrelated styles can continue

### Requirement: Keep storage durable configurable and outside the repository history

Originals SHALL reside in the configured library asset store; tiles and work SHALL retain map/style-addressed storage. All generated stores SHALL be Git-ignored with server-owned paths and no path/link traversal. Asset/source relationships and existing map processing metadata SHALL survive restart. Physical paths SHALL remain private even though committed original bytes are publicly served through library URLs.

#### Scenario: Use safe owned storage

- **WHEN** an upload filename or logical folder name contains path-like input
- **THEN** it cannot select storage paths or access other assets/maps
- **AND** original, tile, staging, and work stores remain untracked

#### Scenario: Recover admission interruption

- **WHEN** a library upload is interrupted or style attachment fails before commit
- **THEN** request-level finally removes the temporary upload and failed attachment queues no style
- **AND** committed sources remain, while a process crash may leave temporary/orphaned files with no startup cleanup

### Requirement: Return safe errors across HTTP and SSE phases

HTTP failures SHALL retain ProblemDetails and safe unexpected HTTP 500 responses. Missing maps SHALL be reported through HTTP reads; group joins SHALL perform no resource lookup. Notification failures SHALL be logged without exposing internal diagnostics or changing committed work. Processing errors SHALL remain durable failed style state. Deletion SHALL emit ProcessingUpdated with deleted:true.

#### Scenario: Fail before and after response start

- **WHEN** a client joins a group for a missing map and requests its HTTP representation
- **THEN** HTTP returns the existing safe not-found response
- **AND** unexpected notification failures do not change a successful commit and current state remains recoverable through HTTP

#### Scenario: Surface a native processing failure safely

- **WHEN** an image decoder or tile write fails after admission
- **THEN** the style becomes failed and its original remains available for retry
- **AND** subscribers receive a safe code and message after failure commits, without exception details or physical paths

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the eight remaining map REST operations, explicit map scope, complete map/style representations, geometry, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap remains name/width/height JSON and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation

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
