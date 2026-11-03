# Spec Delta

## MODIFIED Requirements

### Requirement: Create categories with server-owned identity and timestamps

POST SHALL accept required name and color with optional nullable iconAssetId and optional nullable beforeId. It SHALL append when beforeId is omitted or null, or insert before a category in the route group. The server SHALL generate UUID v7, route-derived categoryGroupId, order and equal UTC timestamps. Additional creation metadata SHALL NOT override server values. Category creation SHALL NOT upload files.

#### Scenario: Successful creation

- **WHEN** a valid category is created with or without a library image in an existing group
- **THEN** its complete representation survives reopening storage and icon is a shared image DTO or null
- **AND** id is UUID v7, categoryGroupId matches the route, and timestamps are equal UTC instants

#### Scenario: Server-owned creation fields

- **WHEN** valid creation input additionally supplies id, categoryGroupId, gameId, timestamps, or markersCount
- **THEN** those fields do not override server-derived identity, membership, timestamps, or count
- **AND** numeric order is not an accepted creation field and returns 400

### Requirement: Attach library image assets as category icons

Category icons SHALL optionally reference committed library images through iconAssetId rather than URLs. Omitted/null creation icons SHALL produce icon null. PATCH omission SHALL preserve the icon; null SHALL detach it. Malformed GUIDs return 400, missing assets 404, and non-images 422. JPEG, PNG, WebP, AVIF and accepted SVG SHALL be permitted. Attached icons SHALL guard asset deletion; detachment or category deletion SHALL NOT delete assets.

#### Scenario: Icon references

- **WHEN** a category attaches a valid raster or SVG image from any folder or game usage
- **THEN** it resolves shared image metadata without copying bytes or requiring a URL
- **AND** video assets and folder IDs cannot be used as icons

#### Scenario: Icon bounds

- **WHEN** iconAssetId is malformed, absent from the library, or identifies a video
- **THEN** the corresponding 400, 404, or 422 response preserves all category data
- **AND** legacy icon strings are not accepted as category mutation input

#### Scenario: Reuse an icon and preserve it after detachment

- **WHEN** two categories share an image and one is deleted, clears or changes its icon
- **THEN** the other category retains its reference and the asset remains in the library
- **AND** asset deletion returns 409 until every category, group and other consumer detaches it

#### Scenario: Create without an icon and clear an existing icon

- **WHEN** creation omits iconAssetId or supplies null
- **THEN** creation succeeds with icon null
- **WHEN** PATCH supplies iconAssetId null
- **THEN** the reference is cleared atomically and subsequent responses contain icon null

### Requirement: Support positive display order within each group

Category order SHALL be server-owned and positive. Placement SHALL use beforeId relative to a destination sibling, with null meaning append. The server SHALL preserve relative sibling order and normalize affected sibling positions atomically. Numeric order inputs SHALL return 400. A self-anchor SHALL return 422; a missing or foreign destination anchor SHALL return 404. Creation and placement SHALL serialize against competing structural writes.

#### Scenario: Default append and concurrency

- **WHEN** beforeId is omitted at creation
- **THEN** the category appends to its group
- **AND** concurrent successful appends persist distinct ordered positions without losing categories

#### Scenario: Explicit duplicates and overflow

- **WHEN** a category is placed before a valid sibling
- **THEN** it appears immediately before that sibling and all other sibling relationships are preserved
- **AND** supplying numeric order returns 400 without changes

#### Scenario: Invalid order values

- **WHEN** beforeId is self-referential, malformed, missing, or in another destination group
- **THEN** the response is respectively 422, 400, 404, or 404 without changes

#### Scenario: Existing duplicate orders

- **WHEN** legacy sibling rows share orders or contain gaps
- **THEN** migration preserves their order/createdAt/id ascending sequence while normalizing positions
- **AND** no category identity, icon, marker ownership, or creation timestamp changes

### Requirement: Update category state atomically through merge patch

PATCH SHALL accept optional name, iconAssetId, color, categoryGroupId and beforeId and return the complete nine-field category with nullable resolved icon. Omission preserves metadata; null icon detaches it. Null required values and empty objects return 422. Metadata, placement, transfer and icon checks SHALL commit atomically. Identity/createdAt and parent timestamps SHALL remain unchanged; only effective changes update category metadata timestamps.

#### Scenario: Preserve parent state and no-ops

- **WHEN** a valid patch changes metadata or transfers within its map
- **THEN** effective category changes and necessary sibling order changes persist together
- **AND** map/game/group metadata timestamps do not change solely for category placement

#### Scenario: Single or combined changes

- **WHEN** one or more valid metadata, destination and placement values are supplied
- **THEN** they apply together and omitted metadata remains unchanged
- **AND** only the moved/edited category updates its metadata timestamp; siblings changing only normalized order retain theirs

#### Scenario: No effective change

- **WHEN** normalized metadata, membership and final sibling sequence are unchanged
- **THEN** HTTP 200 returns the current category without timestamp or attachment changes

#### Scenario: Atomic rejection

- **WHEN** the patch is empty or contains an invalid value alongside valid changes
- **THEN** the appropriate error is returned and no field, position or membership changes

#### Scenario: Reject a failed icon replacement atomically

- **WHEN** a valid rename or transfer accompanies a missing or incompatible non-null iconAssetId
- **THEN** the complete patch fails and prior ownership, reference, order and timestamps remain unchanged

### Requirement: Transfer categories only between groups of the same map

PATCH SHALL identify the current group in the route and the destination through categoryGroupId. Destination and anchor SHALL belong to the route map and destination group respectively. Missing/foreign destinations return 404. Resulting name uniqueness SHALL be checked. A transfer with omitted beforeId SHALL append; metadata-only updates with omitted beforeId SHALL preserve position. Identity, createdAt and marker ownership SHALL be preserved.

#### Scenario: Transfer within one map

- **WHEN** a category moves to another group of the same map
- **THEN** it appears only in the destination and retains markers, ID and createdAt
- **AND** it appends unless an explicit destination beforeId places it elsewhere

#### Scenario: Refuse a sibling map destination

- **WHEN** categoryGroupId identifies a group on another map of the same game
- **THEN** HTTP 404 preserves membership, markers, counts, coordinates, media and timestamps

#### Scenario: Successful transfer and subsequent routes

- **WHEN** a valid patch transfers a category
- **THEN** HTTP 200 returns its new categoryGroupId and the tree contains it exactly once
- **AND** later PATCH/DELETE under the old group returns 404 while the new group route addresses it

#### Scenario: Transfer conflict and combined rename

- **WHEN** the resulting name conflicts with a destination category
- **THEN** HTTP 409 preserves all fields and membership
- **AND** a combined transfer and unique rename succeeds atomically

#### Scenario: Missing or other-game destination

- **WHEN** categoryGroupId identifies a missing group or a group outside the route map
- **THEN** HTTP 404 leaves source and destination unchanged

### Requirement: Document the category API and preserve existing contracts

OpenAPI SHALL expose CreateCategory, UpdateCategory and DeleteCategory under the Categories tag on existing map/group-scoped mutation routes. It SHALL remove ListCategories. Categories SHALL be read through ListCategoryGroups as nested children. Schemas SHALL describe nullable shared image icons, optional iconAssetId and beforeId, read-only order/counts, required color and shared PATCH errors without redundant mapId/gameId response fields.

#### Scenario: Discover map-scoped categories

- **WHEN** a client reads OpenAPI
- **THEN** mutations require gameId/mapId/groupId and categoryGroupId remains category ownership
- **AND** nullable iconAssetId selects or detaches an image while markersCount and order are read-only

#### Scenario: Discover all four operations

- **WHEN** a client reads OpenAPI
- **THEN** POST/PATCH document 200 category responses, DELETE empty 204 and relevant ProblemDetails
- **AND** per-group collection GET, individual GET, PUT, batch and dedicated reorder routes remain absent

## REMOVED Requirements

### Requirement: List complete categories without pagination

**Reason**: Categories are returned in the map-wide group tree, avoiding a request for each group.
**Migration**: Use GET /games/{gameId}/maps/{mapId}/category-groups and read each group's categories array. The old category collection GET SHALL return 405 because its POST route remains, and ListCategories SHALL be absent from OpenAPI.
