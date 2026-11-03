# Backend conventions

Applies to backend implementation and backend tests. Read the [system architecture](architecture.md), [C# style](styleguide.md#c) and, for HTTP behavior, [API conventions](api-conventions.md). Paths in this document are relative to the repository root.

## Feature organization

- Organize Application operations in feature folders, such as `Games/CreateGame`, with operation-specific handlers and validation classes, such as `CreateGameHandler` and `CreateGameValidation`.
- Keep endpoints thin and group a resource's operations in its `Endpoints.cs` file. Place mapping extension methods in separate files in the consuming layer. Return HTTP results through the existing `OutcomeResult` integration.
- Implement mappings as extension methods in the layer that uses them, near the relevant feature. Preserve inward dependency direction; Domain must not depend on API DTOs or infrastructure.
- Keep DTO declarations and mapping extension classes in separate files.
- Keep mapping extensions synchronous and pure: accept materialized values and construct DTOs without database access, injected services, or other I/O. Execute reads explicitly in the consuming operation handlers.
- Persist domain entities directly with EF Core when the domain and persistence models match. Introduce separate persistence models only when needed.
- Use `IApplicationDbContext` directly for persistence access from Application for now. Do not add a repository layer or generic repositories.

## Validation and errors

- Define errors in separate static classes next to the feature that owns them. Keep domain errors in Domain and application-only errors in Application. Use static readonly members for fixed errors and factory methods for parameterized errors.
- Represent expected validation and business failures with `Result`, rather than exceptions.
- Run Application request validation through a shared validation behavior that returns validation failures as `Result`. Avoid repeating validation orchestration in handlers and endpoints.
- Keep domain invariants enforced in Domain even when request validation checks related input rules.

## Persistence

- Use SQLite initially. PostgreSQL is a future addition; do not introduce a second provider preemptively.
- Keep EF Core migrations in Infrastructure, without a separate migrations project. Apply them explicitly through EF CLI rather than automatically at API startup.
- Store enums as strings in the database.

## Category feature ownership

Category and CategoryGroup share Domain/Categories. Group use cases live under Application/Categories/Groups, and Api/Categories/Endpoints.cs maps both route families. Markers remain separate. Pure DTO mapping stays separate from tree retrieval and placement orchestration.
