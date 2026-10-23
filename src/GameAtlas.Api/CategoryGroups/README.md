# Category groups

Groups belong to a game and are available without authentication for draft and published games. All routes start at `/games/{gameId}/category-groups`.

| Method | Suffix | Success |
| --- | --- | --- |
| POST | none | 200 with the created group |
| GET | none | 200 with all groups ordered by `order`, `createdAt`, then `id` |
| PATCH | `/{id}` | 200 with the updated group |
| DELETE | `/{id}` | 204 without a body |

List returns a complete JSON array, including `[]` for an existing game without groups. There is no individual-group GET, dedicated reorder, batch, pagination, search, or alternate sorting endpoint. DTO fields are `id`, `gameId`, `name`, `order`, `createdAt`, and `updatedAt`. Identity is server-generated UUID v7; ownership comes from the route; timestamps are UTC. Group operations do not change the parent game's updatedAt.

## Creation and names

POST accepts name and optional order, for example `{ "name": "Resources", "order": 10 }`. Name is trimmed, has 1–200 characters, and is unique in its game without regard to Latin or Cyrillic letter case. Display spelling is preserved; an internal normalized key enforces uniqueness and is never exposed. Different games may use the same name. Duplicate names return 409 ProblemDetails with a name error, including conflicts detected during saving. Additional server-owned creation properties are ignored as in game creation.

Order is a positive 32-bit integer. If omitted on creation, the group gets `max(order) + 1`, starting at 1. Default append allocation uses a short transaction. If the maximum is already int.MaxValue, default append returns 422: supply an explicit positive order instead. Null, zero, and negative values return 422; non-integers and values outside the integer range return 400.

## Updates and display order

PATCH `/{id}` accepts optional name and order, separately or together:

```json
{
  "name": "Resources",
  "order": 10
}
```

Omitted fields retain their values. An empty object, null name/order, invalid name, or nonpositive order returns 422. Server-owned or unknown fields return 400. Both changes are validated before either is saved: a failure persists neither. Success returns the complete DTO. A no-op after name trimming preserves updatedAt and performs no write; a real change updates it once. A case-only display name change is a real change.

Repeated order values are allowed. List sorts ascending by order, then createdAt, then id. Updating a group's order changes only that group; others retain their numeric orders and timestamps. There is no automatic shifting or renumbering. Exact insertion between adjacent integer keys may require several independent client PATCH requests, which are not an atomic batch. Concurrent edits have no ETag/version check.

PATCH accepts application/merge-patch+json and application/json with identical JSON Merge Patch semantics, including charset parameters. Unsupported media types return 415; malformed JSON, non-object bodies, incompatible types, and invalid GUID representations return 400. PUT is not exposed.

## Deletion and errors

Group DELETE physically removes the scoped group, releases its name, and preserves survivor positions/timestamps. Deleting the owning game cascades to all its groups in the database. Neither operation offers restoration. Categories and markers are not part of this feature yet.

A missing game returns 404, including on collection GET. Missing/foreign groups and repeated deletion return 404. Errors use application/problem+json; unexpected storage failures return safe 500 responses without exception details or SQL and do not persist partial group updates.

Apply the CategoryGroups migration explicitly before serving the API, as documented in Infrastructure/Persistence/README.md. CRUD examples are in GameAtlas.Api.http. OpenAPI operation IDs are CreateCategoryGroup, ListCategoryGroups, UpdateCategoryGroup, and DeleteCategoryGroup.
