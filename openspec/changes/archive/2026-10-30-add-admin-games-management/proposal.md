## Why

Games currently opens a foundation page although the backend already supports catalog management. Administrators need dedicated creation and editing pages, including direct links that work without a previously loaded list.

## What Changes

- Replace the admin Games foundation destination with a searchable, sortable, paginated game list and dedicated creation/editing pages.
- Add GET /games/{id}, returning the canonical Game including its ImageAsset.
- Extend the prepared GamesQueryOptions with detail queries seeded from cached lists using the source list timestamp, and reconcile create/update/delete operations with detail and list caches.
- Reuse the Media image picker through a UI library boundary allowed by Nx.
- Support draft creation, metadata editing, publication, unpublication and confirmed cascading deletion.
- Out of scope: public web catalog, map management UI, authentication, uploads inside the picker, backend schema changes and frontend unit/E2E test work.

## Capabilities

### New Capabilities

- `admin-games`: Browse and manage games through dedicated administration pages.

### Modified Capabilities

- `game-catalog`: Retrieve one game by ID.
- `frontend-api-clients`: Seed game detail queries from lists and maintain caches through mutations.
- `admin-shell`: Replace Games foundation navigation with functional management routes.

## Impact

Backend Games endpoints and Application query handlers; focused integration/OpenAPI coverage; generated Games transport; shared games/data-access; a new admin-only Games feature; reusable admin Media picker UI; admin routes and relevant documentation. Existing user changes to GamesQueryOptions are the implementation baseline and must be preserved. No database migration is expected.
