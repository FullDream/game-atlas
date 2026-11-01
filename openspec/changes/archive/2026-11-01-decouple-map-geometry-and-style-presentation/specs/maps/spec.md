# Spec Delta

## MODIFIED Requirements

### Requirement: Create a named coordinate space without an image

POST /games/{gameId}/maps SHALL accept application/json with required name only and return 200 with a persisted map, UUID v7, UTC timestamps, styles=[] and defaultStyleId=null. Logical dimensions and geometry SHALL remain null until first successful style attachment. No file or processing work SHALL be created.

#### Scenario: Create a blank map

- **WHEN** valid JSON contains name=World
- **THEN** the map survives restart with no geometry, styles, files or processing work

#### Scenario: Reject invalid creation

- **WHEN** name is missing/null, blank or overlength
- **THEN** 422 returns field errors; malformed JSON, incompatible shapes and supplied width/height return 400; unsupported media returns 415, without changes

#### Scenario: Create content before the first style

- **WHEN** groups, categories or finite-coordinate markers are added before a style
- **THEN** content persists normally without invented map geometry; subsequent initialization preserves every marker coordinate

### Requirement: Enforce configurable single-file upload limits

Original admission SHALL remain in the media library under its configurable default 314572800-byte limit and raster bounds 1..16384. Style attachment SHALL introduce neither another upload path nor a byte limit. Differences from logical dimensions or aspect ratio SHALL NOT reject eligible images.

#### Scenario: Accept configured boundaries

- **WHEN** an eligible source within library bounds differs from map dimensions
- **THEN** attachment succeeds without modifying or copying original bytes

#### Scenario: Reject oversize and dimension violations

- **WHEN** a file exceeds library byte or dimension limits
- **THEN** the library rejects admission under its existing contract before style attachment

#### Scenario: Apply changed configuration

- **WHEN** an operator changes the shared admission limit and restarts
- **THEN** later uploads use that limit without rewriting assets or logical geometry

### Requirement: Keep all styles aligned in the map's immutable geometry

The first committed style attachment SHALL initialize previously absent logical dimensions from its source. Subsequent styles SHALL cover the same full extent with independent X/Y scaling regardless of resolution or aspect ratio. Adding, retrying, reordering, changing defaults or deleting styles SHALL preserve initialized geometry, original bytes and marker coordinates. No resizing endpoint SHALL exist.

#### Scenario: Validate the first and later styles

- **WHEN** 8000x8000 then 1000x1000 images are attached, or the reverse
- **THEN** the first establishes logical geometry and both cover its complete extent without moving markers

#### Scenario: Stretch a different aspect ratio

- **WHEN** a 1000x1000 source is attached to an 8000x4000 map
- **THEN** its full width and height map to the logical width and height without cropping or letterboxing

#### Scenario: Keep geometry after removing all images

- **WHEN** the last style is removed and any eligible image is attached later
- **THEN** existing logical dimensions and content remain intact and source assets remain in the library

#### Scenario: Initialize atomically

- **WHEN** competing first-style attachments run or one fails validation/save
- **THEN** only a committed winning attachment initializes geometry; failed operations leave no partial geometry and later attachments use the established space

#### Scenario: Keep geometry after processing failure

- **WHEN** the first attached style fails tile generation
- **THEN** geometry remains initialized and retries or another style do not redefine it

### Requirement: Return complete map metadata

GET /games/{gameId}/maps/{mapId} SHALL retain map metadata fields and return styles in persisted display order. Width, height, extent, tileOrigin, tileSize, minZoom, maxZoom and resolutions SHALL be null before initialization and populated afterward. Style representations SHALL additionally expose their own tile geometry. defaultStyleId SHALL identify the effective ready default or be null. IDs SHALL use UUID v7; internal paths SHALL remain private.

#### Scenario: Read a blank or styled map

- **WHEN** a client reads a map before or after initialization
- **THEN** the complete representation distinguishes absent geometry from initialized geometry, including after all styles are deleted

#### Scenario: Read ordered styles

- **WHEN** styles have been reordered
- **THEN** GET and mutation map responses return the same persisted order and each style's source-based tile geometry

### Requirement: Use centered original-pixel coordinates

Logical coordinates SHALL use the first attached image center as (0,0), X right and Y up, with extent [-width/2,-height/2,width/2,height/2] and tileOrigin [-width/2,height/2]. Each style SHALL map its source center and boundaries to the logical center and boundaries. Existing finite marker coordinates, including positions outside the extent, SHALL remain valid.

#### Scenario: Place the same marker on all styles

- **WHEN** a 10000x8000 map contains a marker at (0,0) and styles at different resolutions
- **THEN** the marker remains at every style's center and extent remains [-5000,-4000,5000,4000]

#### Scenario: Preserve fractional geometry and existing markers

- **WHEN** dimensions are odd or markers have negative/fractional positions
- **THEN** half-unit boundaries and marker coordinates are preserved without rounding or bounds validation

### Requirement: Generate a transparent power-of-two tile pyramid

Each style SHALL provide native-source-resolution lossless WebP tiles of 512x512 without overlap. Its zoom 0 SHALL fit the source within one tile; its maxZoom SHALL represent original-pixel resolution. Adjacent source resolutions SHALL differ by two. Edge padding SHALL be transparent and backgroundColor SHALL NOT be baked in. Tile geometry SHALL be independent of logical map dimensions.

#### Scenario: Render original and reduced levels

- **WHEN** a source exceeds one tile
- **THEN** its pyramid covers 0..maxZoom with the last source resolution equal to one source pixel, including sources larger than the first style

#### Scenario: Keep edge and source transparency

- **WHEN** an odd-sized or transparent image is tiled
- **THEN** source alpha and transparent edge padding remain, with lossless tile encoding

#### Scenario: Handle a small map

- **WHEN** both source dimensions are at most 512
- **THEN** its sole level is zoom 0 at source resolution 1 without enlarging the original

### Requirement: Select the earliest displayable style by creation order

The system SHALL persist the default independently of display order. An explicit selection SHALL require a ready style belonging to the map. Without a surviving ready selection, the first ready style in display order SHALL be chosen and retained as default; no ready styles SHALL produce null. Reordering or another style becoming ready SHALL NOT replace a surviving ready default.

#### Scenario: Skip an unfinished first style

- **WHEN** a later style becomes ready before the first
- **THEN** the later one becomes the default and stays default when the first becomes ready

#### Scenario: Choose a new default after deletion

- **WHEN** the selected default is deleted
- **THEN** the first surviving ready style in display order becomes default, or null if none exists

#### Scenario: Select a ready default

- **WHEN** an administrator selects a ready style
- **THEN** the selection persists across reload and changes default-derived previews without changing order or geometry

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the nine map REST operations, including the map metadata PATCH with name, defaultStyleId and styleOrder, explicit map scope, complete map/style representations, nullable logical geometry, independent style tile geometry, default/order updates, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap accepts name-only JSON and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation

### Requirement: Patch map name without resizing

PATCH /games/{gameId}/maps/{mapId} SHALL accept optional name, defaultStyleId and styleOrder with shared merge-patch semantics and return 200 with the complete map. Name SHALL be non-null when supplied, trimmed, 1..200 characters and unique within the game excluding the current map. Dimensions and geometry SHALL remain immutable. No PUT route SHALL be introduced.

#### Scenario: Rename a scoped map

- **WHEN** a valid patch changes the name of a map belonging to the route game
- **THEN** the name and normalized name are persisted with a new updatedAt
- **AND** createdAt, game timestamps, dimensions, geometry, styles, owned content remain unchanged; default and order remain unchanged when omitted

#### Scenario: Reject invalid patches atomically

- **WHEN** a patch supplies null, blank or overlength name, or an empty object
- **THEN** HTTP 422 returns field errors without changes
- **WHEN** the name conflicts with another map in the game, including a recognized save-time unique constraint conflict
- **THEN** HTTP 409 returns a name error without changes
- **WHEN** a patch contains width, height, geometry, other unknown/read-only properties, incompatible values, a non-object body or malformed JSON
- **THEN** HTTP 400 preserves the existing map

#### Scenario: Preserve scope and supported media

- **WHEN** a valid identifier refers to a missing game/map or a map of another game
- **THEN** HTTP 404 preserves all resources
- **WHEN** the request uses application/json or application/merge-patch+json, including charset parameters
- **THEN** both use identical merge-patch behavior
- **WHEN** another media type is supplied
- **THEN** HTTP 415 rejects the request

#### Scenario: Preserve no-op timestamps

- **WHEN** a nonempty patch supplies the existing name after trimming
- **THEN** HTTP 200 returns the unchanged complete map without changing updatedAt
- **AND** changing display casing alone is allowed without conflicting with the same map

## ADDED Requirements

### Requirement: Persist style order and validate map presentation patches

Map PATCH SHALL accept optional defaultStyleId and styleOrder alongside name, returning 200 with the complete map under shared merge-patch rules. styleOrder SHALL be a complete permutation of current style IDs; new styles SHALL append. Changes SHALL be atomic, no-ops SHALL preserve timestamps, and effective changes SHALL update only the map timestamp.

#### Scenario: Validate default ownership and readiness

- **WHEN** defaultStyleId is supplied
- **THEN** a foreign or missing style returns 404; null or a non-ready style returns 422, with no accompanying changes

#### Scenario: Validate order and preserve fields

- **WHEN** styleOrder contains duplicates, null, or omits current IDs
- **THEN** 422 rejects the entire patch; foreign or missing IDs return 404 and omitted fields remain unchanged

#### Scenario: Reject stale membership

- **WHEN** styles are added or deleted before an older complete order is saved
- **THEN** the stale order is rejected without dropping styles or changing default/name

#### Scenario: Keep default independent

- **WHEN** a complete order permutation is saved
- **THEN** all style states participate in ordering, the ready default stays selected, and no tiles or markers change

### Requirement: Preserve existing map data during geometry transition

A new explicitly applied migration SHALL preserve every existing map dimension, marker coordinate, source and ready tile URL. Existing styles SHALL receive display order from creation sequence and existing effective ready defaults SHALL be preserved. Existing initialized maps without styles SHALL remain initialized. Applied migrations SHALL NOT be rewritten or run on startup.

#### Scenario: Upgrade a populated current store

- **WHEN** the migration runs against maps with styles, content and derived output
- **THEN** geometry and content survive, existing styles retain their order/default and existing tile bytes/URLs remain usable
