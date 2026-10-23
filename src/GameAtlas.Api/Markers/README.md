# Markers API

Markers belong to one category of a game. The four unauthenticated operations work for draft and published games,
including games without a mapId.

| Method | Route | Success |
| --- | --- | --- |
| GET | /games/{gameId}/markers | 200, complete array |
| POST | /games/{gameId}/markers | 200, complete marker |
| PATCH | /games/{gameId}/markers/{id} | 200, complete marker |
| DELETE | /games/{gameId}/markers/{id} | 204, no body |

There is no individual-marker GET, PUT, pagination, search, or server-side category filter. Lists contain all markers
of the route game, ordered by createdAt then id ascending. An existing empty game returns []; a missing game returns 404.

## Creation and representation

POST accepts categoryId, name, and position, plus optional description and media:

```json
{
  "categoryId": "019a0d00-0000-7000-8000-000000000001",
  "name": "Chest",
  "description": "Behind the waterfall",
  "position": { "x": -120.5, "y": 200 },
  "media": [
    { "type": "image", "url": "/images/chest.webp", "width": 640, "height": 480, "alt": "Chest entrance" },
    { "type": "youtubeVideo", "videoId": "Ab_cd-Ef012" },
    { "type": "video", "url": "/videos/chest.mp4", "preview": "/images/preview.jpg" }
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
- Category must exist in the route game. Missing, all-zero, or foreign category IDs return 404; missing/null category
  input returns 422. Malformed GUIDs return 400.
- Omitted media defaults to []. Array order is preserved.

## Media union

The exact, case-sensitive discriminator values are image, youtubeVideo, and video. Type may appear anywhere
inside an object. OpenAPI documents anyOf with all three discriminator mappings for requests and responses.

Images require url and positive 32-bit integer width/height. Alt defaults to the empty string when omitted,
is preserved as supplied, and cannot be null. YouTube videos require an 11-character videoId containing only ASCII
letters, digits, underscores, or hyphens; full YouTube URLs are not accepted. Videos require url and may include
preview. Omitted/null/blank preview becomes null; other preview references are trimmed.

Url and nonblank preview references are trimmed, contain 1..2048 characters, and may be full links or relative paths
with any extension. There are no uploads, remote requests, format checks, external availability checks, or transcoding.
The API never deletes referenced files.

Media have internal entity IDs for persistence only. Neither requests nor responses expose those IDs, and there
are no independent media endpoints, names, or captions. Use image alt for alternative text.
Missing/unknown type, properties from another subtype, internal IDs, and unknown media members return 400.
Missing/null required subtype values, invalid dimensions, and null array elements return indexed validation errors
with 422. Invalid JSON types, fractional/overflowing integer dimensions, and malformed JSON return 400.

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

Transfers are limited to categories in the same game. Scalar, position, category, and media changes commit in one
transaction. Failed updates persist nothing. Equal normalized values and ordered media content preserve updatedAt
and existing media IDs without a write. Creation time and parent game/group/category timestamps do not change.

## Deletion and counts

DELETE physically removes the marker and all its media rows, returning empty 204; missing/repeated/foreign marker
deletion returns 404 ProblemDetails. Category, group, and game deletion cascade through their current markers and
media. Moved markers and transferred categories survive deletion of former parents; referenced files remain untouched.

Category markersCount is computed from committed marker rows in every category response. Creation, deletion, and
same-game transfers change the next count, without storing counters or touching category timestamps.
All expected errors use ProblemDetails. Unexpected storage failures are logged centrally and return safe 500 responses.

Apply the Markers migration explicitly before serving these endpoints; see the Infrastructure persistence README.
