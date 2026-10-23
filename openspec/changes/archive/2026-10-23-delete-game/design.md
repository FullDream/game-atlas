# Design

## Context

See proposal.md for motivation and specs/game-catalog/spec.md for behavior. Games use IApplicationDbContext directly, with a unique slug index and no related EF entities or deletion invariants. Nongeneric Result and OutcomeResult already support empty HTTP 204 responses; the shared metadata convention already documents DELETE success and error responses.

## Goals / Non-Goals

**Goals:** Keep deletion in one feature slice, reuse result/error integration, and distinguish missing rows without a separate existence query.

**Non-Goals:** Introduce a domain deletion method, deletion state, repository, request DTO, or validator without a corresponding business rule. Change shared response metadata or the database schema.

## Decisions

### Delete directly and use the affected row count

Implement DeleteGame as an Application command containing a Guid Id and returning nongeneric Result. DeleteGameHandler filters context.Games by Id and executes ExecuteDeleteAsync with the cancellation token. One affected row means success; zero means DeleteGameErrors.NotFound, a feature-owned static error with ErrorType.NotFound.

ExecuteDeleteAsync persists immediately, so do not call SaveChangesAsync afterward. A single primary-key-filtered statement needs no additional application transaction. It deletes no tracked entity and makes no other changes.

Alternative: load the entity with AsTracking, call Remove, and save. This matches the update persistence pattern but adds an unnecessary read and a race between reading and deleting. There are no domain deletion rules, tracked changes, or domain events requiring entity loading. Revisit this choice if such requirements appear.

The affected-row check also makes repeated or competing deletes straightforward: once the row is gone, a later delete returns NotFound. Do not add a separate AnyAsync check.

### Reuse the existing HTTP boundary

Add DELETE `/{id}` in Api/Games/Endpoints.cs, with operationId DeleteGame and summary "Delete game". Bind id directly as Guid and send the command through IMediator. Return nongeneric OutcomeResult so success becomes 204 and application errors become ProblemDetails. No request DTO or mapping extension is needed for a single route argument.

Keep the route template consistent with PATCH rather than adding a GUID route constraint: malformed GUID values should reach binding and produce 400. A syntactically valid but missing identifier, including Guid.Empty, follows the 404 rule. No standalone validator is required.

Alternative: return TypedResults.NoContent directly. Existing OutcomeResult already supplies both runtime behavior and response metadata, so a second result path would duplicate integration.

### Keep deletion outside the domain entity

There is no domain state transition to represent: the row is removed for either status. Do not add Game.Delete, IsDeleted, timestamps, or status restrictions. A deleted slug becomes reusable through the existing unique index without cleanup code. Image and map identifiers are scalar values; deletion does not attempt external resource cleanup.

## Risks / Trade-offs

- ExecuteDeleteAsync bypasses tracked entities and SaveChanges interceptors: keep the handler isolated from tracked mutations and test storage failures with a command interceptor or an equivalent real database failure, not only a SaveChanges interceptor.
- Physical deletion cannot be undone: restoration and audit storage are outside this change; deployments must not imply rollback can restore deleted rows.
- Future relationships or deletion business rules may require a different persistence path: revisit deletion when those models are introduced rather than preemptively adding cascade policies.
- Concurrent updates have no version checks today: retain the current concurrency policy; do not introduce ETags or new update error handling in this change.

## Migration Plan

No schema migration or data backfill is required. Deploy the additive endpoint with the application. Reverting the application change removes the endpoint but cannot restore previously deleted records.
