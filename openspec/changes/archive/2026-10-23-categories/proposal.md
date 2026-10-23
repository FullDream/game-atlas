# Proposal

## Why

GameAtlas needs categories within category groups so map markers can later be organized by a named, recognizable icon and color. Categories should follow the existing group CRUD conventions while allowing movement between groups of the same game.

## What Changes

- Add four category operations under `/games/{gameId}/category-groups/{groupId}/categories`: create, list, merge-patch update, and physical delete.
- Give categories a group-scoped unique name, icon reference, color, positive display order, server-generated identity, and timestamps.
- Allow atomic metadata updates and transfers between groups of the same game, with the current group in the route and the destination in `categoryGroupId`.
- Return a read-only `markersCount` of zero until marker functionality exists; do not persist a counter.
- Cascade group and game deletion to categories and document the API and migration.
- Keep marker implementation, file uploads, image format checks, pagination, search, individual-category GET, batch operations, and dedicated reorder endpoints out of scope.

## Capabilities

### New Capabilities

- `categories`: Group-owned categories with validated metadata, scoped name uniqueness, display order, same-game transfers, CRUD, and a read-only marker count.

### Modified Capabilities

None. Existing group and game API contracts remain compatible; category cascade behavior is specified by the new categories capability.

## Impact

- Add Categories features to Domain, Application, and Api, following the established CategoryGroups slices and shared validation, mapping, PATCH, and OutcomeResult integrations.
- Extend `IApplicationDbContext`, EF Core configuration, and SQLite constraint conflict classification; generate an Infrastructure migration for the Categories table.
- Reuse `IApplicationTransaction` for mutations and order allocation without exposing provider APIs to Application.
- Extend domain, validation, HTTP, OpenAPI, persistence, concurrency, and rollback coverage. No new provider, project, repository layer, or external service is required.
