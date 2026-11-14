# Spec Delta

## MODIFIED Requirements

### Requirement: Create categories with server-owned identity and timestamps

POST SHALL accept required name and color with optional status (`draft` or `published`, defaulting to `draft` when omitted), optional nullable iconAssetId and optional nullable beforeId. It SHALL append when beforeId is omitted or null, or insert before a category in the route group. The server SHALL generate UUID v7, route-derived categoryGroupId, order and equal UTC timestamps. Null status SHALL return HTTP 422. Additional creation metadata SHALL NOT override server values. Category creation SHALL NOT upload files.

#### Scenario: Successful creation

- **WHEN** a valid category is created with or without a library image in an existing group
- **THEN** its complete representation survives reopening storage, status defaults to `draft` when omitted or matches the supplied `draft`/`published` value, and icon is a shared image DTO or null
- **AND** id is UUID v7, categoryGroupId matches the route, and timestamps are equal UTC instants

#### Scenario: Server-owned creation fields

- **WHEN** valid creation input additionally supplies id, categoryGroupId, gameId, timestamps, or markersCount
- **THEN** those fields do not override server-derived identity, membership, timestamps, or count
- **AND** numeric order is not an accepted creation field and returns 400

### Requirement: Update category state atomically through merge patch

PATCH SHALL accept optional name, iconAssetId, color, status (`draft` or `published`), categoryGroupId and beforeId and return the complete ten-field category with nullable resolved icon and string status. Omission preserves metadata; null icon detaches it. Null required values (including null status) and empty objects return 422. Updating a category's status SHALL change only that category without cascading to its owned markers or parent group. Metadata, placement, transfer and icon checks SHALL commit atomically. Identity/createdAt and parent timestamps SHALL remain unchanged; only effective changes update category metadata timestamps.

#### Scenario: Preserve parent state and no-ops

- **WHEN** a valid patch changes metadata or transfers within its map
- **THEN** effective category changes and necessary sibling order changes persist together
- **AND** map/game/group metadata timestamps do not change solely for category placement

#### Scenario: Single or combined changes

- **WHEN** one or more valid metadata (including status), destination and placement values are supplied
- **THEN** they apply together and omitted metadata remains unchanged
- **AND** only the moved/edited category updates its metadata timestamp; owned markers retain their existing statuses and timestamps, and siblings changing only normalized order retain theirs

#### Scenario: No effective change

- **WHEN** normalized metadata (including status), membership and final sibling sequence are unchanged
- **THEN** HTTP 200 returns the current category without timestamp or attachment changes

#### Scenario: Atomic rejection

- **WHEN** the patch is empty or contains an invalid value (such as null status) alongside valid changes
- **THEN** the appropriate error is returned and no field, position or membership changes

#### Scenario: Reject a failed icon replacement atomically

- **WHEN** a valid rename or transfer accompanies a missing or incompatible non-null iconAssetId
- **THEN** the complete patch fails and prior ownership, reference, order and timestamps remain unchanged

### Requirement: Document the category API and preserve existing contracts

OpenAPI SHALL expose CreateCategory, UpdateCategory and DeleteCategory under the Categories tag on existing map/group-scoped mutation routes. It SHALL remove ListCategories. Categories SHALL be read through ListCategoryGroups as nested children. Schemas SHALL describe required response `status` (`draft` | `published`), optional request `status` on create and PATCH, nullable shared image icons, optional iconAssetId and beforeId, read-only order/counts, required color and shared PATCH errors without redundant mapId/gameId response fields.

#### Scenario: Discover map-scoped categories

- **WHEN** a client reads OpenAPI
- **THEN** mutations require gameId/mapId/groupId, categoryGroupId remains category ownership, and `status` is exposed on create, PATCH, and Category responses
- **AND** nullable iconAssetId selects or detaches an image while markersCount and order are read-only

#### Scenario: Discover all four operations

- **WHEN** a client reads OpenAPI
- **THEN** POST/PATCH document 200 category responses, DELETE empty 204 and relevant ProblemDetails
- **AND** per-group collection GET, individual GET, PUT, batch and dedicated reorder routes remain absent
