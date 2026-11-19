# Spec Delta

## MODIFIED Requirements

### Requirement: Create a complete marker

POST SHALL accept required categoryId, name, and position with optional status (`draft` or `published`, defaulting to `draft` when omitted), optional description and an ordered media array of library asset IDs. Omitted media SHALL default to []. Null status SHALL return HTTP 422. Success SHALL return the nine root fields (including string `status`) with media resolved as shared image/video assets. Server-generated identity/timestamps, route ownership, and zero-style map support SHALL remain unchanged.

#### Scenario: Create on a blank coordinate space

- **WHEN** a valid marker is created in a category of a map with styles=[]
- **THEN** it persists with unchanged position/media semantics, status=`draft` when omitted, description=null and media=[] when omitted
- **AND** adding or deleting styles later does not alter the marker

#### Scenario: Successful creation and defaults

- **WHEN** a valid marker is created in a category of the route map without status/description/media
- **THEN** all nine fields return, status=`draft`, description=null, media=[], UUID v7, and equal UTC timestamps
- **AND** when status=`published` is explicitly supplied, the marker persists and returns status=`published`
- **AND** the marker survives reopening storage even with styles=[]

#### Scenario: Server-owned creation fields

- **WHEN** otherwise valid creation input also supplies id, gameId, createdAt, or updatedAt
- **THEN** those values do not override the server-generated identity, ownership, or timestamps

### Requirement: Apply atomic marker merge patches and preserve no-ops

PATCH SHALL accept optional categoryId, name, description, status (`draft` or `published`), position, and media, with media as an asset-ID array. Null status SHALL return HTTP 422. Updating a marker's status SHALL change only that marker without modifying its category or group. Attachment/reference checks and scalar/position changes SHALL commit atomically. Empty objects SHALL return 422 and unknown/read-only fields 400. Identity/createdAt and parent timestamps SHALL remain unchanged. Equal ordered IDs SHALL be a media no-op; effective changes SHALL update only the marker.

#### Scenario: Preserve parents and recursive position behavior

- **WHEN** a marker is created, changed, transferred, or deleted
- **THEN** parent metadata remains unchanged
- **AND** position merging, ID-array replacement, status updates, read-only-field rejection, and resource-level atomicity remain in force

#### Scenario: Combined update and effective equality

- **WHEN** valid changes combine scalar fields (including status), recursive position members, same-map category transfer, and ordered asset IDs
- **THEN** all effective changes commit together and only marker updatedAt changes
- **AND** equal normalized fields (including unchanged status) and equal ordered asset IDs preserve marker timestamps and attachment rows

#### Scenario: Parent timestamps and read-only fields

- **WHEN** a marker is created, edited, transferred, or deleted
- **THEN** parent game, map, group, and category timestamps and metadata remain unchanged
- **AND** PATCH supplying id, gameId, createdAt, updatedAt, icon, color, or another unknown root field returns HTTP 400

### Requirement: Document marker operations and discriminated media

OpenAPI SHALL retain four map-scoped marker operations in GET/POST/PATCH/DELETE order and nine root response fields (including `status`). Creation and PATCH schemas SHALL expose optional `status` (`draft` | `published`). Creation/PATCH media SHALL be UUID arrays; response media SHALL reuse the library image/video union. YouTube, arbitrary URLs, per-marker media metadata, and old media-object input schemas SHALL be absent. Existing position, category, counts, PATCH, and safe-error contracts SHALL remain.

#### Scenario: Discover scoped operations without media regression

- **WHEN** a client reads OpenAPI
- **THEN** existing marker operation IDs/routes remain, requests accept optional `status` and ordered UUID media arrays, and responses include `status` and shared image/video schemas
- **AND** lists stay unpaginated/unfiltered while YouTube, PUT, and individual-marker GET are absent

#### Scenario: Discover the contract

- **WHEN** a client reads OpenAPI
- **THEN** GET returns complete marker arrays, POST/PATCH return 200 DTOs with nine root fields, DELETE returns empty 204, and relevant 400/404/415/422/500 responses are documented
- **AND** responses expose image/video discriminator mappings with shared alt/preview metadata, while input uses IDs without type discriminators
- **AND** optional patch fields, recursive position semantics, creation requirements, and duplicate-ID validation are discoverable
