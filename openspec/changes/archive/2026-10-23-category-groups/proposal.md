# Proposal

## Why

Games need ordered category groups before categories and map markers can be organized. Clients need to create, browse, rename, delete, and edit display order on individual groups independently for each game.

## What Changes

- Add unauthenticated CRUD under `/games/{gameId}/category-groups`, including a complete unpaginated list; individual group retrieval is out of scope.
- Expose id, gameId, name, order, createdAt, and updatedAt; keep identity, ownership, and timestamps server-controlled.
- Trim names, require 1–200 characters, and enforce case-insensitive name uniqueness within each game, with HTTP 409 for conflicts.
- Append new groups without an explicit order to the end; list in persisted order; physically delete groups and cascade their deletion when the owning game is deleted.
- Allow positive integer order on creation and ordinary PATCH; omit order at creation to append from 1, and permit duplicate order values.
- List by order, createdAt, then id. A single-group change never shifts other groups; no dedicated reorder or batch endpoint is included.
- Preserve timestamps for unchanged values and order; change only the target group when its name or order changes.
- Out of scope: categories and their navigation properties, markers, multiple maps, pagination, search, arbitrary sorting, authorization, restoration, moving groups between games, and ETag/version-based concurrency.

## Capabilities

### New Capabilities

- `category-groups`: Game-owned category group metadata, CRUD, unique names, persisted display ordering, and lifecycle with the owning game.

### Modified Capabilities

None. Existing game HTTP contracts remain unchanged; category-group ownership adds dependent-row cleanup to successful game deletion.

## Impact

- Domain: add CategoryGroup with creation, rename, and position-change invariants.
- Application: add CategoryGroups feature slices, shared DTO/mapping, context access, and feature-owned errors using existing Result and validation integration.
- Infrastructure: add group configuration and an EF CLI-generated SQLite migration with a game foreign key, cascade delete, and name uniqueness constraint; support explicit transactions for ordering and append allocation.
- API: add one resource Endpoints.cs, separate request DTOs/mapping, and name-conflict exception handling using the established persistence classifier pattern.
- Tests and docs: cover domain rules, migration/persistence, HTTP/OpenAPI contracts, atomic single-group updates, and unchanged Games behavior. No new projects or packages are needed.
