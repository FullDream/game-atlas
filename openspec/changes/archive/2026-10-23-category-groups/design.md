# Design

## Context

See proposal.md for motivation and specs/category-groups/spec.md for the contract. The current solution has Api, Application, Domain, and Infrastructure projects. Games use thin grouped Minimal API endpoints, Mediator commands/queries, FluentValidation, Result/OutcomeResult, and IApplicationDbContext with EF entities persisted directly. Tracking is disabled by default.

Category groups introduce the first real child relationship to Game and independently editable positive display orders. The existing game DELETE uses ExecuteDeleteAsync, so dependent cleanup must work at database level rather than through entity loading. Existing shared result metadata already supports 200 arrays and empty 204, and PATCH requests use Optional<T> with shared merge-patch media handling.

## Goals / Non-Goals

**Goals:** Preserve feature slices and inward dependencies; make ownership, unique names, append allocation, and single-group update atomicity enforceable on SQLite; retain the existing Games HTTP contracts.

**Non-Goals:** Add repositories, modules, a generic ordering framework, a generic transaction pipeline, or future Category entities/navigation placeholders. Require Game to load its entire group collection for CRUD. Guarantee detection of concurrent order-only edits without a version token.

## Decisions

### 1. Keep a separate CategoryGroup entity and feature

Add Domain/CategoryGroups/CategoryGroup with immutable Id and GameId, Name, Order, CreatedAt, UpdatedAt, and an internal-purpose persisted NormalizedName property excluded from DTOs. Creation generates UUID v7 and equal UTC timestamps. Domain methods enforce nonempty trimmed names and strictly positive integer order, return Result, and report whether a real change occurred. One domain Update validates name and order before applying either, and changes UpdatedAt once only when values actually change.

Place limits and request validation in Application/CategoryGroups following Games. Add CreateCategoryGroup, ListCategoryGroups, UpdateCategoryGroup, DeleteCategoryGroup slices. Share CategoryGroupDto and separate mapping extensions near the feature. Keep operation-specific static errors near their owners and domain errors in Domain.

The Game foreign key establishes ownership without making every group operation load a Game aggregate. Do not add a Game.Categories/CategoryGroup.Categories collection until categories exist. A future Category can have a required CategoryGroupId and a normal many-to-one relationship; no schema for that future feature belongs here.

Alternative: store all groups through a Game collection. That would require loading unrelated groups for simple name changes and enlarge the existing aggregate without a business invariant requiring it.

### 2. Enforce scoped names with a normalized key

Use the same deterministic normalization for entity updates and conflict lookups: trim display input, then compute NormalizedName using ToUpperInvariant. Retain the trimmed display spelling in Name. This covers ordinary Latin and Cyrillic case without culture-dependent database behavior; it does not introduce accent folding, transliteration, internal whitespace collapsing, or an extra Unicode normalization policy.

Configure a unique composite index on (GameId, NormalizedName). Query it for friendly preflight conflicts, excluding the current ID on rename. A rename may change displayed casing without conflicting with itself. The database constraint is authoritative for races.

Extend IPersistenceFailureClassifier and its SQLite implementation with precise recognition of this constraint, and add category-group name-conflict HTTP handling parallel to GameSlugConflictExceptionHandler. Expected preflight failures use Result; unexpected exceptions remain exceptions and use the centralized safe 500 path. Translate only the recognized name constraint to 409 with a name error.

Alternative: SQLite NOCASE alone. It compares ASCII case only and would not enforce the agreed Cyrillic behavior. Custom collations require consistent registration on every connection, including tooling; a stored normalized key with a normal unique index is easier to inspect and migrate.

References: [SQLite collation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/collation), [ToUpperInvariant](https://learn.microsoft.com/en-us/dotnet/api/system.string.toupperinvariant).

### 3. Persist ownership and ordering explicitly

Add DbSet<CategoryGroup> to IApplicationDbContext and ApplicationDbContext, and a configuration in Infrastructure. Use a required GameId foreign key with OnDelete(Cascade), the existing UTC timestamp converter, and an index on (GameId, Order) for ordered reads. The order index need not be unique: writes are serialized as described below, and repeated order values are explicitly supported.

Generate the migration and model snapshot with EF CLI. Existing games need no backfill because the new table starts empty. Game DELETE can remain a single statement; the database cascade also removes groups when no entities are tracked.

### 4. Use explicit short transactions for group mutations

Expose BeginTransactionAsync on IApplicationDbContext, returning IApplicationTransaction, an Application-owned IAsyncDisposable contract with only CommitAsync. Infrastructure wraps EF Core's IDbContextTransaction and delegates commit and asynchronous disposal. Disposal without commit rolls back; handlers use await using so early returns and exceptions release the transaction. Keep DatabaseFacade and EF transaction types inside Infrastructure. DbSet remains the existing agreed EF dependency; no repository, generic unit-of-work service, or new package is introduced.

Begin the transaction before parent/group checks and reads used to calculate a write. Use the provider's normal non-deferred SQLite transaction, which takes the write reservation before mutation decisions, and keep it short. In particular:

- Create: check the game, check the normalized name, use the supplied positive Order, or allocate max(Order) + 1 (starting at 1), add, save, and commit.
- Update: scope by both IDs, check the name excluding self, validate and apply name/order in one domain mutation, and save only a real change before commit.
- Delete: scope by both IDs and remove the group, keeping survivor positions and timestamps; missing rows return NotFound.

Do not silently opt into a deferred read-then-write transaction, which can require retrying the entire operation when another writer intervenes. Competing creates, deletes, game deletion, and updates are serialized by SQLite's writer locking. A storage timeout still uses the safe 500 path; no new retry service or conflict classification for arbitrary lock failures is added.

Alternative: default max-position allocation outside a transaction. Two creates can choose the same end position when default order is computed concurrently. A SaveChanges transaction alone does not protect those earlier reads.

Reference: [SQLite transactions and deferred-transaction limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).

### 5. Edit order on an individual group

Order is an int greater than zero and may repeat. POST and PATCH use Optional<int?> to distinguish omission from explicit null: null is invalid; omission on POST appends and omission on PATCH preserves. List orders by Order, CreatedAt, and Id. Updates affect only their target; no renumbering, membership checks, batch API, or dedicated reorder endpoint is included. If max(Order) is int.MaxValue, default append returns 422 asking for an explicit order.

One dragged group can be updated with one PATCH. Adjacent integer keys can require several independent client updates for an exact placement; those updates do not form an atomic batch. No automatic repositioning is promised.

### 6. Preserve the API conventions

Add Api/CategoryGroups/Endpoints.cs under `/games/{gameId}/category-groups` with the four operation IDs in the spec. Keep request DTOs, DTOs, and mapping extensions in separate files. POST accepts name and optional order, ignoring extra creation properties as Games creation does. PATCH accepts optional name and optional order. Disallow unknown PATCH members, including server-owned fields. Bind route IDs directly as Guid; malformed values return 400, while a valid missing or foreign ID returns 404.

Use OutcomeResult<CategoryGroupDto> for create/update, OutcomeResult<IReadOnlyCollection<CategoryGroupDto>> for list, and nongeneric OutcomeResult for delete. Return JSON arrays without PaginatedResult. Reuse WithMergePatch and FluentValidation-to-OpenAPI integration; register create and update request/command validation mappings.

Only name and order may change in ordinary PATCH. Returning current DTOs for no-ops matches Games. Parent Game.UpdatedAt is not changed by group operations; the timestamp belongs to that resource's own metadata.

HTTP response expectations:

| Operation | Success | Specific expected failures |
| --- | --- | --- |
| Create | 200 DTO | 404 missing game; 409 name conflict; 422 name validation |
| List | 200 array | 404 missing game |
| Update | 200 DTO | 404 missing/foreign resource; 409 name conflict; 422 empty patch/invalid name or order |
| Delete | 204 | 404 missing/foreign resource or repeated deletion |

All operations retain binding 400 and safe 500 responses; the PATCH operation also document 415. Shared metadata may include its established additional error responses. No global metadata rewrite is needed.

## Risks / Trade-offs

- Unpaginated reads scale with group count: accepted for the agreed complete-list UI.
- SQLite serializes writers database-wide: keep transactions short and perform no HTTP/external work inside them. Revisit transaction strategy when PostgreSQL is introduced.
- Deletion leaves position gaps: preserve them for survivor timestamp stability; no operation automatically renumbers, and no-op requests preserve stored positions.
- Case normalization depends on the application's Unicode casing implementation: compute keys consistently, keep them out of request bodies, and test Latin and Cyrillic cases through real SQLite connections.
- Cascade deletion is irreversible: document the dependent deletion and test it through the current ExecuteDeleteAsync-based game endpoint.

## Migration Plan

Generate and review an additive Infrastructure migration; test migration from the current schema with existing games, as well as a fresh database. Apply explicitly with EF CLI before running the updated API. No automatic startup migration or data backfill is introduced.

Rollback requires reverting the application and applying the migration down only when loss of all category-group records is acceptable. Existing Games rows remain intact; deleted games and cascaded groups cannot be restored by rollback.
