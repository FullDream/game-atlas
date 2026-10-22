# Design

## Context

See proposal.md for motivation and specs/game-catalog/spec.md for the behavior contract. The existing layered monolith has Game.Create, private property setters, a unique Games.Slug index, direct IApplicationDbContext access, shared Result/OutcomeResult handling, and SQLite timestamp conversion. EF queries default to NoTracking.

Optional<T> already tracks presence, its JSON converter preserves explicit null for nullable wrapped types, and OptionalRuleExtensions conditionally validates specified values. OpenAPI already unwraps Optional fields and maps Application FluentValidation rules onto API schemas. POST deliberately ignores additional properties, as covered by existing integration tests and the main spec.

## Goals / Non-Goals

**Goals:** Keep mutation rules in the aggregate, preserve field presence end to end, reuse existing validation and error infrastructure, and make runtime PATCH behavior match OpenAPI.

**Non-Goals:** Introduce no repository abstraction, new persistence model, general-purpose merge engine, domain dependency on HTTP request types, or concurrency token. Existing scalar fields require no recursive object or array implementation in this change.

## Decisions

### 1. Typed patch contract with explicit presence

Keep CreateGameRequest and UpdateGameRequest in separate files. UpdateGameRequest uses Optional<string?> for Name, Slug, and Image, Optional<Guid?> for MapId, and Optional<GameStatus?> for Status. Add an UpdateGame command carrying the route Id and the same optional domain values; map it in the existing API GameMappingExtensions file.

Nullable wrapped value types are intentional: Optional<GameStatus> would turn explicit JSON null into default Draft in the current converter. Optional<GameStatus?> preserves null for a 422 validation failure. Omission stays unspecified at every boundary; mapping must never collapse it into null. Status uses the existing string enum converter; unknown strings and numeric JSON tokens are binding failures (400), while explicit null is a semantic failure (422).

Use request-local JsonUnmappedMemberHandling.Disallow rather than a global serializer setting, so unknown and immutable fields fail only for PATCH. The contract contains no Id or timestamps. Non-object JSON, invalid types, and malformed input use safe 400 ProblemDetails. The API continues to accept the existing case-insensitive property and enum binding conventions.

**Alternative:** A JsonDocument patch engine would add arbitrary JSON manipulation and duplicate validation for a resource with only scalar values. Plain nullable properties would lose the difference between omission and clearing MapId.

### 2. HTTP media-type enforcement before body binding

Register PATCH on `/games/{id}` in the resource's existing Endpoints.cs, with a Guid route parameter, operation ID UpdateGame, human summary `Update game`, OutcomeResult<GameDto>, and request metadata for both application/merge-patch+json and application/json using one schema and identical merge-patch semantics. An unparseable route identifier is a binding failure (400); a valid missing GUID returns 404.

Enforce the two supported media types, allowing charset parameters for both, before deserialization using a narrowly scoped guard associated with the PATCH endpoint. Both use the same request binding, validation, and update path. Merely accepting any JSON-compatible `+json` content type is insufficient. Keep this HTTP-only concern in Api and reuse built-in ProblemDetails services for 415/400. Do not make the thin endpoint implement merge logic or application validation. No PUT route is added.

**Alternative:** Requiring only application/merge-patch+json would add client friction without changing update behavior. Accepting every JSON-compatible media type would also admit application/json-patch+json, which denotes a different patch format. Applying update checks to every route would change existing POST behavior.

### 3. Aggregate-owned atomic mutation and change detection

Add Game.Update accepting the five Optional domain values and returning Result<bool>, where success indicates whether any effective value changed. Use Optional.GetValueOrDefault(currentValue) to build candidate values while preserving explicitly supplied null, trim strings, and validate candidate domain invariants before assigning any field. Use explicit null checks to establish non-null candidates without null-forgiving assignments. Reuse the required-string and slug-format rules from creation without moving technical length limits into Domain. Add domain errors for null/undefined status values near GameErrors.

If validation fails, return errors without mutating the entity. If all normalized candidate values equal current values, return success with false and preserve UpdatedAt. Otherwise assign the mutable fields together, set UpdatedAt to DateTimeOffset.UtcNow once, and return true. Id and CreatedAt have no mutation path. Both defined status transitions are permitted with no extra publication prerequisites. Empty-patch rejection belongs in Application; a direct domain call with no changes is safely a no-op.

**Alternative:** Assigning properties in the handler bypasses the aggregate. Assigning sequentially before validation risks leaving a partially mutated tracked entity. Always touching UpdatedAt makes unchanged requests alter list sorting and causes unnecessary database writes.

### 4. Conditional request validation and persistence orchestration

Create Games/UpdateGame with command, UpdateGameHandler, UpdateGameValidation, and UpdateGameErrors. Use OptionalRuleExtensions for supplied fields and the shared behavior for orchestration. Reuse GameMetadataLimits, required-value checks, and slug syntax; validate trimmed lengths without losing the original property identity in validation descriptors. Require at least one specified mutable field. Reject null status and undefined enum values through Result for in-process callers, even though JSON binding rejects unknown enum representations earlier.

Load the game with AsTracking and SingleOrDefaultAsync because global tracking is disabled. Return a feature-owned NotFound error if absent. If a normalized supplied slug differs from the current one, check whether another game owns it, explicitly excluding the target Id. Perform this conflict check before invoking Game.Update so a rejected conflict does not leave a mutated tracked aggregate. Call Game.Update, propagate failures, and save once only when its result indicates changes. Return the shared game.ToDto() for both changed and unchanged success paths.

Retain the database unique index as the race-safe authority. Reuse the existing SQLite failure classifier and GameSlugConflictExceptionHandler for recognized save conflicts (409). Other exceptions continue to built-in safe 500 handling; never convert database exceptions to Result. A single SaveChanges is transactional, so a failed save persists none of the patch. No-op requests perform no SaveChanges.

**Alternative:** Application-only invariants would leave the domain unsafe for other callers. Relying only on the precheck permits duplicate slugs during races; catching all DbUpdateException instances as conflicts misreports unrelated failures.

### 5. Shared OpenAPI integration and usage documentation

Register UpdateGameRequest to UpdateGame with the existing OpenApiValidationSourceResolver. Reuse and, only as needed, extend the shared Optional/FV schema transformers so conditional rules expose trimmed string limits and slug patterns under their real field names without making supplied fields required. Distinguish optional presence from permitted null, and avoid mutating shared enum schema references when removing null from a particular property.

Document the object body as nonempty with unknown properties disallowed and enumerate the update response statuses 200, 400, 404, 409, 415, 422, and 500. Prefer built-in schema metadata and the existing shared transformer pipeline over a feature-specific transformer or duplicate validation annotations. Update Games/README.md and GameAtlas.Api.http with edit, publish, unpublish, and clear-map examples.

**Alternative:** Duplicating validation in attributes or a bespoke per-operation transformer lets runtime and documentation drift. Reusing CreateGameRequest would expose the wrong presence semantics.

## Risks / Trade-offs

- Optional values can be read while unspecified, or null can silently become Draft -> Preserve presence through mapping, use nullable wrapped status, and test omission/null separately.
- Conditional FV expressions can hide constraints from schema discovery -> Keep descriptor property names aligned and add generated OpenAPI tests for optional presence, nullability, lengths, pattern, and string enums.
- Global serializer changes can break POST's ignored-extra-fields contract -> Scope strict unknown-member handling and media-type checks to PATCH and run existing create/list tests.
- EF defaults to detached reads -> Explicit AsTracking for update loading and verify persistence after reopening SQLite.
- Slug checks race with other writes -> Retain the unique index, test a real update constraint violation, and verify no partial metadata or timestamp persistence.
- Concurrent updates have no version conflict detection and may overwrite overlapping values -> Apply supplied changes to the loaded aggregate; ETags and lost-update prevention remain explicitly out of scope.
- Wall-clock timestamps are not a strict monotonic sequence -> Preserve existing UTC clock semantics and do not add an artificial timestamp increment.

## Migration Plan

No schema change or migration is expected: all mutable fields and string status already exist. Apply existing migrations explicitly as usual. Deploy the additive PATCH route and documentation together. Rolling back the application removes the route without reverting saved metadata or status; existing GET can already return Published games.
