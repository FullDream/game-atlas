# Tasks

## 1. Domain update behavior

- [x] 1.1 Add Game.Update with Optional domain values, candidate-state validation, Result<bool> change detection, and domain status errors; verify domain tests cover all mutable fields, preserved omissions, clearing MapId, required nulls, invalid slug/status, and no mutation on failure.
- [x] 1.2 Implement normalized equality and aggregate-owned UpdatedAt changes; verify domain tests cover same-value and trimmed no-ops, real metadata and status changes, both publication transitions, immutable Id/CreatedAt, UTC timestamps, and technical length limits remaining outside Domain.

## 2. Application command, validation, and persistence

- [x] 2.1 Add Games/UpdateGame command, UpdateGameValidation, and application-owned errors using OptionalRuleExtensions and shared metadata limits; verify Application tests cover unspecified fields, explicit null, whitespace, exact and excessive trimmed lengths, slug syntax, defined/null/undefined status, empty patch rejection, and correct field error names.
- [x] 2.2 Implement UpdateGameHandler with AsTracking, NotFound, uniqueness checks excluding the target game before domain mutation, Game.Update, conditional SaveChanges, and shared ToDto; verify mediator integration tests with SQLite cover persisted edits, both status transitions, missing games, own-slug acceptance, conflicts leaving both games unchanged, and validation failures preventing writes.
- [x] 2.3 Verify no-op and failure persistence behavior using SQLite integration tests: no-op patches must not call SaveChanges, effective changes survive reopening the database, and real duplicate-slug update failures use existing conflict classification while unrelated save failures remain exceptions for safe HTTP handling.

## 3. PATCH HTTP contract

- [x] 3.1 Add separate UpdateGameRequest with Optional nullable wrapped types and request-local unmapped-member rejection; extend the separate API mapping file and resource Endpoints.cs with UpdateGame; verify API tests cover all six response data categories (identity, strings, map, timestamps, status, complete DTO shape), individual field updates, omission, clear-map, and 404 for a valid missing identifier.
- [x] 3.2 Accept application/merge-patch+json and application/json through the same merge-patch path with endpoint-scoped media-type enforcement before deserialization; verify API tests exercise omission, explicit null, validation, and updates with both types, accept charset=utf-8 for both, reject application/json-patch+json and text/plain with 415 ProblemDetails even for malformed bodies, and confirm no PUT update route.
- [x] 3.3 Verify request rejection and atomicity with API tests for empty object (422), explicit required nulls (422), malformed/non-object bodies and wrong value types (400), unknown/numeric status (400), invalid GUID representations (400), and unknown/immutable fields even alongside valid changes (400); assert game values and timestamps remain unchanged.
- [x] 3.4 Verify API tests return 200 with unchanged UpdatedAt for normalized no-ops, 409 for existing and persistence-time slug conflicts, and safe 500 for unrelated persistence failures; confirm ProblemDetails media types and absence of internal exception details.
- [x] 3.5 Document Update game in Games/README.md and add edit, publication, return-to-draft, and clear-map examples to GameAtlas.Api.http covering both supported media types and identical merge-patch semantics; verify examples and documented status codes match the API contract and do not introduce PUT.

## 4. OpenAPI contract

- [x] 4.1 Register UpdateGameRequest to UpdateGame validation mapping and reuse or minimally extend the existing shared Optional/FluentValidation schema support; verify generated OpenAPI tests cover operationId UpdateGame, GUID route parameter, both supported request media types sharing one schema and merge-patch semantics, five optional fields, only MapId permitting null, string enum values, metadata lengths and slug pattern, nonempty object documentation, and unknown properties disallowed.
- [x] 4.2 Document the complete GameDto success response and 400/404/409/415/422/500 ProblemDetails responses; verify OpenAPI tests confirm those responses, no PUT operation, and unchanged creation/list schemas without exposing Optional wrapper internals.

## 5. Integration checks

- [x] 5.1 Run `dotnet test GameAtlas.slnx -c Release --verbosity quiet` and verify all existing and new tests pass, including POST ignoring additional server-owned/status fields and existing list/search/sort behavior; run `git diff --check` and inspect the final diff for project conventions and changes limited to this feature.
- [x] 5.2 Run `openspec validate update-game --strict` and verify every delta scenario has implementation/test evidence before marking tasks complete; confirm no EF schema migration is needed and no unrelated files or generated build artifacts are included.
