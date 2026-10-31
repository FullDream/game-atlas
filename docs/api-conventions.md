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

GET /games/{id} returns a draft or published game with the same expanded representation as listing: id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt and status. Cover is a required canonical ImageAsset and banner is nullable ImageAsset. Maps are read-only summaries with id, name and nullable ImageAsset preview from the default style source; complete maps use their separate endpoints. Release dates use YYYY-MM-DD without time. Retrieval does not change timestamps. Valid missing or deleted identifiers return 404 ProblemDetails; malformed UUID identifiers return 400. OpenAPI exposes the operation as GetGame under the Games tag.
