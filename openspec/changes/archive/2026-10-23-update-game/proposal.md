# Proposal

## Why

Games can currently be created and listed but cannot be corrected or published. Partial updates must preserve omitted values and protect identity and timestamps while enforcing the same game invariants as creation.

## What Changes

- Add unauthenticated PATCH `/games/{id}` accepting `application/merge-patch+json` and `application/json` with identical merge-patch semantics, with optional name, slug, image, mapId, and status.
- Distinguish omission from explicit null: preserve omitted values, allow clearing mapId, and reject null for required fields.
- Allow publication and returning to draft through status updates.
- Reject empty patches, unknown fields, and client-supplied identity or timestamps; return the complete existing GameDto on success.
- Add a domain `Game.Update` method to validate and apply changes atomically, preserving UpdatedAt for normalized no-ops.
- Enforce slug uniqueness during updates, including persistence races, and return ProblemDetails for failures.
- Extend existing Optional and FluentValidation OpenAPI support to describe the update contract accurately.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-catalog`: Add partial game updates, publication transitions, update-specific validation and error responses, and persistence guarantees.

## Impact

- Domain: Game update behavior and domain-owned status errors.
- Application: Games/UpdateGame command, handler, validation, and application-owned errors using IApplicationDbContext and shared OptionalRuleExtensions.
- Api: Resource Endpoints.cs, separate request and mapping files, PATCH media-type enforcement, OpenAPI integration, and usage examples.
- Infrastructure: Reuse existing entity mapping, timestamp storage, unique index, and conflict classification; no schema migration is expected.
- Tests: Domain invariants, conditional validation, SQLite-backed API behavior, persistence failures, and OpenAPI regression coverage.
- Out of scope: PUT, JSON Patch, a generic JSON document patch engine, authentication, map existence checks, new publication prerequisites, optimistic concurrency/ETags, PostgreSQL, and changes to existing POST/GET behavior. Global PATCH conventions are already committed in AGENTS.md and openspec/config.yaml.
