# API conventions

Applies to HTTP endpoints, request and response contracts, API clients and contract tests.

## Resource updates

- Use PATCH for resource updates, accepting both `application/merge-patch+json` and `application/json` with identical JSON Merge Patch semantics (RFC 7396: https://www.rfc-editor.org/rfc/rfc7396). Allow charset parameters for both media types. Do not use PUT in this project. Reject other update media types, including `application/json-patch+json` and `text/plain`, with HTTP 415.
- Make every mutable field optional in update requests. Use the existing `Optional<T>` to distinguish an omitted field from an explicitly supplied value, including `null`. Optional presence does not make a domain field nullable.
- Leave omitted fields unchanged. Apply supplied values; `null` removes or clears a value only when the resource permits it. Reject `null` for required fields with HTTP 422. Use nullable wrapped types when necessary to preserve explicit `null` for validation, including value types such as enums.
- Merge supplied object members recursively. Replace arrays as a whole rather than merging their elements.
- Require resource update bodies to be JSON objects. Reject malformed JSON, incompatible JSON types, and unknown or read-only fields with HTTP 400. Reject an empty object with HTTP 422 because at least one mutable field must be supplied.
- Validate all supplied changes and the resulting domain state before saving. Apply a patch atomically; a failed patch must not persist partial changes.

## HTTP representation and errors

- Use camelCase for HTTP query parameter names while keeping C# property names in PascalCase. Apply query-name casing globally through the OpenAPI configuration; do not repeat `FromQuery.Name` solely to change casing.
- Return HTTP errors as `ProblemDetails` through the API result integration or centralized exception handling.
- Handle unexpected library and database exceptions centrally as HTTP 500 responses, without converting them to `Result`. Log exception details and do not expose them to clients.
- Translate specifically recognized database constraint conflicts to their agreed HTTP error responses; do not treat every database exception as a conflict.
- Serialize enums as camelCase strings in the API. Document query enum values in camelCase and accept names case-insensitively through the shared endpoint convention; keep request properties strongly typed as enums.

## OpenAPI schemas

- Keep C# DTO names internal to the implementation. Public schema identifiers omit the `Dto` suffix, including generic arguments: `GameDto` becomes `Game` and `PaginatedResult<MediaItemDto>` becomes `PaginatedResultOfMediaItem`. Request, result, and enum names retain their existing meaning. Compose naming with `Optional<T>` inlining so PATCH presence and nullability stay intact.
- Reuse canonical `MediaFolder`, `ImageAsset`, and `VideoAsset` schemas from `MediaItem` and `MediaAsset` unions, concrete responses, and nested properties. Union references and discriminator mappings must target the same canonical components; do not expose parent-prefixed duplicates.
- Concrete media schemas are ordinary objects with required literal `type` values and no single-variant discriminator wrapper. Video `preview` is nullable `ImageAsset`; nullable composition is separate from polymorphism. Keep true mixed unions and the runtime serialization attributes, which ensure exactly one `type` property in HTTP responses.
- Apply normalization in the API OpenAPI layer and reject conflicting concrete representations before merging. Renaming schemas requires regenerating existing frontend clients and migrating their consumers in the same change. Continue serving the document and Scalar in development and generating clients explicitly over HTTP.

## Game retrieval

GET /games/{id} returns a draft or published game with the same expanded representation as listing: id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt and status. Cover is a required canonical ImageAsset and banner is nullable ImageAsset. Maps are read-only summaries with id, name, slug and nullable ImageAsset preview from the default style source; complete maps use their separate endpoints. Release dates use YYYY-MM-DD without time. Retrieval does not change timestamps. Valid missing or deleted identifiers return 404 ProblemDetails; malformed UUID identifiers return 400. OpenAPI exposes the operation as GetGame under the Games tag.

## Public game and map reads

Public read contracts are additive and live in `Games/PublicEndpoints.cs` and `Maps/PublicEndpoints.cs`, under OpenAPI tags `Public Games` and `Public Maps`. Existing management operations retain their current contracts and visibility. The shared Games frontend client generates both management and public Games operations; web consumers use separate public query options.

- `GET /public/games` (`ListPublicGames`) returns a paginated published-only catalog. Cards contain id, name, slug, canonical cover, nullable banner, mapsCount, markersCount and categoriesCount. The offset/limit defaults are 0/20 with limit 1..100. Search, sorting, validation and tie-breaking follow the existing game list. Publication filtering precedes total calculation and pagination.
- `GET /public/games/{gameSlug}` (`GetPublicGame`) returns published game metadata, canonical images, public counts and published map summaries ordered by createdAt then id. Summaries contain id, name, slug, markersCount, categoriesCount and nullable preview from the selected ready default style. Missing or hidden games return 404 ProblemDetails.
- `GET /public/games/{gameSlug}/maps/{mapSlug}` (`GetPublicMap`) resolves a published map within its published game. It returns logical geometry, ready styles in display order, the selected ready default, ordered groups with nested categories, and all visible markers in one flat array referencing categoryId. Missing, hidden or foreign resources return 404 ProblemDetails. Empty/style-less maps remain readable with empty arrays and nullable geometry/default.

Public visibility requires published status at every level: game, map, group, category and marker. Hidden ancestors hide their descendants. All counts use those same visible sets; empty visible groups/categories remain present. Public DTOs omit publication status, administrative timestamps and processing details. Marker media and category icons reuse canonical media contracts; images and tiles remain URLs. Ready styles contain id, name, backgroundColor, tileGeometry, tilesUrlTemplate and a canonical ImageAsset preview from the style source for selector thumbnails, without processing progress/errors. Marker pagination and viewport loading are not used; the expected map size is approximately 1000–2000 markers, with no hard truncation limit.

Detailed map retrieval has an Application-only `ContentVisibility` mode: `PublishedOnly` or `IncludeDrafts`. The latter includes drafts throughout the owned hierarchy while retaining ready-style filtering and the same response shape. The public endpoint always selects `PublishedOnly`; query flags cannot enable preview. There is no preview HTTP endpoint yet; it must ship together with administrator authorization. Existing management endpoints and static media/tile access are not protected or changed by this public read boundary.

## Map creation

POST /games/{gameId}/maps requires name and slug. Nullable input types preserve validation of missing/null values as 422; stored fields are required. Slugs are trimmed, 1..200 characters, lowercase ASCII letters/digits separated by single hyphens. They are client-supplied, unique per game, and included as strings in complete map responses and game map summaries. Recognized slug conflicts return 409. Existing ID-based addressing remains unchanged.

## Map metadata updates

PATCH /games/{gameId}/maps/{mapId} accepts optional name, slug, defaultStyleId and styleOrder and returns the complete map. styleOrder replaces the complete style ID permutation; defaultStyleId must reference a ready style of this map. Null presentation values and incomplete/duplicate orders return 422; missing/foreign style IDs return 404. Order and default are independent. Width, height and geometry are read-only, absent before the first successful style attachment and immutable afterward. Slug follows creation validation and game-scoped uniqueness; omission preserves it, null returns 422, and renaming does not regenerate it. Names follow creation validation and game-scoped uniqueness; an identical trimmed name is a no-op preserving updatedAt. Actual renames update the map timestamp without changing the owning game timestamp or queuing processing. Missing or foreign maps return 404. The shared merge-patch media, field-presence and error conventions apply.

## Category trees and placement

GET /games/{gameId}/maps/{mapId}/category-groups returns groups with nested categories, optional canonical image icons and computed marker counts. Group and nested category arrays are returned in display order; numeric order is internal and omitted from responses. Group/category operations share Categories. The former per-group category GET is removed; the retained POST path returns 405 for GET.

Placement uses optional nullable beforeId rather than writable order. Creation and cross-group transfer append when omitted; same-parent metadata updates preserve position. Null appends; a sibling ID inserts before it. Self anchors return 422, missing/foreign anchors 404 and numeric order input 400. Transfers stay within one map and validate resulting-name uniqueness atomically. iconAssetId is optional on creation and nullable on PATCH; null detaches. See the [Categories API](../src/GameAtlas.Api/Categories/README.md).

Group color accepts #RGB or #RRGGBB and returns uppercase #RRGGBB. Omitted creation color defaults to #F59E0B; PATCH omission preserves color, while null is invalid. Category color is optional and nullable: omitted/null on creation stores no color, PATCH omission preserves it and explicit null clears it. Non-null colors retain hex validation and normalization.
