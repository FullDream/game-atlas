# Markers Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Expose an ordered media union without public child identities`
- TO: `### Requirement: Attach ordered library assets without independent marker media models`

## MODIFIED Requirements

### Requirement: Create a complete marker
POST SHALL accept required categoryId, name, and position with optional description and an ordered media array of library asset IDs. Omitted media SHALL default to []. Success SHALL return the existing eight root fields with media resolved as shared image/video assets. Server-generated identity/timestamps, route ownership, and zero-style map support SHALL remain unchanged.

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

### Requirement: Attach ordered library assets without independent marker media models
Marker media SHALL reference existing library images/videos through ordered UUID arrays. Duplicate IDs and null elements SHALL return 422, malformed GUIDs or legacy media objects 400, missing assets 404, and folder/incompatible references 422. Responses SHALL reuse shared library image/video representations including public asset IDs and required type. Separate per-marker media identities/metadata SHALL NOT be exposed.

#### Scenario: Mixed media round trip
- **WHEN** media contains an image asset ID followed by a video asset ID
- **THEN** both resolve in exactly that order as shared library representations with id, type, URL, and metadata
- **AND** one asset may be reused by other markers but cannot occur twice within the same marker

#### Scenario: Reject ambiguous or unknown variants
- **WHEN** a media element is an old image/video/YouTube object, a URL, or a malformed GUID
- **THEN** HTTP 400 rejects the complete request
- **AND** null elements or duplicate IDs return 422 without partial persistence

#### Scenario: Shared metadata is resolved rather than copied
- **WHEN** attached image alt/name or a video's preview changes in the library
- **THEN** subsequent marker responses expose current shared metadata without updating marker timestamps or attachment IDs/order

### Requirement: Replace media arrays atomically through marker updates
Omitted creation media SHALL produce []; omitted PATCH media SHALL preserve attachments. A supplied asset-ID array SHALL replace the whole ordered attachment list. [] SHALL clear attachments; null SHALL return 422. Every ID SHALL be validated before any marker state is saved. Detachment SHALL NOT remove library assets. No separate marker-media update/reorder endpoint SHALL exist.

#### Scenario: Replace and clear the list
- **WHEN** PATCH supplies a valid ordered array of distinct asset IDs
- **THEN** it becomes the complete attachment list and omitted former attachments are removed
- **AND** [] clears attachments, omitted media preserves them, and all underlying assets remain in the library

#### Scenario: Reject partial element patches
- **WHEN** an array contains a partial media object or an invalid, duplicated, missing, or incompatible asset reference
- **THEN** the operation fails with the applicable 400/404/422 response
- **AND** no scalar, position, category, or attachment change is persisted

### Requirement: Apply atomic marker merge patches and preserve no-ops
PATCH SHALL retain optional categoryId, name, description, position, and media, with media now an asset-ID array. Attachment/reference checks and scalar/position changes SHALL commit atomically. Empty objects SHALL return 422 and unknown/read-only fields 400. Identity/createdAt and parent timestamps SHALL remain unchanged. Equal ordered IDs SHALL be a media no-op; effective changes SHALL update only the marker.

#### Scenario: Preserve parents and recursive position behavior
- **WHEN** a marker is created, changed, transferred, or deleted
- **THEN** parent metadata remains unchanged
- **AND** position merging, ID-array replacement, read-only-field rejection, and resource-level atomicity remain in force

#### Scenario: Combined update and effective equality
- **WHEN** valid changes combine scalar fields, recursive position members, same-map category transfer, and ordered asset IDs
- **THEN** all effective changes commit together and only marker updatedAt changes
- **AND** equal normalized fields and equal ordered asset IDs preserve marker timestamps and attachment rows

#### Scenario: Parent timestamps and read-only fields
- **WHEN** a marker is created, edited, transferred, or deleted
- **THEN** parent game, map, group, and category timestamps and metadata remain unchanged
- **AND** PATCH supplying id, gameId, createdAt, updatedAt, icon, color, or another unknown root field returns HTTP 400

### Requirement: Physically delete markers and cascade current ownership
DELETE SHALL physically remove the scoped marker and its attachment rows and return empty 204. Repeated deletion SHALL return 404. Category/group/map/game deletion SHALL cascade through current marker ownership without deleting shared assets or their files. Other markers' attachments SHALL remain unchanged. Removing a style SHALL NOT delete marker attachments.

#### Scenario: Map deletion and style deletion have distinct ownership
- **WHEN** a map is deleted
- **THEN** its marker/attachment rows disappear while sibling maps and every shared asset remain intact
- **AND** style deletion alone preserves every marker, attachment, position, and category count

#### Scenario: Delete and repeat
- **WHEN** a marker referencing shared image/video assets is deleted
- **THEN** its marker and attachment rows are absent after reopening storage and the response is empty 204
- **AND** referenced assets and another marker's references remain; repeated deletion returns 404

#### Scenario: Parent deletion after transfers
- **WHEN** a category, group, map, or game is deleted through its existing API
- **THEN** current marker and attachment rows cascade while assets remain in the library
- **AND** moved markers survive former-parent deletion, and detached assets are not automatically expired

### Requirement: Persist marker mutations safely
Marker state, category checks, and ordered asset attachments SHALL be durable and atomic. Concurrent asset deletion SHALL NOT create dangling references. Unexpected storage/library failures SHALL return safe 500 without being classified as name conflicts. Failed writes SHALL restore prior attachments and category counts, leaving every shared asset unchanged.

#### Scenario: Failure after a write but before commit
- **WHEN** creation, transfer, deletion, or attachment replacement fails after commands execute but before commit
- **THEN** prior marker fields, attachment order, and observable counts are restored
- **AND** assets/bytes remain intact and the safe 500 response reveals no internal details

### Requirement: Document marker operations and discriminated media
OpenAPI SHALL retain four map-scoped marker operations in GET/POST/PATCH/DELETE order and eight root response fields. Creation/PATCH media SHALL be UUID arrays; response media SHALL reuse the library image/video union. YouTube, arbitrary URLs, per-marker media metadata, and old media-object input schemas SHALL be absent. Existing position, category, counts, PATCH, and safe-error contracts SHALL remain.

#### Scenario: Discover scoped operations without media regression
- **WHEN** a client reads OpenAPI
- **THEN** existing marker operation IDs/routes remain, requests accept ordered UUID media arrays, and responses use shared image/video schemas
- **AND** lists stay unpaginated/unfiltered while YouTube, PUT, and individual-marker GET are absent

#### Scenario: Discover the contract
- **WHEN** a client reads OpenAPI
- **THEN** GET returns complete marker arrays, POST/PATCH return 200 DTOs, DELETE returns empty 204, and relevant 400/404/415/422/500 responses are documented
- **AND** responses expose image/video discriminator mappings with shared alt/preview metadata, while input uses IDs without type discriminators
- **AND** optional patch fields, recursive position semantics, creation requirements, and duplicate-ID validation are discoverable

## REMOVED Requirements

### Requirement: Validate image attachment metadata
**Reason**: Image metadata and alt now belong to the shared library rather than each marker.
**Migration**: Upload a validated library image and send its ID. Legacy metadata conversion is not provided.

### Requirement: Accept YouTube references by video identifier
**Reason**: The first library version supports uploaded video files only; YouTube is intentionally removed.
**Migration**: Use uploaded video assets in a fresh database; legacy YouTube migration is not provided and existing data is never discarded automatically.

### Requirement: Accept hosted video references and optional previews
**Reason**: Uploaded video bytes and image previews now belong to library assets.
**Migration**: Upload video and optional preview image assets, configure the library video preview by ID, and attach the video ID.

### Requirement: Validate media references without remote inspection
**Reason**: Marker mutations accept asset IDs rather than arbitrary file-reference strings.
**Migration**: Upload files through the library API and attach asset IDs in a fresh database. No legacy importer, remote fetching or guessed URL conversion is provided.
