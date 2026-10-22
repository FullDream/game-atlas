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

Invalid ranges, sorting tokens, or a direction without a field return 422 validation ProblemDetails; explicitly empty sorting values are invalid. Non-integer or overflowing pagination values return 400 ProblemDetails. Unexpected storage errors use the built-in safe 500 response. See `GameAtlas.Api.http` for default, search, and sorted examples.

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
