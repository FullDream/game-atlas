# Create Game

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
