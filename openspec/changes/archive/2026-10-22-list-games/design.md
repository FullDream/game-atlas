# Design

## Context

See proposal.md for motivation and specs/game-catalog/spec.md for the behavior contract. The existing game-catalog spec covers creation only. Api/Games/Endpoints.cs groups resource operations and returns OutcomeResult. Application uses Mediator, a shared Result-based validation behavior, and IApplicationDbContext.Games. GameDto and GameMappingExtensions currently live under Games/CreateGame. SQLite has no pagination helpers or listing query. Game timestamps are UTC DateTimeOffset values stored as TEXT; direct DateTimeOffset ordering is unsupported by the SQLite EF provider.

## Goals / Non-Goals

**Goals:** Add one read slice, share the exact creation DTO, and execute filtering, counting, sorting, and pagination in SQLite. Keep query validation in Application and the endpoint thin.

**Non-Goals:** No generic query/repository framework, dynamic property-expression builder, status filter, client-side full-catalog filtering, mandatory default sort, Unicode search extension, or domain changes for query concerns.

## Decisions

### Shared contracts and feature organization

Add `Application/Common/Pagination/PaginatedResult.cs` containing exactly `public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, int Limit, int Offset, int Total);`. Move GameDto and its separate GameMappingExtensions file into Application/Games, updating existing creation references without changing its JSON response. Reuse the same DTO and mapper for the list; materializing at most the requested page before ToDto avoids requiring a custom method to translate to SQL.

Add Application/Games/ListGames with ListGames query, ListGamesHandler, and ListGamesValidation in separate files. Return Result<PaginatedResult<GameDto>>. Add GET to the existing resource-level Endpoints.cs, with operationId ListGames and summary `List games`. Keep query request data and query-to-Application mapping separate from mapping implementation, using the existing API Games mapping file. A separate list-only DTO would duplicate the agreed complete contract, so it is not introduced.

### Binding and validation

Use a separate ListGamesRequest class with FromQuery properties and bind it through AsParameters. Nullable Offset and Limit distinguish omission; mapping supplies defaults 0 and 20, documented with DefaultValue attributes. Keep descriptions on the request properties. Register ListGamesRequest -> ListGames with the existing OpenAPI validation source resolver. Extend the shared FluentValidationSchemaTransformer to resolve query property container metadata and apply the same validator-to-schema rules used for body contracts. Numeric bounds come from ListGamesValidation; do not duplicate them with Range annotations or a ListGames-specific transformer. Use nullable strings for sorting rather than HTTP-bound enums: unsupported string values reach FluentValidation and become 422, whereas unparseable integers remain built-in 400 ProblemDetails. Runtime validation stays in the Application behavior.

Validate Offset >= 0, Limit in [1,100], the fixed case-insensitive sort field whitelist, asc/desc, and the dependency of a supplied direction on a supplied field. Preserve an omitted direction as null until validation completes; only then default it to asc for a supplied field. Do not silently default unsupported options or interpret numeric enum values. Let the existing validation behavior aggregate field errors and skip handler execution.

Planning defaults: explicit empty or whitespace-only sorting values are invalid; surrounding sorting whitespace is not normalized into an accepted token. Query names are documented in camelCase while C# properties use PascalCase. Search alone is trimmed, with empty search disabling the filter. No unrequested search length limit is added. HTTP query-name binding retains framework conventions.

### Query execution and consistency

Start from the no-tracking Games query and do not filter by status. Apply the optional name predicate, then await CountAsync for Total. Apply an explicit switch over the validated sort field and direction, followed by Skip and Take and asynchronous page materialization. Count and page execute sequentially on the scoped context; never Task.WhenAll them. Map only the page to DTOs and construct the shared pagination result with the effective request values.

When sorting is omitted, do not add OrderBy, including an Id default. This intentionally retains provider-defined ordering as requested. When sorting is present, use the requested field/direction and Id ascending as an internal tie-breaker, without promising locale-aware name collation. Return an empty collection with the actual filtered Total for an out-of-range offset. No snapshot across the count and page queries is promised; concurrent writes can change results between them. A transaction solely to freeze the catalog is not introduced.

### SQLite search

Use parameterized EF.Functions.Like with a contains pattern and an explicit escape character. Escape the escape character, `%`, and `_` before surrounding the trimmed search with wildcard markers. This implements literal substring semantics while reusing SQLite's default ASCII-insensitive LIKE behavior. Do not apply a lowercasing workaround that claims Unicode support, reject Cyrillic input, add a normalized-name column, or register custom functions. PostgreSQL Unicode matching is a later provider concern.

### SQLite timestamp ordering and compatibility

Keep Domain and DTO timestamps as DateTimeOffset. In Infrastructure, explicitly convert CreatedAt and UpdatedAt to invariant-culture UTC strings in the existing provider format `yyyy-MM-dd HH:mm:ss.FFFFFFFzzz`, with an invariant parser on reads. This lets SQL order the mapped string provider values rather than unsupported DateTimeOffset expressions. All application-created timestamps have offset zero; their text order follows UTC chronological order without truncating fractional seconds.

The format matches existing Microsoft.Data.Sqlite TEXT binding. Preserve old rows and prove compatibility by seeding old-format values before applying the new model. Do not use Julian-day conversion, millisecond truncation, raw user-controlled ORDER BY, or client-side sorting after paging. Those alternatives either lose precision or bypass the agreed query structure. Do not directly reference EF internal converter types.

Generate a new Infrastructure migration and snapshot update if EF detects the provider-type mapping change; do not rewrite the archived initial migration. A model-only/metadata migration is acceptable because physical storage remains TEXT. Inspect generated operations and verify that they neither drop game data nor rewrite timestamps unnecessarily. The converter is SQLite-specific and must be reconsidered when PostgreSQL is introduced.

## Risks / Trade-offs

- [Unordered offset pagination can repeat or skip entries] -> This is the explicitly selected behavior; document it and do not assert insertion order in unsorted tests.
- [LIKE contains patterns and deep offsets can scan many rows] -> Bound page size to 100; no speculative index or full-text engine is added.
- [DateTimeOffset SQL ordering fails with the current mapping] -> Use the compatible string converter and test both timestamp fields in SQL with fractional-second precision and nonzero offsets/pages.
- [Count and page see concurrent writes differently] -> Document ordinary two-query consistency; snapshot semantics are outside this change.
- [Case-insensitive Cyrillic search remains incomplete] -> Document the SQLite limitation and revisit with PostgreSQL rather than adding transitional schema.
- [Moving the DTO breaks existing source references] -> Update Api, Application, and tests together, and run the full Create Game regression suite.

## Migration Plan

Keep existing game data and public response shapes intact. If the mapping change generates a new migration, apply it explicitly through EF CLI using Infrastructure and Api, as documented in the existing persistence README. Test both a fresh migrated SQLite file and a file with rows written by the original timestamp mapping. Verify no pending model changes after the generated migration. Rollback reverts the new mapping metadata without dropping Games or recreating the database; inspect the generated Down operations before documenting the exact command.

## References

- SQLite ordering limitations: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations
- Existing timestamp TEXT format: https://github.com/dotnet/efcore/blob/v10.0.0/src/Microsoft.Data.Sqlite.Core/SqliteValueBinder.cs
- SQLite LIKE behavior: https://www.sqlite.org/lang_expr.html#like
