# Design

## Context

See proposal.md for motivation. ASP.NET Core 10.0.13 creates component names from CLR type names and prefixes union child identifiers with their parent identifier. Concrete media DTOs also declare themselves as their only derived type so that direct serialization emits `type`. `AddOptionalSupport` already owns a schema-reference callback that must continue to inline `Optional<T>`. Games and Media clients use explicit uncached HTTP generation through Orval.

## Goals / Non-Goals

**Goals:** Keep naming and media schema normalization in the API OpenAPI layer; verify all local references, union mappings, literal values, nullability, and serialization contracts. Migrate all existing frontend consumers in the same change.

**Non-Goals:** Alter CLR DTO declarations, serializers, HTTP payloads, endpoints, dependencies, build-time generation, or frontend test coverage.

## Decisions

1. Centralize public schema naming with recursive generic argument handling and preserve optional inlining. Renaming CLR DTOs would cause unnecessary backend churn and would not address union prefixes.
2. Normalize media components after schema generation through a document transformer. Map known concrete media variants to canonical components, flatten their single-variant wrappers, and rewrite schema references and discriminator mappings together. Validate equivalent concrete shapes before merging duplicates so contextual schema differences cannot be discarded silently. Keep true multi-variant unions and nullable composition intact. A title-only change would leave duplicate generated frontend models, so it is insufficient.
3. Retain concrete media serialization attributes and required literal `type` properties. Removing attributes would change direct serialization; adding ordinary discriminator properties would risk duplicate metadata in union responses.
4. Regenerate the two existing domain clients through their Nx targets against the updated running backend. Change handwritten imports, exports, and annotations; do not hand-edit generated output. No Maps client is introduced.

## Risks / Trade-offs

- Schema identifier changes break generated source names → regenerate Games and Media clients and migrate consumers atomically.
- Rewriting references can leave dangling mappings or lose nullability → contract tests traverse references, assert exact union targets, preview nullability, required literals, and PATCH semantics.
- Framework changes can alter generated concrete shapes → fail visibly on unexpected or conflicting media schemas rather than merging by name alone.
- A generated model can collide with browser type names → use an explicit local import alias where needed without restoring DTO names.

## Migration Plan

Implement naming and normalization, update backend contract tests, serve the updated API on an available local port, regenerate both existing clients over HTTP, migrate consumers, and update conventions. Validate with backend build and focused contract/serialization tests, frontend lint and production builds, OpenSpec validation, documentation checking, and whitespace checking. Rollback reverts the schema and client migration together.
