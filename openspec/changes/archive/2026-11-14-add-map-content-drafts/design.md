# Design

## Context

Currently, only `Game` has a publication lifecycle (`GameStatus.Draft` and `GameStatus.Published`, stored as strings in SQLite and serialized as camelCase `'draft' | 'published'` via `JsonStringEnumConverter<GameStatus>(JsonNamingPolicy.CamelCase)`). `Map`, `CategoryGroup`, `Category`, and `Marker` entities have no status property. See `proposal.md` for motivation and scope.

On the frontend, the admin workspace uses Spartan UI (`@spartan-ng/brain` + `@spartan-ng/helm` wrappers under `frontend/libs/shared/ui-helm`), TanStack Query Angular with handwritten `*-query.options.ts` classes, and Orval-generated OpenAPI clients per domain (`games`, `maps`, `categories`, `markers`). Map markers on the canvas are rendered with OpenLayers (`ol/style/Style` and `ol/style/Icon` generated via an offscreen 2D canvas in `frontend/libs/maps/ui/src/lib/map-marker-presentation.ts`, with hover tooltips in `map-markers.ts`).

## Goals / Non-Goals

**Goals:**

- Introduce a consistent `Draft` / `Published` status model across `Map`, `CategoryGroup`, `Category`, and `Marker` in the domain, database, and OpenAPI schemas without breaking existing `GameStatus` conventions.
- Support optional `status` on creation (`POST`, defaulting to `draft`) and individual entity updates (`PATCH`, without cascading to children).
- Provide an atomic, single-round-trip bulk publish operation (`POST /games/{gameId}/maps/{mapId}/publish`) that transitions a map and all of its draft groups, categories, and markers to `published`.
- Deliver cohesive admin UI controls (`hlm-switch`, unified `Settings & Tools` form, map editor header `Publish` / `Publish changes` action) and clear visual indicators for draft state across cards, trees, selects, and the OpenLayers canvas.

**Non-Goals:**

- Filtering out `draft` maps, categories, groups, or markers on existing unauthenticated API endpoints (public web app consumption is not yet built).
- Cascading status changes when toggling an individual entity via `PATCH`.
- Adding publication status to `MapStyle` or media assets.

## Decisions

### 1. Publication Status Enum and Serialization

- **Decision**: Unify all entity publication statuses (`Game`, `Map`, `CategoryGroup`, `Category`, `Marker`) into a single shared enum `PublicationStatus` in the root `GameAtlas.Domain` namespace (`src/GameAtlas.Domain/PublicationStatus.cs`) with members `Draft = 0` and `Published = 1`, decorated with `[JsonConverter(typeof(JsonStringEnumConverter<PublicationStatus>))]` using `JsonNamingPolicy.CamelCase` so Orval generates a single `PublicationStatus` (`'draft' | 'published'`) string union type.
- **Rationale**: Eliminates duplicate two-state enums across domains while keeping API contracts and OpenAPI schemas uniform. Storing as string (`HasConversion<string>()`, `HasMaxLength(32)`) in EF Core keeps SQLite rows human-readable across `Games`, `Maps`, `CategoryGroups`, `Categories`, and `Markers`.
- **Alternatives considered**:
    - _Per-entity status enums (`GameStatus`, `MapStatus`, `CategoryGroupStatus`, `CategoryStatus`, `MarkerStatus`)_: Rejected in favor of a single `PublicationStatus` enum to avoid redundant identical enums in both backend and generated frontend clients.
    - _Boolean `isPublished` flag_: Rejected because `status: 'draft' | 'published'` is already established and extensible.

### 2. Creation Default (`draft`) vs. Individual `PATCH` (Non-Cascading) vs. Dedicated Bulk Publish Endpoint

- **Decision**:
    1. `POST` requests for `Map`, `CategoryGroup`, `Category`, and `Marker` accept `Optional<...Status> Status` (or nullable with null rejected as 422 when explicitly supplied in JSON), defaulting to `Draft` when omitted.
    2. `PATCH` requests for `Map`, `CategoryGroup`, `Category`, and `Marker` accept `Optional<...Status> Status` using the existing `Optional<T>` merge-patch binder (`!Status.HasValue` -> preserve; `Status.Value is null` -> HTTP 422; defined value -> update only that entity).
    3. Bulk publishing uses a dedicated action endpoint: `POST /games/{gameId}/maps/{mapId}/publish` (operation ID `PublishMap`, tagged under `Maps`, no request body required).
- **Rationale**:
    - Separating individual `PATCH` from bulk publish avoids unexpected side effects when an author toggles a single map or group switch in a settings form, while giving the map editor header button (`Publish` / `Publish changes`) a single atomic transaction on the backend.
    - Inside `PublishMapHandler`, the handler verifies map ownership under `gameId`, then updates the `Map` (if `Draft`) and all owned `CategoryGroups`, `Categories`, and `Markers` currently in `Draft` to `Published` with `UpdatedAt = utcNow` within the SQLite write lock and transaction, leaving already-`Published` rows and their `UpdatedAt` timestamps untouched, and returns `200 OK` with `MapDto`.
- **Alternatives considered**:
    - _Cascading on `PATCH /games/{gameId}/maps/{mapId}` when `status: 'published'`_: Rejected because the user explicitly wants individual switch toggles in forms to affect only that entity, while the header button publishes everything on the map.
    - _Client-side fan-out of N PATCH requests_: Rejected due to partial failure risk, slow sequential SQLite writes, and unnecessary network chatter.

### 3. Map Canvas Draft Indicator Rendering (`libs/maps/ui`)

- **Decision**:
    - Extend `MapMarker` in `frontend/libs/maps/ui/src/lib/map-viewport-types.ts` with `readonly draftStates?: readonly MarkerDraftState[]` (ordered from most specific: `'marker'`, `'category'`, `'group'`).
    - In `map-marker-presentation.ts` (`frontend/libs/maps/feature-editor/src/lib/map-marker-presentation.ts`), compute `draftStates` for each marker by collecting all active draft levels in order (`marker.status === 'draft'`, `category.status === 'draft'`, `group.status === 'draft'`), omitting `draftStates` when all three are `'published'`.
    - In `map-markers.ts`:
        - Include the primary (most specific) draft level (`draftStates?.[0]`) in the icon `signature` and `#maskedIcon` cache key so OpenLayers icon cache invalidates immediately when the badge color changes.
        - When `draftStates?.[0]` is defined, draw a circular badge dot in the top-right corner of the marker canvas icon (white outer stroke ring for contrast against dark/light map tiles, filled with `#F59E0B` for `'marker'`, `#0EA5E9` for `'category'`, or `#8B5CF6` for `'group'`).
        - Update `#showTooltip` to render a two-line tooltip when `draftStates.length > 0`: line 1 is the marker name, line 2 is a muted caption listing every active draft level (`Draft: marker`, `Draft: marker, category`, `Draft: marker, category, group`, etc.).
- **Rationale**: Keeps `libs/maps/ui` transport-independent (no dependency on API DTOs) while leveraging the existing offscreen canvas icon cache for zero DOM-overlay overhead per marker and surfacing full draft context on hover.

### 4. Admin UI Controls and Status Indicators

- **Decision**:
    - **Switch Component**: Generate `@spartan-ng/helm/switch` into `frontend/libs/shared/ui-helm/switch` (`hlm-switch`) and use it across `MapEditorDialog`, `MapSettings`, `CategoryEditorDialog`, and `MarkerEditor`.
    - **`MapSettings` Form Refactor**: Replace the two separate inline `hlmInputGroupAddon` save icon buttons in `frontend/libs/maps/feature-editor/src/lib/map-settings/map-settings.html` with standard inputs for `Map name` and `Map slug`, a `Published` `hlm-switch`, and one form-level `Save changes` submit button that sends a single `PATCH` containing only changed fields (`name`, `slug`, `status`).
    - **Map Editor Header Action**: Replace the `Back to maps` link in `uiPageHeaderPortal` (`feature-editor.html`) with the publish button:
        - Shows `Publish` (enabled) when `map.status === 'draft'`.
        - Shows `Publish changes` when `map.status === 'published'`, enabled iff `hasDraftContent()` is true (at least one group, category, or marker on the map has `status === 'draft'`), and disabled when everything is already `published` or while queries are loading/errored or mutation is pending.
        - Uses a simple optimistic cache update in `MapsQueryOptions.publish`:
            - `onMutate`: cancels active map/groups/markers queries for that map, saves a snapshot (`{ previousMap, previousList, previousGroups, previousMarkers }`), and immediately sets `status: 'published'` in cache via `setQueryData` on the map (`detail` + `list`), all `CategoryGroup` and nested `Category` items (`getGroupsQueryKey`), and all `Marker` items (`getMarkersQueryKey`), without network refetches.
            - `onError`: restores the saved snapshot back into `setQueryData` for map, groups, and markers.
            - `onSuccess`: writes the authoritative `MapDto` returned by the server via `#acceptMap`.
    - **Badges in Lists, Trees, Selects, and Cards**:
        - _Left panel (`MapCategories`, `MapMarkers`) & Selects (`CategoryEditorDialog`, `MarkerEditor`)_: Render a Spartan `<span hlmBadge variant="secondary">Draft</span>` only when in draft (`status === 'draft'`, or for marker rows when marker/category/group is draft); render nothing for `published` to avoid visual clutter in dense lists.
        - _Game Maps Tab (`maps/feature-list`) & Game Cards/Editor (`games/feature-list`, `games/feature-editor`)_: Render explicit status badges using Spartan `hlmBadge` variants (`variant="default"` for `Published`, `variant="secondary"` for `Draft`) without custom color classes.
        - _Game Catalog Cards (`games/feature-list/game-card`)_: On map preview thumbnails where `map.status === 'draft'`, render a small top-right pill with `matVisibilityOffRound` and `hlmTooltip="Draft"`.

## Risks / Trade-offs

- **[Risk] Failed bulk publish after optimistic UI transition** → **Mitigation**: Keep a lightweight snapshot of the previous map, groups, and markers query data in `onMutate` and restore it in `onError` via `setQueryData`, avoiding any full-list refetch/invalidation while guaranteeing clean rollback on error.
- **[Risk] Preexisting database rows failing non-null `Status` constraint during migration** → **Mitigation**: Configure the EF Core migration with `defaultValue: "Published"` so existing maps, groups, categories, and markers are seamlessly backfilled as `Published` while new entities created through domain constructors/endpoints default to `Draft` unless specified.

## Migration Plan

1. Add `Status` property to `Map`, `CategoryGroup`, `Category`, and `Marker` domain classes and EF Core configurations (`HasConversion<string>()`, `HasMaxLength(32)`, required).
2. Generate an EF Core migration in `src/GameAtlas.Infrastructure/Persistence/Migrations` that adds non-null `TEXT` `Status` columns with default `"Published"` to `Maps`, `CategoryGroups`, `Categories`, and `Markers`.
3. Apply the migration locally and regenerate frontend OpenAPI clients via `pnpm run generate-api`.
