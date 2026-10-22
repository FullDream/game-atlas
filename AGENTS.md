# Project conventions

- Follow the formatting, naming, and code style rules defined in `.editorconfig`. Treat `.editorconfig` as the source of truth.
- Use `camelCase` for private instance fields without an underscore (`_`) prefix.
- Follow standard C# naming conventions for constants, static fields, types, methods, properties, and parameters, unless overridden by `.editorconfig`.
- Do not use Hungarian notation or prefixes that encode a variable's type or scope.
- Preserve established .NET naming conventions, including the `I` prefix for interfaces (e.g., `IService`).
- Prefer expression-bodied members for simple, single-expression implementations when consistent with `.editorconfig`.
- Use partial classes only when the type must be split across files, including required generated declarations. Do not introduce partial solely to generate a regex in a domain entity; use a cached regex instead.
- Insert a blank line before `return` statements, except when `return` is the first statement in a block.
- Insert a blank line after a group of local variable declarations before the next statement.
- Separate adjacent fields or properties with a blank line when either declaration spans multiple lines, including attributes, accessors, or an initializer. Compact single-line declarations may remain grouped.
- Insert a blank line after a completed multiline invocation (including fluent chains and calls with block lambdas) before the next statement. Do not insert blank lines inside a continuous invocation chain or before a closing delimiter when no statement follows.
- Separate control-flow statements and blocks (`if`, `for`, `foreach`, `while`, `do`, `switch`, `try`, etc.) from surrounding statements with blank lines. Do not insert blank lines between related clauses such as `if`/`else`, `try`/`catch`/`finally`, or `do`/`while`.

## Git workflow

- When moving tracked files, use `git mv` where practical. If a move was already performed through another tool, stage both the old and new paths together with `git add -A -- <old-path> <new-path>`.
- Verify moves with `git diff --cached --name-status --find-renames`. Do not leave the new file staged while its old path's deletion remains unstaged. Git detects renames from content similarity rather than storing a separate move operation.

## Backend architecture

- Start with a layered monolith using the Api, Application, Domain, and Infrastructure projects. Group functionality by feature within these projects. Consider separate module projects when feature areas become large enough to justify them.
- Keep domain rules in Domain and use-case orchestration in Application. Keep HTTP concerns in Api and persistence implementations and EF Core configurations in Infrastructure.
- Organize Application operations in feature folders, such as `Games/CreateGame`, with operation-specific handlers and validation classes, such as `CreateGameHandler` and `CreateGameValidation`.
- Keep endpoints thin and group a resource's operations in its `Endpoints.cs` file. Place mapping extension methods in separate files in the consuming layer. Return HTTP results through the existing `OutcomeResult` integration.
- Implement mappings as extension methods in the layer that uses them, near the relevant feature. Preserve inward dependency direction; Domain must not depend on API DTOs or infrastructure.
- Keep DTO declarations and mapping extension classes in separate files.
- Persist domain entities directly with EF Core when the domain and persistence models match. Introduce separate persistence models only when needed.
- Use `IApplicationDbContext` directly for persistence access from Application for now. Do not add a repository layer or generic repositories.

## HTTP updates

- Use PATCH with `Content-Type: application/merge-patch+json` for resource updates, following JSON Merge Patch (RFC 7396: https://www.rfc-editor.org/rfc/rfc7396). Do not use PUT in this project. Reject unsupported update media types with HTTP 415.
- Make every mutable field optional in update requests. Use the existing `Optional<T>` to distinguish an omitted field from an explicitly supplied value, including `null`. Optional presence does not make a domain field nullable.
- Leave omitted fields unchanged. Apply supplied values; `null` removes or clears a value only when the resource permits it. Reject `null` for required fields with HTTP 422. Use nullable wrapped types when necessary to preserve explicit `null` for validation, including value types such as enums.
- Merge supplied object members recursively. Replace arrays as a whole rather than merging their elements.
- Require resource update bodies to be JSON objects. Reject malformed JSON, incompatible JSON types, and unknown or read-only fields with HTTP 400. Reject an empty object with HTTP 422 because at least one mutable field must be supplied.
- Validate all supplied changes and the resulting domain state before saving. Apply a patch atomically; a failed patch must not persist partial changes.

## Validation and errors

- Define errors in separate static classes next to the feature that owns them. Keep domain errors in Domain and application-only errors in Application. Use static readonly members for fixed errors and factory methods for parameterized errors.
- Represent expected validation and business failures with `Result`, rather than exceptions.
- Run Application request validation through a shared validation behavior that returns validation failures as `Result`. Avoid repeating validation orchestration in handlers and endpoints.
- Keep domain invariants enforced in Domain even when request validation checks related input rules.
- Return HTTP errors as `ProblemDetails` through the API result integration or centralized exception handling.
- Handle unexpected library and database exceptions centrally as HTTP 500 responses, without converting them to `Result`. Log exception details and do not expose them to clients.
- Translate specifically recognized database constraint conflicts to their agreed HTTP error responses; do not treat every database exception as a conflict.

## Persistence and data representation

- Use SQLite initially. PostgreSQL is a future addition; do not introduce a second provider preemptively.
- Keep EF Core migrations in Infrastructure, without a separate migrations project. Apply them explicitly through EF CLI rather than automatically at API startup.
- Store enums as strings in the database and serialize them as strings in the API.
