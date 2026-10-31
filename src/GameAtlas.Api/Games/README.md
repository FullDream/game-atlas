# Games

## Delete game

DELETE `/games/{id}` requires no authentication or request body. It permanently removes a draft or published game and returns HTTP 204 with no response body after deletion and any map cleanup intent commit together. All its maps, styles, categories, groups, markers and media rows cascade; owned map files are removed durably by the worker. Other games remain unchanged. The selected image asset and marker-media assets remain in the shared library.

A missing game, including a repeated deletion or an unknown valid GUID, returns HTTP 404 ProblemDetails. An identifier that cannot be parsed as a GUID returns 400 ProblemDetails. Unexpected storage failures return a safe 500 response without internal details.

Deleted games disappear from list items and totals, and a valid PATCH to their identifier returns 404. Their slugs become available for creation or updating another game under the existing validation and uniqueness rules. Deletion has no restoration mechanism. Apply the Maps migration before deploying the map-aware handler; see Infrastructure Maps README for upgrade and rollback. See `GameAtlas.Api.http` for an example.

## List games

GET `/games` requires no authentication and includes all statuses, including Draft. Optional query parameters:

| Parameter     | Default                     | Accepted values                                           |
| ------------- | --------------------------- | --------------------------------------------------------- |
| offset        | 0                           | Integer >= 0                                              |
| limit         | 20                          | Integer from 1 to 100                                     |
| search        | None                        | Literal name substring; surrounding whitespace is trimmed |
| sortBy        | None                        | name, createdAt, updatedAt (case-insensitive)             |
| sortDirection | asc when sortBy is supplied | asc, desc (case-insensitive); requires sortBy             |

The HTTP 200 response is `{ "items": [], "limit": 20, "offset": 0, "total": 0 }`. Each item has the complete game shape shown below for creation. Total counts search matches before pagination; an offset beyond the matches returns an empty array with the actual Total. Empty search means no filter. Percent, underscore, and backslash are literal search characters, not wildcard syntax.

SQLite search ignores ASCII letter case. Cyrillic input is allowed but matching across Cyrillic letter cases is not guaranteed. Without sortBy games are ordered by createdAt ascending, then Id ascending. Explicit sorting occurs before pagination, with Id used to break ties. Name ordering follows SQLite's default collation. Count and page are separate reads; concurrent changes may affect their consistency.

Invalid pagination ranges, undefined sorting enum values, or a direction without a field return 422 validation ProblemDetails. Inputs that cannot bind to integers or enums, including unknown enum names and empty sorting values, return 400 ProblemDetails. Unexpected storage errors use the built-in safe 500 response. See `GameAtlas.Api.http` for default, search, and sorted examples.

API requests and Application queries use nullable GameSortBy and SortDirection enums with ASP.NET Core binding. A shared endpoint convention normalizes query enum names before binding to accept them case-insensitively. OpenAPI lists camelCase names. Standard enum parsing also accepts surrounding whitespace, numeric values, and comma-separated names when they resolve to defined values. GameSortBy values 0, 1, and 2 mean Name, CreatedAt, and UpdatedAt; SortDirection values 0 and 1 mean Asc and Desc.

## Update game

PATCH `/games/{id}` requires no authentication. Both `application/merge-patch+json` and `application/json` use identical JSON Merge Patch semantics; `charset=utf-8` is accepted with either. PUT is not supported. Other request media types, including `application/json-patch+json` and `text/plain`, return 415 ProblemDetails.

Supply at least one of `name`, `slug`, `coverId`, `bannerId`, `releaseDate`, `developer`, `description`, or `status`. Omitted fields retain their values; null for mutable required fields returns 422. Maps are independent child resources; GameDto includes read-only map summaries and content counts, with no selected mapId. Supplying mapId in PATCH returns 400 atomically. Metadata uses the same trimming, length, and slug rules as creation. Status accepts string values `draft` and `published`; publication and returning to draft have no extra prerequisites.

Success returns 200 with the complete GameDto. The server preserves `id` and `createdAt` and changes `updatedAt` only when at least one normalized value changes. A nonempty patch with existing values returns the current game without saving or touching its timestamp. An empty object returns 422. Unknown fields and client-supplied `id`, `createdAt`, or `updatedAt` return 400, even alongside valid changes. JSON bodies must be objects; malformed JSON, incompatible value types, invalid GUIDs, and unknown or numeric status representations return 400. Errors use ProblemDetails.

A valid patch for a missing game returns 404; it never creates a game. Keeping the current slug is allowed. A slug owned by another game returns 409, including when the unique constraint detects a race during saving. Any failed patch leaves all stored game fields unchanged. Unexpected storage failures return safe 500 responses. Concurrent updates have no ETag or version check in this version.

## Create game

Apply SQLite migrations before starting the API (see Infrastructure/Persistence/README.md). POST `/games` requires no authentication yet. Run the example in `GameAtlas.Api.http` using the API's HTTPS launch profile.

The request accepts `name`, `slug`, and required UUID `coverId` selecting an existing library image. Additional `mapId` input is ignored without parsing, like other server-owned creation metadata; new games have no maps. Required strings are trimmed and must remain nonempty. Name and slug allow up to 200 characters. Missing/null/empty image IDs return 422; malformed IDs return 400, missing assets 404, and video/folder IDs 422. Slugs contain lowercase ASCII letters or digits separated by single hyphens. Create maps separately through POST /games/{gameId}/maps.

Success is HTTP 200 with all stored fields, for example:

```json
{
  "id": "019a0000-0000-7000-8000-000000000001",
  "name": "Elden Ring",
  "slug": "elden-ring",
  "cover": {
    "type": "image",
    "id": "019a0000-0000-7000-8000-000000000112",
    "folderId": null,
    "name": "Cover",
    "url": "/media/files/019a0000-0000-7000-8000-000000000112",
    "contentType": "image/png",
    "byteLength": 1024,
    "width": 32,
    "height": 24,
    "alt": "",
    "placeholder": null,
    "createdAt": "2026-10-22T18:00:00+00:00",
    "updatedAt": "2026-10-22T18:00:00+00:00"
  },
  "banner": null,
  "releaseDate": null,
  "developer": null,
  "description": null,
  "markersCount": 0,
  "categoriesCount": 0,
  "maps": [],
  "createdAt": "2026-10-22T18:00:00+00:00",
  "updatedAt": "2026-10-22T18:00:00+00:00",
  "status": "draft"
}
```

The server generates a UUID v7, sets equal UTC creation/update timestamps, and always creates a draft. These server-owned fields are not request inputs. Reusing a slug fails rather than creating another game.

Failures use `application/problem+json`: malformed JSON or GUID input returns 400, invalid metadata returns 422 with field errors, a duplicate slug returns 409, and unexpected failures return a safe 500 response. In development, OpenAPI is available at `/openapi/v1.json` and Scalar at `/scalar`.

## Shared cover images

All game responses resolve the complete canonical ImageAsset at cover. PATCH omission preserves the cover; explicit null is invalid. Replacing a cover keeps the former asset in the library. Shared metadata edits appear in later game responses without touching game timestamps. A used cover cannot be deleted (409), including when its physical original is missing.

## Image relationship migration

Apply the new migration explicitly with dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api. The complete chain supports fresh databases. The earlier GameImageAssets migration rejects legacy game rows with arbitrary image strings before modifying them; no string-to-asset conversion or automatic database reset is provided. Prepare a fresh database separately when needed. Rollback with nonempty games is also rejected; restore a backup instead. Applied migrations remain unchanged. ExpandedGameMetadata upgrades games that already use image assets by renaming the cover reference and adding nullable metadata/banner, preserving identities, references and timestamps.

## Extended metadata and content summaries

Creation accepts optional nullable bannerId, releaseDate, developer and description. PATCH omission preserves these values; null clears them. Dates use YYYY-MM-DD without time or timezone. Developer is trimmed and blank becomes null; description is plain text preserving line breaks, with empty text normalized to null. Banner selection validates existing library images like cover selection. Replacing either image or deleting a game never deletes shared assets.

Every game response includes marker/category counts across all owned maps and maps ordered by createdAt then id. Map entries contain only id, name and nullable preview, the canonical ImageAsset source of the earliest surviving ready style. Games without content return zero counts and maps=[]. Complete map APIs remain separate. Counts/maps are read-only PATCH fields, rejected with 400. Metadata-only asset/content changes do not change game timestamps. The old imageAssetId PATCH property is rejected with 400.
