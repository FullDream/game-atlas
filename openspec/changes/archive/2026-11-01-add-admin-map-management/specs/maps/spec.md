## MODIFIED Requirements

### Requirement: Validate map names within their game

Map names SHALL be trimmed, preserve display spelling, contain 1..200 characters, and be unique per game after invariant case normalization. Duplicate names SHALL return HTTP 409 with name errors, including recognized save-time conflicts. Different games SHALL allow equal names. No editable map order or selected-map field SHALL be introduced. Map names SHALL also be editable through the scoped map metadata PATCH.

#### Scenario: Name uniqueness and scope

- **WHEN** a game already has World and another creation supplies ' world '
- **THEN** HTTP 409 preserves the existing map
- **AND** World is allowed in another game, and deleting the original frees its name

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the nine map REST operations, including the name-only map metadata PATCH, explicit map scope, complete map/style representations, geometry, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap remains name/width/height JSON and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation

## ADDED Requirements

### Requirement: Patch map name without resizing

PATCH /games/{gameId}/maps/{mapId} SHALL accept optional name with shared merge-patch semantics and return 200 with the complete map. Name SHALL be required when supplied, trimmed, 1..200 characters and unique within the game excluding the current map. Dimensions and geometry SHALL remain immutable. No PUT route SHALL be introduced.

#### Scenario: Rename a scoped map

- **WHEN** a valid patch changes the name of a map belonging to the route game
- **THEN** the name and normalized name are persisted with a new updatedAt
- **AND** createdAt, game timestamps, dimensions, geometry, styles, default selection and owned content remain unchanged

#### Scenario: Reject invalid patches atomically

- **WHEN** a patch supplies null, blank or overlength name, or an empty object
- **THEN** HTTP 422 returns field errors without changes
- **WHEN** the name conflicts with another map in the game, including a recognized save-time unique constraint conflict
- **THEN** HTTP 409 returns a name error without changes
- **WHEN** a patch contains width, height, other unknown/read-only properties, incompatible values, a non-object body or malformed JSON
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
