# Proposal

## Why

Games already support `draft` and `published` states, but maps, category groups, categories, and markers become immediately public-ready upon creation with no way to stage work-in-progress content. Map authors need to prepare maps, categories, category groups, and markers in `draft` state, visually distinguish draft content in the admin workspace, toggle publication per entity, or publish an entire map's content in one action.

## What Changes

- Add publication `status` (`draft` | `published`) to `Map`, `CategoryGroup`, `Category`, and `Marker` domain entities, database schema (with migration defaulting existing rows to `published`), and API response DTOs (`MapDto`, `GameMapDto`, `CategoryGroupDto`, `CategoryDto`, `MarkerDto`).
- Accept optional `status` (`draft` | `published`, defaulting to `draft` when omitted) on `POST` creation requests for maps, category groups, categories, and markers.
- Accept optional `status` (`draft` | `published`, rejecting explicit `null` with HTTP 422) on `PATCH` requests for maps, category groups, categories, and markers; updating an entity's `status` via `PATCH` changes only that entity without cascading to child entities.
- Add a dedicated bulk publish endpoint `POST /games/{gameId}/maps/{mapId}/publish` that atomically transitions the map and all of its draft category groups, categories, and markers to `published`, updating `updatedAt` only on records whose status changed.
- Add `@spartan-ng/helm/switch` (`hlm-switch`) to the shared UI library and expose a publication status switch across map creation/edit dialogs, the map editor `Settings & Tools` form, category/group creation and edit dialogs, and marker creation/edit/repeated-placement forms.
- Refactor the map editor `Settings & Tools` form to replace per-field inline save icon buttons with a single form-level `Save changes` submit button covering `name`, `slug`, and `status`.
- Replace the `Back to maps` header action in the map editor with a `Publish` / `Publish changes` action button wired to `POST /games/{gameId}/maps/{mapId}/publish`.
- Render draft status indicators across the admin UI:
  - Colored corner indicator dots and multiline hover tooltips on map canvas markers when the marker, its category, or its category group is `draft`.
  - `Draft` badges on draft groups, categories, and markers in the map editor left panel and inside category/group select dropdowns.
  - `Published` and `Draft` status badges on map cards in the game editor `Maps` tab.
  - A compact top-right draft icon badge with a `Draft` tooltip on draft map preview thumbnails inside game catalog cards.

### Out of Scope

- Public web app (`apps/web`) filtering or read-only viewer restrictions based on `status` (will be addressed when public map consumption is implemented).
- Cascading status changes when toggling an individual map, category group, or category via `PATCH`.
- Publication status on map styles (`MapStyle`) or marker media items.
- Bulk unpublish ("revert all to draft") endpoints or actions.

## Capabilities

### New Capabilities

### Modified Capabilities
- `game-catalog`: Include `status` (`draft` | `published`) in lightweight map summaries (`GameMapDto`) returned with game responses.
- `maps`: Add `status` (`draft` | `published`, default `draft` on create) to map creation, retrieval, and merge patch contracts, backfill existing maps as `published`, and add `POST /games/{gameId}/maps/{mapId}/publish` to publish a map and all of its draft category groups, categories, and markers atomically.
- `category-groups`: Add `status` (`draft` | `published`, default `draft` on create) to category group creation, retrieval, and merge patch contracts.
- `categories`: Add `status` (`draft` | `published`, default `draft` on create) to category creation, retrieval, and merge patch contracts.
- `markers`: Add `status` (`draft` | `published`, default `draft` on create) to marker creation, retrieval, and merge patch contracts.
- `map-viewer`: Support optional draft state indicators (corner colored dot by draft source and second-line hover tooltip text) on rendered map canvas markers.
- `admin-games`: Display a compact top-right draft icon badge with a `Draft` tooltip on draft map preview thumbnails in game catalog cards.
- `admin-maps`: Add `hlm-switch` publication controls across map, category/group, and marker forms, unify the `Settings & Tools` map form under a single `Save changes` button, replace the map editor header `Back to maps` button with `Publish` / `Publish changes`, and show draft badges/indicators across map cards, category trees, marker lists, and select dropdowns.

## Impact

- **Backend (`src/GameAtlas.Domain`, `src/GameAtlas.Infrastructure`, `src/GameAtlas.Api`)**:
  - Domain entities (`Map`, `CategoryGroup`, `Category`, `Marker`), EF Core entity configurations, and a new SQLite migration adding `Status` columns.
  - `Games`, `Maps`, `CategoryGroups`, `Categories`, and `Markers` vertical slices, DTOs, validators, OpenAPI metadata, and a new `PublishMap` endpoint in `Maps`.
  - Integration tests in `tests/GameAtlas.Api.Tests`.
- **Frontend (`frontend/`)**:
  - Regenerated Orval OpenAPI clients and models in `libs/games/data-access`, `libs/maps/data-access`, `libs/categories/data-access`, and `libs/markers/data-access`.
  - New Spartan UI switch helm wrapper in `libs/shared/ui-helm/switch`.
  - Admin UI updates in `libs/games/feature-list`, `libs/maps/feature-list`, `libs/maps/feature-editor`, and `libs/maps/ui`.
