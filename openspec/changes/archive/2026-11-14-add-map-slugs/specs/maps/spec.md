# Spec Delta

## MODIFIED Requirements

### Requirement: Create a named coordinate space without an image

POST /games/{gameId}/maps SHALL accept application/json with required name and slug and return 200 with a persisted map, UUID v7, UTC timestamps, styles=[] and defaultStyleId=null. Logical dimensions and geometry SHALL remain null until first successful style attachment. No file or processing work SHALL be created.

#### Scenario: Create a blank map

- **WHEN** valid JSON contains name=World and slug=world
- **THEN** the map survives restart with no geometry, styles, files or processing work

#### Scenario: Reject invalid creation

- **WHEN** name or slug is missing/null, blank or overlength, or slug has invalid syntax
- **THEN** 422 returns field errors; malformed JSON, incompatible shapes and supplied width/height return 400; unsupported media returns 415, without changes

#### Scenario: Create content before the first style

- **WHEN** groups, categories or finite-coordinate markers are added before a style
- **THEN** content persists normally without invented map geometry; subsequent initialization preserves every marker coordinate

### Requirement: Return complete map metadata

GET /games/{gameId}/maps/{mapId} SHALL include the required string slug, retain other map metadata fields and return styles in persisted display order. Width, height, extent, tileOrigin, tileSize, minZoom, maxZoom and resolutions SHALL be null before initialization and populated afterward. Style representations SHALL additionally expose their own tile geometry. defaultStyleId SHALL identify the effective ready default or be null. IDs SHALL use UUID v7; internal paths SHALL remain private.

#### Scenario: Read a blank or styled map

- **WHEN** a client reads a map before or after initialization
- **THEN** the complete representation distinguishes absent geometry from initialized geometry, including after all styles are deleted

#### Scenario: Read ordered styles

- **WHEN** styles have been reordered
- **THEN** GET and mutation map responses return the same persisted order and each style's source-based tile geometry

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the nine map REST operations, including the map metadata PATCH with name, slug, defaultStyleId and styleOrder, explicit map scope, complete map/style representations, nullable logical geometry, independent style tile geometry, default/order updates, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap accepts name/slug JSON and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation

### Requirement: Patch map name without resizing

PATCH /games/{gameId}/maps/{mapId} SHALL accept optional name, slug, defaultStyleId and styleOrder with shared merge-patch semantics and return 200 with the complete map. Name SHALL be non-null when supplied, trimmed, 1..200 characters and unique within the game excluding the current map. Dimensions and geometry SHALL remain immutable. No PUT route SHALL be introduced.

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

#### Scenario: Patch slug with other metadata

- **WHEN** a patch supplies a valid unique slug with optional name, defaultStyleId or styleOrder changes
- **THEN** all supplied changes commit atomically and the complete response includes the new slug
- **AND** omitted fields remain unchanged, actual changes update only the map timestamp, and a patch containing only its current trimmed slug preserves updatedAt
- **AND** renaming a map without supplying slug preserves its slug

### Requirement: Persist style order and validate map presentation patches

Map PATCH SHALL accept optional defaultStyleId and styleOrder alongside name and slug, returning 200 with the complete map under shared merge-patch rules. styleOrder SHALL be a complete permutation of current style IDs; new styles SHALL append. Changes SHALL be atomic, no-ops SHALL preserve timestamps, and effective changes SHALL update only the map timestamp.

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

## ADDED Requirements

### Requirement: Validate client-supplied map slugs

Map slugs SHALL be trimmed and contain 1..200 characters of lowercase ASCII letters or digits separated by single hyphens. Missing, null, empty, whitespace-only, overlength or invalid slugs SHALL return HTTP 422 with slug field errors without changes. New map slugs SHALL be client-supplied without generation, lowercasing or repair. PATCH omission SHALL preserve slug; explicit null SHALL be rejected.

#### Scenario: Accept a trimmed slug

- **WHEN** a valid create or PATCH supplies slug=" world-2 "
- **THEN** the persisted and returned slug is "world-2"

#### Scenario: Reject invalid slug values atomically

- **WHEN** creation omits slug or creation/PATCH supplies null, empty, whitespace-only, 201 characters, uppercase, non-ASCII letters, spaces, repeated hyphens, or leading/trailing hyphens
- **THEN** HTTP 422 identifies slug and preserves all resource fields and timestamps
- **AND** incompatible JSON types return 400 rather than being converted to strings

### Requirement: Enforce game-scoped map slug uniqueness

At most one map per game SHALL own a slug. Creation or PATCH using another map's slug in that game SHALL return HTTP 409, including recognized save-time conflicts. Keeping the target map's slug SHALL be allowed. Different games SHALL allow equal map slugs. Existing game-scoped name uniqueness SHALL remain independent. Deleting a map SHALL release its slug.

#### Scenario: Reject duplicate creation or update

- **WHEN** another map of the routed game owns the supplied slug
- **THEN** HTTP 409 rejects the complete creation or PATCH, including accompanying valid name and presentation changes

#### Scenario: Resolve competing writes

- **WHEN** otherwise valid competing creations or updates claim the same game-scoped slug
- **THEN** at most one map owns that slug and recognized constraint losers receive 409
- **AND** unrelated persistence errors remain safe 500 responses

#### Scenario: Preserve scope and reuse

- **WHEN** maps of different games use slug=world or a map keeps its existing slug
- **THEN** slug uniqueness permits the operation, subject to independent name validation
- **AND** deleting a map permits a later map of the same game to reuse its slug

### Requirement: Backfill map slugs without losing existing data

An explicit upgrade SHALL assign every preexisting map the slug map-{id:N}, where id:N is its existing UUID in lowercase without hyphens. It SHALL preserve map identities, game ownership, names, timestamps, geometry, defaults, styles, processing state, content and files. Existing game slugs SHALL remain unchanged. Upgrades SHALL NOT rewrite previously applied migrations or run automatically at API startup.

#### Scenario: Upgrade a populated store

- **WHEN** an operator applies the slug migration to a store containing maps with arbitrary names and content
- **THEN** every old map receives its deterministic valid slug and existing metadata, relationships and files survive
- **AND** required game-scoped slug uniqueness holds after restart

#### Scenario: Initialize a fresh store

- **WHEN** all migrations are explicitly applied to an empty database
- **THEN** map creation requires a valid client-supplied slug and equal slugs remain allowed across games
