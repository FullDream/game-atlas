# Markers API

Markers belong to one category in one map. The four unauthenticated operations work for draft and published games,
including maps without styles. A map and category must exist before marker creation.

| Method | Route | Success |
| --- | --- | --- |
| GET | /games/{gameId}/maps/{mapId}/markers | 200, complete array |
| POST | /games/{gameId}/maps/{mapId}/markers | 200, complete marker |
| PATCH | /games/{gameId}/maps/{mapId}/markers/{id} | 200, complete marker |
| DELETE | /games/{gameId}/maps/{mapId}/markers/{id} | 204, no body |

There is no individual-marker GET, PUT, pagination, search, or server-side category filter. Lists contain all markers
of the route map, ordered by createdAt then id ascending. An existing empty map returns []; a missing or foreign game/map returns 404.

## Creation and representation

POST accepts categoryId, name, and position, plus optional description and media:

```json
{
  "categoryId": "019a0d00-0000-7000-8000-000000000001",
  "name": "Chest",
  "description": "Behind the waterfall",
  "position": { "x": -120.5, "y": 200 },
  "media": [
    "019a0000-0000-7000-8000-000000000112",
    "019a0000-0000-7000-8000-000000000113"
  ]
}
```

The response has exactly id, categoryId, name, description, position, media, createdAt, and updatedAt.
The server generates UUID v7 and equal UTC creation/update timestamps. Supplied server-owned creation properties
cannot override these values. Id and createdAt are immutable. Duplicate marker names are allowed.

- Name is trimmed and contains 1..200 characters.
- Description is plain text, trimmed and limited to 5000 characters. Omission at creation, null, empty, or whitespace
  becomes null. It is not interpreted as HTML.
- Position requires both finite double coordinates at creation. Zero, negative, and fractional values are valid;
  these are X/Y coordinates without geographic restrictions.
- Category must exist in the route map. Missing, all-zero, or foreign category IDs return 404; missing/null category
  input returns 422. Malformed GUIDs return 400.
- Omitted media defaults to []. Array order is preserved.

## Shared media attachments

Mutation `media` is an ordered array of distinct committed library asset UUIDs. Upload files through
`POST /media/assets` before attaching them. Omission at creation means []; arrays are replaced as a whole.
Images (including accepted SVG) and uploaded MP4/WebM videos can be reused across markers, maps and games.
YouTube and legacy URL/object inputs are no longer supported.

Responses resolve the shared image/video DTO union with required `type` (`image` or `video`). They include asset IDs,
names, URLs and current library metadata. Images include shared alt and nullable persisted placeholders;
video preview is a shared image DTO or null. There are no marker-owned copies of these fields.
Update alt, names or video previews through the library; subsequent marker reads see those changes without changing
marker timestamps. Reordering the same IDs changes attachment order while retaining existing relationships.

Null arrays, null elements, empty GUIDs and duplicate IDs return 422; malformed GUIDs and legacy object items return
400. Missing assets return 404; folder IDs return 422. Attachment validation is atomic with scalar and position changes.
Asset deletion returns 409 while any marker or other consumer uses it. Removing an attachment does not delete the asset.

## Atomic updates

PATCH accepts application/merge-patch+json and application/json, with charset parameters and identical RFC 7396
semantics. Other types, including application/json-patch+json and text/plain, return 415. Bodies must be JSON objects.

Every mutable root field is optional. Omission preserves its value. Description may be cleared with null;
categoryId, name, position, individual supplied coordinates, and media cannot be null (422).
Unknown/read-only PATCH members and unknown position members return 400. Root {} returns 422.

Objects merge recursively, while arrays replace as a whole:

```json
{ "position": { "x": 200 } }
```

This preserves the stored y. An empty position object is a valid no-op. To reorder or edit media, send the complete
desired array, with complete subtype fields for every element. [] clears all media. Omitted media leaves them intact.

```json
{
  "categoryId": "019a0d00-0000-7000-8000-000000000002",
  "description": null,
  "media": []
}
```

Transfers are limited to categories in the same map. Scalar, position, category, and media changes commit in one
transaction. Failed updates persist nothing. Equal normalized values and ordered media content preserve updatedAt
and existing media IDs without a write. Creation time and parent game/map/group/category timestamps do not change.

## Deletion and counts

DELETE physically removes the marker and all its attachment rows, returning empty 204; missing/repeated/foreign marker
deletion returns 404 ProblemDetails. Category, group, map, and game deletion cascade through their current markers and
media. Moved markers and transferred categories survive deletion of former parents; referenced files remain untouched.

Category markersCount is computed from committed marker rows in every category response. Creation, deletion, and
same-map transfers change the next count, without storing counters or touching category timestamps.
All expected errors use ProblemDetails. Unexpected storage failures are logged centrally and return safe 500 responses.

Apply the current media-library migrations explicitly before serving these endpoints; see the Infrastructure persistence README.
