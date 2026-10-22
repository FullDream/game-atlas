# Games

## List games

GET `/games` requires no authentication and includes all statuses, including Draft. Optional query parameters:

| Parameter | Default | Accepted values |
| --- | --- | --- |
| offset | 0 | Integer >= 0 |
| limit | 20 | Integer from 1 to 100 |
| search | None | Literal name substring; surrounding whitespace is trimmed |
| sortBy | None | name, createdAt, updatedAt (case-insensitive) |
| sortDirection | asc when sortBy is supplied | asc, desc (case-insensitive); requires sortBy |

The HTTP 200 response is `{ "items": [], "limit": 20, "offset": 0, "total": 0 }`. Each item has the complete game shape shown below for creation. Total counts search matches before pagination; an offset beyond the matches returns an empty array with the actual Total. Empty search means no filter. Percent, underscore, and backslash are literal search characters, not wildcard syntax.

SQLite search ignores ASCII letter case. Cyrillic input is allowed but matching across Cyrillic letter cases is not guaranteed. Without sortBy the API adds no ordering, so page order is unspecified and may vary. Explicit sorting occurs before pagination, with Id used to break ties. Name ordering follows SQLite's default collation. Count and page are separate reads; concurrent changes may affect their consistency.

Invalid pagination ranges, undefined sorting enum values, or a direction without a field return 422 validation ProblemDetails. Inputs that cannot bind to integers or enums, including unknown enum names and empty sorting values, return 400 ProblemDetails. Unexpected storage errors use the built-in safe 500 response. See `GameAtlas.Api.http` for default, search, and sorted examples.

API requests and Application queries use nullable GameSortBy and SortDirection enums with ASP.NET Core binding. A shared endpoint convention normalizes query enum names before binding to accept them case-insensitively. OpenAPI lists camelCase names. Standard enum parsing also accepts surrounding whitespace, numeric values, and comma-separated names when they resolve to defined values. GameSortBy values 0, 1, and 2 mean Name, CreatedAt, and UpdatedAt; SortDirection values 0 and 1 mean Asc and Desc.

## Update game

PATCH `/games/{id}` requires no authentication. Both `application/merge-patch+json` and `application/json` use identical JSON Merge Patch semantics; `charset=utf-8` is accepted with either. PUT is not supported. Other request media types, including `application/json-patch+json` and `text/plain`, return 415 ProblemDetails.

Supply at least one of `name`, `slug`, `image`, `mapId`, or `status`. Omitted fields retain their values. Explicit `mapId: null` clears the map identifier; null for any other mutable field returns 422. Metadata uses the same trimming, length, and slug rules as creation. Map existence is not checked. Status accepts string values `draft` and `published`; publication and returning to draft have no extra prerequisites.

Success returns 200 with the complete GameDto. The server preserves `id` and `createdAt` and changes `updatedAt` only when at least one normalized value changes. A nonempty patch with existing values returns the current game without saving or touching its timestamp. An empty object returns 422. Unknown fields and client-supplied `id`, `createdAt`, or `updatedAt` return 400, even alongside valid changes. JSON bodies must be objects; malformed JSON, incompatible value types, invalid GUIDs, and unknown or numeric status representations return 400. Errors use ProblemDetails.

A valid patch for a missing game returns 404; it never creates a game. Keeping the current slug is allowed. A slug owned by another game returns 409, including when the unique constraint detects a race during saving. Any failed patch leaves all stored game fields unchanged. Unexpected storage failures return safe 500 responses. Concurrent updates have no ETag or version check in this version.

## Create game

Apply SQLite migrations before starting the API (see Infrastructure/Persistence/README.md). POST `/games` requires no authentication yet. Run the example in `GameAtlas.Api.http` using the API's HTTPS launch profile.

The request accepts `name`, `slug`, `image`, and optional nullable `mapId`. Required strings are trimmed and must remain nonempty. Name and slug allow up to 200 characters; image allows 2048 and need not be a URL. Slugs contain lowercase ASCII letters or digits separated by single hyphens. Map identifiers are stored without an existence check.

Success is HTTP 200 with all stored fields, for example:

```json
{
  "id": "019a0000-0000-7000-8000-000000000001",
  "name": "Elden Ring",
  "slug": "elden-ring",
  "image": "elden-ring-cover",
  "mapId": null,
  "createdAt": "2026-10-22T18:00:00+00:00",
  "updatedAt": "2026-10-22T18:00:00+00:00",
  "status": "draft"
}
```

The server generates a UUID v7, sets equal UTC creation/update timestamps, and always creates a draft. These server-owned fields are not request inputs. Reusing a slug fails rather than creating another game.

Failures use `application/problem+json`: malformed JSON or GUID input returns 400, invalid metadata returns 422 with field errors, a duplicate slug returns 409, and unexpected failures return a safe 500 response. In development, OpenAPI is available at `/openapi/v1.json` and Scalar at `/scalar`.
