# Categories API

Categories belong to one category group. All operations are unauthenticated and work for draft and published games.
The base route is `/games/{gameId}/category-groups/{groupId}/categories`.

| Method | Route | Success |
| --- | --- | --- |
| POST | Base route | 200, complete category |
| GET | Base route | 200, complete ordered array |
| PATCH | Base route + `/{id}` | 200, complete category |
| DELETE | Base route + `/{id}` | 204, no body |

There is no individual-category GET, PUT, batch or reorder operation, pagination, search, or alternate sorting.
Missing games, groups, categories, and resources outside the route ownership return 404 ProblemDetails.
An existing group without categories returns `[]`.

## Representation

```json
{
  "id": "019a0d00-0000-7000-8000-000000000001",
  "categoryGroupId": "019a0d00-0000-7000-8000-000000000002",
  "name": "Chests",
  "icon": "/icons/chest.svg",
  "color": "#FF8800",
  "order": 1,
  "markersCount": 0,
  "createdAt": "2026-10-23T12:00:00+00:00",
  "updatedAt": "2026-10-23T12:00:00+00:00"
}
```

The server generates UUID v7 and equal UTC timestamps at creation. `markersCount` is read-only and always zero
until marker functionality is introduced; no counter is stored. Internal normalized names and a redundant game ID
are not exposed. The route determines membership at creation; extra creation properties do not override server values.

## Creation and validation

POST accepts required `name`, `icon`, and `color`, plus optional `order`.

- Name: 1 through 200 characters after trimming. Display case is preserved. Names are unique within their group
  after invariant case normalization, including Latin and Cyrillic. Equal names in different groups are allowed.
- Icon: 1 through 2048 characters after trimming. It is an opaque reference: full links and relative paths are accepted,
  including SVG, PNG, WebP, and extensionless links. The API does not fetch images or validate URLs or image formats.
- Color: `#RGB` or `#RRGGBB`, accepting either ASCII hexadecimal letter case. Short colors expand to uppercase
  `#RRGGBB`, for example `#fff` becomes `#FFFFFF`. No surrounding whitespace, named colors, or alpha channel is accepted.
- Order: a positive 32-bit integer. Omission appends after the group's maximum, starting at 1. Duplicate explicit
  orders are allowed. If the maximum is 2147483647, automatic append returns 422; supply an explicit positive order.

Validation failures return 422 ProblemDetails; name conflicts return 409 with `name` errors, including recognized
save-time uniqueness failures. Fractional, string, or out-of-range integer orders and incompatible JSON field types return 400.

## Updates and transfers

PATCH accepts `application/merge-patch+json` and `application/json`, including charset parameters, with identical
JSON Merge Patch semantics. All five mutable fields (`name`, `icon`, `color`, `order`, `categoryGroupId`) are optional:
omission preserves the existing value; explicit null returns 422. An empty object returns 422. Unknown/read-only fields,
malformed JSON, non-object bodies, incompatible types, and malformed GUIDs return 400. Other PATCH media types,
including `application/json-patch+json` and `text/plain`, return 415.

For transfer, use the **current group** in the route and send the destination `categoryGroupId` in the body.
The destination must belong to the same game; a missing, all-zero, or other-game group ID returns 404.
The resulting name must be unique in that destination. A combined rename and transfer is permitted and validated
using the new name. Omitted order is preserved on transfer, even if another destination category has the same order.
Use the new group route after success; PATCH and DELETE under the old route return 404.

Updates and transfers are atomic. Invalid values, conflicts, and save failures do not persist partial changes.
Only an effective change updates the target category's `updatedAt`; normalized no-ops preserve it without an update write.
Identity and creation time never change. Other categories and parent game/group metadata and timestamps remain unchanged.

## Listing and deletion

GET returns every category of the route group ordered by `order`, then `createdAt`, then `id`, all ascending.
Changing or deleting one category does not shift or renumber others; order gaps and duplicates are intentional.

DELETE physically removes a category and releases its name. Repeated deletion returns 404. Deleting a group cascades
to its current categories; deleting a game cascades through its groups. A transferred category survives deletion
of its former group. Unrelated game/group categories are preserved.

Unexpected library and storage exceptions return safe 500 ProblemDetails without internal details. Only precisely
recognized category name uniqueness failures become 409. Apply the Infrastructure migration explicitly before use;
see [SQLite migrations](../../GameAtlas.Infrastructure/Persistence/README.md). Runnable requests are in
[GameAtlas.Api.http](../GameAtlas.Api.http).
