# Categories Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Accept icon references without restricting image formats`
- TO: `### Requirement: Attach library image assets as category icons`

## MODIFIED Requirements

### Requirement: Create categories with server-owned identity and timestamps
POST SHALL accept required name, iconAssetId, and color with optional order and return the complete category. iconAssetId SHALL identify a committed library image. The server SHALL generate UUID v7, route-derived categoryGroupId, and equal UTC timestamps. Additional creation metadata SHALL NOT override server values. Category creation SHALL NOT upload files.

#### Scenario: Successful creation
- **WHEN** a valid category references an existing library image in an existing group
- **THEN** its complete representation survives reopening storage and icon resolves the shared image DTO
- **AND** id is UUID v7, categoryGroupId matches the route, and timestamps are equal UTC instants

#### Scenario: Server-owned creation fields
- **WHEN** valid creation input additionally supplies id, categoryGroupId, gameId, timestamps, or markersCount
- **THEN** those fields do not override server-derived identity, membership, timestamps, or count

### Requirement: Attach library image assets as category icons
Category icons SHALL reference library images through iconAssetId rather than arbitrary URLs. Missing/null required IDs SHALL return 422, malformed GUIDs 400, missing assets 404, and non-image assets 422. JPEG, PNG, WebP, AVIF, and accepted SVG SHALL be permitted. Attachments SHALL participate in the library deletion guard; changing or deleting a category SHALL NOT delete its asset.

#### Scenario: Icon references
- **WHEN** a category attaches a valid raster or SVG image asset from any folder or game usage
- **THEN** it resolves shared image metadata without copying bytes or requiring a URL
- **AND** video assets and folder IDs cannot be used as icons

#### Scenario: Icon bounds
- **WHEN** iconAssetId is missing/null, malformed, absent from the library, or identifies a video
- **THEN** the corresponding 422, 400, 404, or 422 response preserves all category data
- **AND** legacy icon strings are no longer accepted as category mutation input

#### Scenario: Reuse an icon and preserve it after detachment
- **WHEN** two categories share one image and one is deleted or changes its icon
- **THEN** the other category retains its reference and the asset remains in the library
- **AND** explicit asset deletion returns 409 until every category and other consumer detaches it

### Requirement: List complete categories without pagination
GET SHALL retain the complete ordered category array and its nine fields: id, categoryGroupId, name, icon, color, order, markersCount, createdAt, and updatedAt. icon SHALL be the shared image-asset representation rather than a string. Ordering SHALL remain order/createdAt/id ascending. No individual GET, pagination, search, alternate sorting, batch update, or reorder endpoint SHALL be introduced.

#### Scenario: Ordered complete collection
- **WHEN** an existing group contains categories, including duplicate orders and equal creation timestamps
- **THEN** every category is returned exactly once with all nine fields, ordered by order, then createdAt, then id ascending
- **AND** categories of other groups are absent

#### Scenario: Empty group
- **WHEN** an existing group has no categories
- **THEN** its list response is HTTP 200 with `[]`
- **AND** a missing or foreign group returns HTTP 404 instead

#### Scenario: Resolve current shared icon metadata
- **WHEN** an icon asset is renamed, moved, or given different alt text through the library
- **THEN** subsequent category responses include that current shared image metadata without changing category timestamps
- **AND** collection queries load icons without one query per category

### Requirement: Update category state atomically through merge patch
PATCH SHALL accept optional name, iconAssetId, color, order, and categoryGroupId and return the nine-field category with resolved icon. Omission SHALL preserve fields; null required mutable values and empty objects SHALL return 422. Asset checks, parent transfers, metadata, and attachment updates SHALL commit atomically. Identity/createdAt and parent timestamps SHALL remain unchanged; only effective changes update the category.

#### Scenario: Preserve parent state and no-ops
- **WHEN** a valid patch changes category metadata or transfers within its map
- **THEN** only effective category state and its own updatedAt change
- **AND** normalized no-ops preserve its timestamp without affecting either parent map or game

#### Scenario: Single or combined changes
- **WHEN** a client supplies one or more valid mutable values
- **THEN** they are applied together, with omitted values preserved and only the target category's updatedAt changed
- **AND** game, map, group, and other category metadata and timestamps remain unchanged

#### Scenario: No effective change
- **WHEN** supplied values equal current state after name trimming/color normalization, including the same iconAssetId and group
- **THEN** HTTP 200 returns the current representation without changing category updatedAt or rewriting its attachment

#### Scenario: Atomic rejection
- **WHEN** a patch is empty or contains an invalid value alongside otherwise valid changes
- **THEN** the response is the relevant validation failure and no field or membership changes

#### Scenario: Reject a failed icon replacement atomically
- **WHEN** a valid name or group change accompanies a missing or incompatible iconAssetId
- **THEN** the complete patch fails and prior ownership, icon reference, metadata, and timestamps remain unchanged

### Requirement: Document the category API and preserve existing contracts
OpenAPI SHALL retain the four map-scoped category operations, GUID parents, counts, ordering, colors, and PATCH errors. Mutations SHALL expose required/optional iconAssetId as appropriate, and the nine-field response SHALL expose icon as the shared image schema. Arbitrary icon URLs and old icon mutation properties SHALL NOT be advertised. No redundant mapId/gameId response fields SHALL be added.

#### Scenario: Discover map-scoped categories
- **WHEN** a client reads OpenAPI
- **THEN** routes require gameId/mapId/groupId and categoryGroupId remains category ownership
- **AND** iconAssetId selects a shared library image while markersCount stays read-only

#### Scenario: Discover all four operations
- **WHEN** a client reads OpenAPI
- **THEN** POST/PATCH document 200 category responses, GET a plain array, DELETE empty 204, and relevant ProblemDetails
- **AND** name/color/order constraints, GUID iconAssetId, shared image icon metadata, and optional PATCH presence are documented
- **AND** individual GET, PUT, batch, pagination, and reorder remain absent
