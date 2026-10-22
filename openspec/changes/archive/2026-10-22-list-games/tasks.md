# Tasks

## 1. Shared response contracts

- [x] 1.1 Add the exact shared PaginatedResult<T> record under Application/Common/Pagination and move GameDto and its separate GameMappingExtensions file to Application/Games; update references and verify a solution build plus existing Create Game response and OpenAPI tests pass without JSON changes.

## 2. SQLite timestamp ordering

- [x] 2.1 Configure invariant UTC string conversions for CreatedAt and UpdatedAt in Infrastructure using the existing SQLite timestamp TEXT format; verify SQLite integration tests execute ascending and descending ordering for both fields with fractional-second precision and identical timestamps without client evaluation.
- [x] 2.2 Generate any required new migration and snapshot update through EF CLI, leaving InitialGames unchanged; verify fresh migrations and original-format populated databases round-trip timestamps unchanged, retain all games, and report no pending model changes. Update persistence documentation with the mapping compatibility and explicit migration/rollback commands if a migration is generated.

## 3. Application listing slice

- [x] 3.1 Add ListGames query and ListGamesValidation with pagination defaults, range checks, case-insensitive sort whitelists, and direction/field dependency; verify Application tests cover offset 0/negative, limit 1/100/outside range, omitted direction, mixed case, unknown/numeric/empty sorting tokens, and aggregated Result validation errors through the existing behavior.
- [x] 3.2 Add ListGamesHandler using IApplicationDbContext, shared GameDto, and sequential filtered CountAsync then paged materialization; verify SQLite integration tests cover all statuses, empty catalogs, defaults, limit/offset slices, out-of-range offsets, correct Total before pagination, no writes, and no full-catalog materialization.
- [x] 3.3 Add trimmed literal substring search using escaped parameterized LIKE; verify SQLite tests cover mixed-case ASCII matching, omitted/empty/whitespace search, trimmed terms, no matches, literal percent/underscore/escape characters, permitted Cyrillic input, and Total based on the same filter as Items.
- [x] 3.4 Implement the explicit sort-field/direction switch and optional Id tie-breaker only for sorted requests; verify all six field/direction combinations and ascending default with nonzero offset, mixed-case options, and tied values. Inspect captured SQL to confirm sorting precedes LIMIT/OFFSET and that omitted sorting produces no ORDER BY; unsorted tests must not assume insertion order.

## 4. HTTP contract and documentation

- [x] 4.1 Add GET /games to the existing Games/Endpoints.cs with query binding, a separate mapping extension, Mediator dispatch, and OutcomeResult<PaginatedResult<GameDto>>; verify HTTP tests assert 200, the exact items/limit/offset/total wrapper, full shared item metadata, all statuses, and no authentication requirement.
- [x] 4.2 Verify HTTP validation through the Application pipeline and built-in binding: 422 field-associated ProblemDetails for invalid ranges, unknown sorting options, direction without field, and explicitly empty sorting tokens; 400 ProblemDetails for non-integer/overflow pagination; safe 500 on unexpected storage errors. Confirm a valid sort field with omitted direction succeeds.
- [x] 4.3 Add List games OpenAPI metadata and runnable default, search, and sorted HTTP examples; document pagination bounds, ASCII-only case-insensitive search, literal search semantics, and unspecified ordering when sorting is absent. Verify OpenAPI exposes all five query parameters, pagination defaults/bounds, the paginated response and error responses, and that examples match successful HTTP tests.

## 5. Integration verification

- [x] 5.1 Build the solution, run all Domain, Application, and Integration tests, validate list-games with openspec --strict, and check migration/model consistency; verify existing Create Game, Result/OutcomeResult, Optional serialization, and OpenAPI tests remain passing with no unintended schema or public response changes.
