# Category groups

Groups belong to the Categories feature and OpenAPI tag. Base route: `/games/{gameId}/maps/{mapId}/category-groups`.

| Method | Suffix | Result                                   |
| ------ | ------ | ---------------------------------------- |
| GET    | none   | 200 complete ordered tree                |
| POST   | none   | 200 complete created group               |
| PATCH  | /{id}  | 200 complete updated group with children |
| DELETE | /{id}  | empty 204                                |

See the [Categories API](../README.md) for tree shape, placement and error conventions.

## Creation and names

POST accepts name, color, optional nullable iconAssetId and optional nullable beforeId. Names trim to 1..200 characters and are unique within their map after invariant case normalization, including Cyrillic case. Identity, mapId, timestamps, order and counts are server-owned.

An omitted/null beforeId appends; a sibling ID inserts before it. Numeric order is rejected. A new group can have no icon and has count 0 and categories [].

## Updates and display order

PATCH accepts optional name, color, iconAssetId and beforeId. Omission preserves state, iconAssetId null clears and beforeId null appends. Missing/foreign anchors return 404 and self anchors 422. Group map membership is immutable.

Placement normalizes positions atomically, retaining sibling metadata timestamps and child category metadata. Equivalent metadata and unchanged sequence preserve updatedAt and perform no save. Parent map/game timestamps remain unchanged.

## Icons and counts

The optional icon resolves canonical shared image metadata. Library deletion returns 409 while referenced. Detachment/deletion retains media bytes. markersCount is computed from current child category markers.

## Deletion and migration

Deletion removes the group and current categories/markers while retaining library files; transferred-out categories survive. Repeated deletion returns 404 and surviving order/timestamps remain unchanged.

Apply CategoryTree explicitly before serving the API. See [persistence instructions](../../../GameAtlas.Infrastructure/Persistence/README.md).

## Color

Group color follows category hex validation: #RGB or #RRGGBB, normalized to uppercase #RRGGBB. Omitted creation color defaults to #F59E0B for compatibility; explicit null and invalid values return 422. PATCH omission preserves color and null is rejected. Apply CategoryGroupColor after CategoryTree; existing groups receive #F59E0B without changing timestamps.
