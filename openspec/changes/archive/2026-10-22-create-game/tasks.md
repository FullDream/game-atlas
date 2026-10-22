# Tasks

## 1. Domain model

- [x] 1.1 Add Game and GameStatus in Domain/Games with a Result-returning creation boundary, normalized metadata, UUID v7, UTC creation timestamps, nullable MapId, and enforced Draft status; verify Domain tests cover valid creation, equal timestamps, generated identity, and absence of client control over server-owned fields.
- [x] 1.2 Enforce required metadata and lowercase ASCII slug syntax in Domain; verify Domain tests cover null/blank values, invalid slug forms, and arbitrary non-URL image strings. Keep technical length enforcement in Application and verify its validator tests cover exact and over-limit trimmed input.

## 2. Persistence and migrations

- [x] 2.1 Add Application's Domain and EF Core references, expose DbSet<Game> on IApplicationDbContext, and configure Game in Infrastructure with a unique Slug index, string Status, nullable MapId without a foreign key, and required metadata; verify a build and update PersistenceTests to assert the Game model while retaining scoped-context checks.
- [x] 2.2 Add required EF design-time tooling and generate the initial Infrastructure migration and snapshot using Api as the startup project; verify applying it to a temporary SQLite file creates the expected schema and explicitly confirm API startup does not apply migrations.
- [x] 2.3 Add SQLite persistence tests covering duplicate slug rejection, distinct slugs with the same name, string status storage, and metadata round-tripping after reopening a migrated file; verify these tests pass without using an in-memory EF substitute.
- [x] 2.4 Document EF migration generation, explicit application, connection-string configuration, and rollback/data-loss implications in Infrastructure persistence documentation; verify the documented commands work against a disposable SQLite database.

## 3. Application slice and shared validation

- [x] 3.1 Add shared Mediator validation behavior for Result-returning requests using IFailureFactory, sequential asynchronous validators, property-associated errors, and short-circuiting; verify Application tests cover aggregation, skipped handler execution on failure, valid requests reaching the handler once, and cancellation propagation.
- [x] 3.2 Add the CreateGame command, CreateGameHandler, CreateGameValidation, DTO, and extension mappings under Application/Games/CreateGame; save through IApplicationDbContext once and return the complete DTO. Verify tests cover trimmed input, validation failures, successful creation with null or supplied MapId, and Result-based rejection of an existing slug if a precheck is used.

## 4. API and exception handling

- [x] 4.1 Add the narrow persistence-failure classification contract and SQLite implementation for the specific Games.Slug unique violation; verify tests using actual SQLite failures distinguish that conflict from unrelated constraints and unexpected database errors without converting exceptions to Result.
- [x] 4.2 Register centralized exception and ProblemDetails handling, including safe 500 errors, direct 409 mapping of recognized slug exceptions, and 400 binding failures; verify HTTP tests outside development confirm status, ProblemDetails content type, and absence of exception, SQL, or connection details.
- [x] 4.3 Add and register POST /games in the resource-level Games/Endpoints.cs with request-to-command extensions in a separate mapping file, Mediator dispatch, and OutcomeResult; verify HTTP tests assert 200 with all fields, `draft` string status, UUID v7, equal UTC timestamps, omitted/null/supplied MapId, ignored additional server-owned fields, and persisted normalized values.
- [x] 4.4 Verify HTTP rejection scenarios with migrated SQLite: required and over-limit metadata, invalid slug syntax, malformed JSON/GUID input, sequential duplicate slug, concurrent attempts, and unexpected storage failure. Assert 422/400/409/500 ProblemDetails as specified, no invalid rows, and one row for a successfully created contested slug; include a deterministic unique-constraint test independent of prechecks and SQLite lock timing.
- [x] 4.5 Document the request, full 200 response, and ProblemDetails errors in endpoint OpenAPI metadata and a runnable HTTP example; verify the OpenAPI document exposes the operation and string status, preserves existing validation conventions, and the example works against a migrated local database.

## 5. Integration checks

- [x] 5.1 Build GameAtlas.slnx and run all existing and added test projects; verify no regressions in Result/OutcomeResult, Optional serialization, OpenAPI validation, or persistence registration and that the game-catalog scenarios are covered.
