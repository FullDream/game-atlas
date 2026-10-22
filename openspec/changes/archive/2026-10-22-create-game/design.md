# Design

## Context

See proposal.md for motivation and specs/game-catalog/spec.md for behavior. The solution already has four net10.0 projects and three xUnit test projects. Application registers Mediator and FluentValidation, but has no feature handlers and no Domain reference. IApplicationDbContext currently exposes only SaveChangesAsync. Infrastructure provides an empty SQLite context with scoped registration, NoTracking queries, and split queries. Api already supports OutcomeResult, validation ProblemDetails (422), conflict ProblemDetails (409), OpenAPI, and camelCase string enums with numeric values disabled. Program.cs has an empty endpoint group and no exception pipeline. PersistenceTests currently asserts an empty EF model.

## Goals / Non-Goals

**Goals:** Implement one complete request-to-storage slice while preserving the existing result and dependency-injection mechanisms. Establish shared validation and exception handling only as needed for Create Game.

**Non-Goals:** No module assemblies, shared aggregate framework, repository layer, generic CRUD infrastructure, event bus, or automated migration runner. Do not add endpoints outside the proposal's scope.

## Decisions

### Feature structure and dependency direction

Keep the existing projects. Add Game and GameStatus under Domain/Games; place the command, CreateGameHandler, CreateGameValidation, response DTO, and related mapping in Application/Games/CreateGame. Group the Games resource endpoints in Api/Games/Endpoints.cs. Keep request-to-command extension mapping in a separate GameMappingExtensions.cs file in that feature area. EF configurations and migrations stay in Infrastructure/Persistence.

Application references Domain and EF Core to expose DbSet<Game> through IApplicationDbContext. Infrastructure references Application and provides the implementation; Api composes them. This deliberately accepts EF coupling in Application instead of introducing repository wrappers or a provider-neutral query language. Domain remains independent of EF and HTTP.

### Domain and mapping

Persist Game directly with private mutation and a Result-returning creation method. The domain creation boundary trims and checks required strings and slug syntax so callers cannot bypass invariants. Generate the ID with Guid.CreateVersion7(), use one UTC timestamp for both creation fields, and assign Draft internally. Use DateTimeOffset for timestamps. GameStatus includes Draft and Published, but no publication operation is introduced.

Use extension mappings near the feature in the consuming layer: API request to command and Application Game to response DTO. Map through the domain creation method rather than setting domain state from a transport object. No mapping package or separate persistence entity is needed.

Planning defaults: duplicate names are allowed because only slug uniqueness was requested; whitespace is trimmed before length checks; status serializes as `draft` using existing API conventions. Server-owned fields are absent from the request contract and additional JSON properties retain the framework's existing ignored-property behavior. No new strict unknown-property policy is introduced.

### Validation pipeline

Add a shared Mediator behavior for Result-returning requests, using the existing IFailureFactory<TSelf> contract to produce validation failures without reflection or exceptions. Run registered validators asynchronously and sequentially, aggregate property-associated errors, and short-circuit before invoking the handler. Sequential execution avoids overlapping scoped DbContext operations if validators later need storage access.

CreateGameValidation uses property-level FluentValidation rules to check required trimmed strings, technical length limits, and slug syntax without mutating the command. Technical length constants live in Application and are reused by EF configuration; Domain does not enforce storage/input length limits. The handler calls the domain creation method, optionally checks slug availability for a normal conflict Result, adds the entity through IApplicationDbContext, saves once, and maps the response. Domain checks remain authoritative; the behavior prevents repeated validation orchestration, not domain invariant enforcement. Do not catch arbitrary persistence exceptions in the handler.

### HTTP and errors

POST /games binds a request containing name, slug, image, and nullable mapId, maps it to a command, sends it through Mediator, and returns OutcomeResult with HTTP 200 on success. Keep existing 422 and 409 Result mappings and document the response DTO and errors in OpenAPI using existing conventions.

Use the built-in UseExceptionHandler and AddProblemDetails for unexpected failures, including safe HTTP 500 responses and framework logging. Keep a custom IExceptionHandler only for the recognized slug conflict; it returns false for other exceptions. Disable throwing on binding errors and use built-in status code pages with ProblemDetails for malformed JSON and invalid GUID HTTP 400 responses in all environments. Preserve cancellation handling rather than classifying request cancellation as a slug conflict.

Protect slug with a unique index in SQLite. A precheck improves ordinary conflict handling but cannot guarantee uniqueness. Add an Infrastructure-owned classifier for the specific SQLite unique-constraint failure on Games.Slug, exposed to Api through a narrow contract without exposing SQLite types to Application or Domain. The centralized handler maps that recognized failure directly to 409 ProblemDetails; unrelated constraints, locks, and connection failures remain unexpected 500 errors. Do not convert thrown database failures to Result or leak SQL/provider messages to the response. This narrow seam is justified by provider-specific exception inspection; it is not a repository abstraction.

### Storage and migrations

Map the entity with IEntityTypeConfiguration<Game>, a unique Slug index, required metadata, configured string lengths, nullable MapId, and string conversion for Status. SQLite does not enforce configured maximum lengths automatically, so application/domain checks are required. Do not create a map foreign key or perform a map lookup. Preserve scoped context registration and existing query defaults; adding a new entity explicitly is compatible with NoTracking query defaults.

Generate the initial migration in Infrastructure using Api as the startup project. Add compatible EF design-time tooling and a repository-local EF tool manifest if needed so commands are reproducible. Use configuration/environment connection strings; do not embed machine-specific paths or credentials. Do not use EnsureCreated in the runtime or add startup Migrate calls. A future PostgreSQL provider will need its own migration strategy.

## Risks / Trade-offs

- [Application depends on EF Core] -> Accepted for direct DbContext access; keep provider-specific behavior in Infrastructure.
- [Duplicate requests race] -> Unique index is authoritative; test the provider classifier and simultaneous requests separately from the precheck.
- [SQLite locks during concurrent writes] -> Do not classify locks as duplicates; use a deterministic test of the unique violation and a concurrent test that checks the final row count.
- [Duplicated input and domain checks drift] -> Cover accepted boundaries and rejected inputs through domain and pipeline tests; do not extract speculative validation frameworks.
- [Existing persistence test assumes no entities] -> Update it to verify the Game model and scoped context behavior.
- [A pre-existing local SQLite file was created outside migrations] -> Inspect its contents before applying the migration; recreate only disposable local files with explicit authorization if needed.

## Migration Plan

Generate and inspect the migration and snapshot, including the unique slug index and string status column. Apply explicitly with `dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api`, supplying the GameAtlas connection string through configuration. Document generation and application commands alongside persistence documentation.

Validate applying migrations to an empty temporary SQLite file and reopening it after game creation. For rollback, stop API writes, back up any non-disposable database, and use EF CLI to return to the previous migration; reverting the initial migration removes the Games table and its data. Never automatically delete a database to repair migration failures.
