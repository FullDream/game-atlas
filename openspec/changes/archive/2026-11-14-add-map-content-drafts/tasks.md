# Tasks

## 1. Domain & Persistence Schema (`Map`, `CategoryGroup`, `Category`, `Marker`)

- [x] 1.1 Unify publication status into a shared `PublicationStatus` enum (`Draft = 0`, `Published = 1` with camelCase `JsonStringEnumConverter`) in `src/GameAtlas.Domain/PublicationStatus.cs` for `Game`, `Map`, `CategoryGroup`, `Category`, and `Marker`, defaulting to `Draft` when not explicitly specified; verify `dotnet build src/GameAtlas.Domain` succeeds.
- [x] 1.2 Configure required string-converted `Status` (`HasConversion<string>()`, `HasMaxLength(32)`) in `MapConfiguration`, `CategoryGroupConfiguration`, `CategoryConfiguration`, and `MarkerConfiguration`, generate an EF Core migration that adds non-null `Status` columns with default `"Published"` for existing rows, and verify `dotnet build src/GameAtlas.Infrastructure` succeeds.

## 2. Backend API: Maps, Game Map Summaries & Bulk Publish

- [x] 2.1 Include `status` on `GameMapDto` (`src/GameAtlas.Api/Games/GameMapDto.cs` and game projections) and on `MapDto` (`src/GameAtlas.Api/Maps/MapDto.cs`), and update `CreateMap` and `UpdateMap` commands, validators, endpoints, and OpenAPI schema mappings to accept optional `status` (`draft` | `published`, defaulting to `draft` on `POST` and rejecting `null` with 422 on `POST`/`PATCH` without cascading to child entities); verify with integration tests in `tests/GameAtlas.Api.Tests`.
- [x] 2.2 Implement `POST /games/{gameId}/maps/{mapId}/publish` (`PublishMap` under `src/GameAtlas.Api/Maps/PublishMap/`) to atomically transition the scoped map and all of its draft category groups, categories, and markers to `Published` (advancing `UpdatedAt` only on records whose status changed) and return `200 OK` with `MapDto`; verify with integration tests covering draft map publish, published map with draft children, idempotent no-op publish, and 404 scope checks.

## 3. Backend API: Category Groups, Categories & Markers Status

- [x] 3.1 Add `status` (`draft` | `published`) to `CategoryGroupDto` and `CategoryDto`, and update `CreateCategoryGroup`, `UpdateCategoryGroup`, `CreateCategory`, and `UpdateCategory` commands, validators, handlers, and OpenAPI documentation (`src/GameAtlas.Api/Categories/README.md`) so `POST` defaults omitted `status` to `draft` and `PATCH` updates only the target entity's status (rejecting `null` with 422); verify with integration tests in `tests/GameAtlas.Api.Tests`.
- [x] 3.2 Add `status` (`draft` | `published`) to `MarkerDto`, and update `CreateMarker` and `UpdateMarker` commands, validators, handlers, and OpenAPI metadata so `POST` defaults omitted `status` to `draft` and `PATCH` updates only the marker's status (rejecting `null` with 422); verify with integration tests in `tests/GameAtlas.Api.Tests`.

## 4. Frontend Shared UI (`hlm-switch`) & OpenAPI Client Generation

- [x] 4.1 Generate `@spartan-ng/helm/switch` (`libs/shared/ui-helm/switch`) using the Angular/Nx generator, export it via `@game-atlas/shared/ui-helm/switch` in `frontend/tsconfig.base.json`, and verify `pnpm --dir frontend exec nx build` / typecheck passes for the library.
- [x] 4.2 Regenerate frontend OpenAPI clients (`pnpm --dir frontend run generate-api`) against the updated backend OpenAPI specification and update `MapsQueryOptions` (`frontend/libs/maps/data-access`) to expose `publish(gameId, mapId)` with optimistic `setQueryData` status updates for map, category groups/categories, and markers (and snapshot rollback in `onError`, without query invalidation); verify generated models include `status` on `GameMap`, `Map`, `CategoryGroup`, `Category`, and `Marker`.

## 5. Map Viewer Canvas Draft Indicators & Multiline Tooltip (`libs/maps/ui`)

- [x] 5.1 Extend `MapMarker` in `frontend/libs/maps/ui/src/lib/map-viewport-types.ts` with `draftStates?: readonly MarkerDraftState[]`, update `map-markers.ts` to include the primary draft state in icon cache keys, draw a top-right corner dot (`#F59E0B` for `'marker'`, `#0EA5E9` for `'category'`, `#8B5CF6` for `'group'` with contrasting outline), and update `#showTooltip` to render a second line listing all active draft levels (e.g. `Draft: marker, category, group`).

## 6. Admin Game Cards & Game Maps Tab Status UI

- [x] 6.1 Update `GameCard` (`frontend/libs/games/feature-list/src/lib/game-card/`) to display a compact top-right badge with `matVisibilityOffRound` and `hlmTooltip="Draft"` on map preview thumbnails when `map.status === 'draft'` (showing no badge when `published`), and verify with component tests in `libs/games/feature-list`.
- [x] 6.2 Update `FeatureList` (`frontend/libs/maps/feature-list/`) to render Spartan `hlmBadge` status badges (`default` for `Published`, `secondary` for `Draft`) on each map card, and add a `Published` `hlm-switch` (defaulting to unchecked/`draft` on creation) to `MapEditorDialog` (`map-editor-dialog`) for both creating and editing maps.

## 7. Admin Map Editor: Header Publish Action, Unified Settings Form, Category/Marker Forms & Badges

- [x] 7.1 Refactor `MapSettings` (`frontend/libs/maps/feature-editor/src/lib/map-settings/`) to remove the per-input inline save icon buttons, add a `Published` `hlm-switch`, and submit changed `name`, `slug`, and `status` fields together via a single `Save changes` button; verify with component tests in `libs/maps/feature-editor`.
- [x] 7.2 Update `CategoryEditorDialog` and `MapCategories` (`frontend/libs/maps/feature-editor/src/lib/map-categories/`) to include a `Published` `hlm-switch` (defaulting to `draft` on create), render `hlmBadge` `Draft` badges on draft group and category rows in the tree, and render `hlmBadge` `Draft` badges next to draft groups in the category group `hlm-select`.
- [x] 7.3 Update `MarkerEditor` and `MapMarkers` (`frontend/libs/maps/feature-editor/src/lib/marker-editor/` and `map-markers/`) to include a `Published` `hlm-switch` in single-create, edit, and repeated-placement template forms, render `hlmBadge` `Draft` badges on draft groups/categories inside the marker category `hlm-select`, and show an `hlmBadge` `Draft` badge on marker accordion rows when the marker, its category, or its group is `draft`.
- [x] 7.4 Replace the `Back to maps` portal button in `FeatureEditor` (`frontend/libs/maps/feature-editor/src/lib/feature-editor.html` and `feature-editor.ts`) with the `Publish` / `Publish changes` button wired to `MapsQueryOptions.publish` (with `onError` toast in `publishMap()`), and compute per-marker `draftStates` (`marker`, `category`, `group`) in `map-marker-presentation.ts` for `ui-map-viewport`.
