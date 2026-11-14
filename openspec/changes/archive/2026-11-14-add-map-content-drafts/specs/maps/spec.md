# Spec Delta

## ADDED Requirements

### Requirement: Publish a map and all owned draft content atomically

Unauthenticated POST /games/{gameId}/maps/{mapId}/publish SHALL atomically transition the scoped map and all of its owned draft category groups, categories, and markers to `published`, and return HTTP 200 with the complete map representation. Only records whose status changes from `draft` to `published` SHALL update `updatedAt`. Missing or foreign game/map routes SHALL return HTTP 404 without changes.

#### Scenario: Publish a draft map with mixed draft and published content

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a draft map owning draft and published groups, categories, and markers
- **THEN** the map and every owned group, category, and marker persist with status `published`
- **AND** `updatedAt` advances only on the map and owned entities that were previously `draft`, while already-published entities retain their prior `updatedAt`

#### Scenario: Publish changes on an already published map

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a published map that contains at least one draft group, category, or marker
- **THEN** the draft child entities transition to `published` with a new `updatedAt` while the map itself retains its existing `updatedAt`

#### Scenario: Idempotent publish when all content is already published

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a published map whose groups, categories, and markers are all already `published`
- **THEN** HTTP 200 returns the complete map and no `updatedAt` timestamp changes on the map or any child entity

#### Scenario: Missing or foreign map on publish

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish targets a nonexistent game, nonexistent map, or a map belonging to another game
- **THEN** HTTP 404 ProblemDetails is returned without modifying any map or content

### Requirement: Backfill existing map and content publication statuses

An explicit migration SHALL add non-null string `Status` columns to `Maps`, `CategoryGroups`, `Categories`, and `Markers` and initialize all preexisting rows to `Published` (`published` in API responses) without changing their identities, ownership, coordinates, order, or timestamps. Applied migrations SHALL NOT be rewritten or run automatically on startup.

#### Scenario: Upgrade a populated store

- **WHEN** the publication status migration is applied to a database containing maps, category groups, categories, and markers
- **THEN** every preexisting row receives status `published` and retains its ID, relationships, and timestamps

## MODIFIED Requirements

### Requirement: Create a named coordinate space without an image

POST /games/{gameId}/maps SHALL accept application/json with required name and slug and optional status (`draft` or `published`, defaulting to `draft` when omitted) and return 200 with a persisted map, UUID v7, UTC timestamps, string status, styles=[] and defaultStyleId=null. Logical dimensions and geometry SHALL remain null until first successful style attachment. No file or processing work SHALL be created.

#### Scenario: Create a blank map

- **WHEN** valid JSON contains name=World and slug=world and omits status
- **THEN** the map is persisted and returned with status `draft` and survives restart with no geometry, styles, files or processing work

#### Scenario: Create a map with explicit status

- **WHEN** valid JSON contains name=World, slug=world, and status=`published` (or `draft`)
- **THEN** the map is persisted and returned with the supplied status

#### Scenario: Reject invalid creation

- **WHEN** name or slug is missing/null, blank or overlength, slug has invalid syntax, or status is null or an undefined enum value
- **THEN** 422 returns field errors; malformed JSON, incompatible shapes, unknown enum strings and supplied width/height return 400; unsupported media returns 415, without changes

#### Scenario: Create content before the first style

- **WHEN** groups, categories or finite-coordinate markers are added before a style
- **THEN** content persists normally without invented map geometry; subsequent initialization preserves every marker coordinate

### Requirement: Return complete map metadata

GET /games/{gameId}/maps/{mapId} SHALL include the required string slug and string status (`draft` or `published`), retain other map metadata fields and return styles in persisted display order. Width, height, extent, tileOrigin, tileSize, minZoom, maxZoom and resolutions SHALL be null before initialization and populated afterward. Style representations SHALL additionally expose their own tile geometry. defaultStyleId SHALL identify the effective ready default or be null. IDs SHALL use UUID v7; internal paths SHALL remain private.

#### Scenario: Read a blank or styled map

- **WHEN** a client reads a map before or after initialization
- **THEN** the complete representation includes its current status (`draft` or `published`) and distinguishes absent geometry from initialized geometry, including after all styles are deleted

#### Scenario: Read ordered styles

- **WHEN** styles have been reordered
- **THEN** GET and mutation map responses return the same persisted order and each style's source-based tile geometry

### Requirement: Patch map name without resizing

PATCH /games/{gameId}/maps/{mapId} SHALL accept optional name, slug, status, defaultStyleId and styleOrder with shared merge-patch semantics and return 200 with the complete map. Name SHALL be non-null when supplied, trimmed, 1..200 characters and unique within the game excluding the current map. Status SHALL be non-null (`draft` or `published`) when supplied and SHALL update only the map itself without cascading to owned category groups, categories, or markers. Dimensions and geometry SHALL remain immutable. No PUT route SHALL be introduced.

#### Scenario: Rename a scoped map

- **WHEN** a valid patch changes the name of a map belonging to the route game
- **THEN** the name and normalized name are persisted with a new updatedAt
- **AND** createdAt, game timestamps, dimensions, geometry, styles, status, and owned content remain unchanged; default and order remain unchanged when omitted

#### Scenario: Toggle map publication status without cascading to children

- **WHEN** a valid patch changes a map's status between `draft` and `published`
- **THEN** the map's status and updatedAt are updated while every owned category group, category, and marker retains its existing status and timestamps

#### Scenario: Reject invalid patches atomically

- **WHEN** a patch supplies null, blank or overlength name, null status, or an empty object
- **THEN** HTTP 422 returns field errors without changes
- **WHEN** the name conflicts with another map in the game, including a recognized save-time unique constraint conflict
- **THEN** HTTP 409 returns a name error without changes
- **WHEN** a patch contains width, height, geometry, other unknown/read-only properties, incompatible values, unknown status strings, a non-object body or malformed JSON
- **THEN** HTTP 400 preserves the existing map

#### Scenario: Preserve scope and supported media

- **WHEN** a valid identifier refers to a missing game/map or a map of another game
- **THEN** HTTP 404 preserves all resources
- **WHEN** the request uses application/json or application/merge-patch+json, including charset parameters
- **THEN** both use identical merge-patch behavior
- **WHEN** another media type is supplied
- **THEN** HTTP 415 rejects the request

#### Scenario: Preserve no-op timestamps

- **WHEN** a nonempty patch supplies the existing name after trimming and/or the existing status
- **THEN** HTTP 200 returns the unchanged complete map without changing updatedAt
- **AND** changing display casing alone is allowed without conflicting with the same map

#### Scenario: Patch slug with other metadata

- **WHEN** a patch supplies a valid unique slug with optional name, status, defaultStyleId or styleOrder changes
- **THEN** all supplied changes commit atomically and the complete response includes the new slug and status
- **AND** omitted fields remain unchanged, actual changes update only the map timestamp, and a patch containing only its current trimmed slug preserves updatedAt
- **AND** renaming a map without supplying slug preserves its slug

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the ten map REST operations, including CreateMap with optional status, the map metadata PATCH with name, slug, status, defaultStyleId and styleOrder, PublishMap at POST /games/{gameId}/maps/{mapId}/publish, explicit map scope, complete map/style representations with map status, nullable logical geometry, independent style tile geometry, default/order updates, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap accepts name, slug, and optional status JSON, UpdateMap includes optional status, PublishMap is exposed under the Maps tag at POST /games/{gameId}/maps/{mapId}/publish returning Map, and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation
