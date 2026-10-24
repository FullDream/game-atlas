# Maps Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Add one style through a flat typed multipart contract`
- TO: `### Requirement: Attach one library image as a map style`

## MODIFIED Requirements

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

### Requirement: Expose processing state directly on each style
Style representations SHALL retain id, name, backgroundColor, status, progress, error, tilesUrlTemplate, createdAt, and updatedAt and add source as the shared image-asset DTO. Existing queued/processing/ready/failed and progress semantics SHALL remain. source SHALL resolve the referenced asset, not a copied original/version model. Tile templates SHALL remain null until successful publication.

#### Scenario: Report queued and ready styles
- **WHEN** an accepted style has not completed processing
- **THEN** its status and progress describe its own processing attempt and tilesUrlTemplate is null
- **AND** after publication it becomes ready with progress 100 and a style-addressed tile template

#### Scenario: Report processing failure safely
- **WHEN** a style's processing fails
- **THEN** tilesUrlTemplate remains null and status is failed
- **AND** error exposes only a safe code and message, never paths, stack traces, or library diagnostics

#### Scenario: Resolve source metadata without retiling
- **WHEN** a source asset is renamed, moved, or given new alt text
- **THEN** subsequent map/style/SSE representations resolve that metadata without changing style timestamps, attempts, pixels, or tile URLs

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
- **AND** metadata and SSE expose its tile template only after a successful ready commit
- **AND** recovery or cleanup eventually rebuilds or removes unreferenced files

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
Map DELETE SHALL retain empty 204, ownership cascades, sibling isolation, SSE closure, and durable derived-output cleanup. Its style source links, category icon links, and marker attachment rows SHALL disappear with their resources, but shared assets, preview links owned by surviving videos, and original bytes SHALL remain. No asset deletion SHALL follow automatically from deleting a map or game.

#### Scenario: Delete one of several maps
- **WHEN** a map containing groups, categories, markers, media, and processing styles is deleted
- **THEN** only that ownership hierarchy disappears and its SSE stream emits mapDeleted and closes
- **AND** sibling maps, their content/files/counts/coordinates, and game timestamps remain unchanged

#### Scenario: Delete an empty map or recreate a name
- **WHEN** a map without styles is deleted
- **THEN** deletion succeeds and frees its game-scoped name
- **AND** a later map using that name receives a new identity and does not inherit deleted content

### Requirement: Remove map files durably and prevent stale publication
Map/style/game deletion SHALL durably clean only map-owned tiles and work output, never shared library originals. Cleanup failures SHALL retry after restart. Stale processing SHALL NOT restore deleted metadata or advertise output. No source-file leases SHALL be introduced. Once a style relationship is removed, active native reading SHALL NOT independently block explicit asset deletion; disappearance may fail the attempt.

#### Scenario: Delete during image processing
- **WHEN** a style, map, or game is deleted during tiling
- **THEN** resource metadata disappears after commit and stale output cannot be advertised
- **AND** source assets remain unless explicitly deleted through the library, and only derived files are eventually removed

#### Scenario: Retry filesystem cleanup
- **WHEN** derived-output deletion fails and the application restarts
- **THEN** cleanup resumes without deleting shared originals or unrelated outputs
- **AND** source assets remain discoverable even after their final style is gone

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

### Requirement: Document maps content ownership and rendering metadata
OpenAPI SHALL retain nine map operations, explicit map scope, fifteen-field MapDto, geometry, retry, SSE, and static WebP delivery. AddMapStyle SHALL document name/backgroundColor/sourceAssetId JSON rather than multipart; style DTOs SHALL include shared source image metadata. Documentation SHALL explain public library originals and asset preservation during derived-output/resource deletion.

#### Scenario: Discover independent map and style creation
- **WHEN** a client reads OpenAPI
- **THEN** CreateMap remains name/width/height JSON and AddMapStyle becomes name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent

## REMOVED Requirements

### Requirement: Preserve existing data when adopting map ownership
**Reason**: Legacy map-ownership conversion is no longer supported; preparation commands and models have been removed.
**Migration**: Initialize a fresh empty database with the full migration chain. Existing databases and files are never reset or deleted automatically.
