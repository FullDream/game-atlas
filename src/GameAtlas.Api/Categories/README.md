# Categories API

Groups and categories share the Categories OpenAPI tag and functional folders in each backend layer. They remain separate entities and tables. Groups belong to maps, categories to groups, and markers reference categories. Operations remain unauthenticated for draft/published games and work without styles.

## Tree retrieval

GET `/games/{gameId}/maps/{mapId}/category-groups` (ListCategoryGroups) returns the complete ordered tree as a plain array. Each group contains id, mapId, name, color, createdAt, updatedAt, nullable icon, markersCount and categories. Array positions define display order for groups and their nested categories; numeric order is not exposed. Empty maps return []; empty groups contain categories [] and count 0.

Each category contains id, categoryGroupId, name, nullable icon, color, order, markersCount, createdAt and updatedAt. Icons resolve canonical ImageAsset metadata. Counts derive from current persisted markers; group counts sum their categories. Retrieval uses a bounded number of reads rather than one per node.

The per-group category collection GET and ListCategories are removed. GET on the retained POST path returns 405. No individual GET, PUT, pagination, batch or dedicated reorder route is exposed.

## Category mutations

Base route: `/games/{gameId}/maps/{mapId}/category-groups/{groupId}/categories`.

| Method | Suffix | Result                |
| ------ | ------ | --------------------- |
| POST   | none   | 200 complete category |
| PATCH  | /{id}  | 200 complete category |
| DELETE | /{id}  | empty 204             |

POST requires name and color, with optional nullable iconAssetId and beforeId. Names trim to 1..200 characters and are unique within the resulting group after invariant case normalization. Colors accept #RGB/#RRGGBB and normalize to uppercase #RRGGBB. UUID v7 and UTC timestamps are server-generated.

PATCH accepts optional name, iconAssetId, color, categoryGroupId and beforeId. Omission preserves metadata; iconAssetId null detaches the icon. Name/color/group cannot be null. Creation without an icon is valid.

## Placement and transfer

Numeric order is server-owned and rejected in input with 400. beforeId inserts before a destination sibling; null appends. Creation and cross-group transfer without beforeId append; same-parent metadata updates without beforeId preserve position.

The route identifies the current group; categoryGroupId selects another group of the same map. Identity, createdAt and marker references survive transfer. Later requests must use the new group route.

Placement, icon checks, resulting-name uniqueness and metadata commit atomically. Affected sibling sets receive normalized positive positions without changing sibling metadata timestamps. Deletion preserves surviving positions/timestamps. Effective changes update the target timestamp; no-ops perform no write.

Missing/foreign anchors return 404, self anchors 422, malformed GUIDs 400 and name conflicts 409.

## Icons and deletion

Icons reference committed raster or admitted SVG library images. Missing assets return 404 and incompatible assets 422. Attached group/category icons guard library deletion. Clearing references or deleting content retains library files.

DELETE physically removes current markers and marker-media relationships. Group/map/game deletion cascades by current ownership. Transferred-out categories survive deletion of their former group.

## PATCH contract and errors

application/json and application/merge-patch+json are accepted, including charset parameters. Empty patches and null required fields return 422. Unknown/read-only fields, malformed JSON and incompatible types return 400. Unsupported PATCH types return 415. Errors use ProblemDetails; unexpected storage failures remain safe 500 responses.

See [group operations](Groups/README.md), [API conventions](../../../docs/api-conventions.md) and [migration instructions](../../GameAtlas.Infrastructure/Persistence/README.md).
