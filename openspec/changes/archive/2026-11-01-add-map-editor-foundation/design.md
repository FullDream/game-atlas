# Design

## Context

See proposal.md for motivation and scope. Maps data-access already generates style add/update/delete endpoints, exposes detail/add options and owns map caches. Update/delete style options are missing. Admin currently disables Open editor and nests the map list in the game settings editor. Backend metadata already describes centered pixel geometry and immutable XYZ tiles. Admin's development proxy currently forwards API and media paths, but not /map-tiles.

## Goals / Non-Goals

**Goals:** isolate the engine behind a small shared Angular API, preserve existing domain/platform boundaries, and reuse the current REST, TanStack and shared media-picker integration.

**Non-Goals:** a generic multi-engine framework, a new application state store, backend changes, or a public viewer page. Feature scope exclusions are listed in proposal.md.

## Decisions

### Shared viewport and admin orchestration

Create non-buildable libs/maps/ui (type:ui, scope:maps, platform:shared) and libs/maps/feature-editor (type:feature, scope:maps, platform:admin) using Nx generators. UI depends on shared UI/util only, never maps data-access. Export a small typed presentation contract for geometry, styles and selected style ID, using signal inputs/models and outputs. Feature adapts API models structurally; do not duplicate backend DTOs or expose OpenLayers instances.

Alternative: put the viewport in the admin feature. Rejected because web cannot depend on admin libraries. A shared API-aware feature would also violate UI/data-access directions and couple the viewport to transport.

### Direct OpenLayers integration

Use ol directly, with browser-only lazy initialization in Angular render hooks. An imperative integration effect updates engine state; component destruction disposes the map, observers and listeners. ResizeObserver updates map size. Use a custom pixel Projection, TileGrid from returned extent/origin/tileSize/resolutions, and XYZ with wrapX:false and the returned tile template. Keep coordinate precision; never apply geographic conversion. Fit the initial extent, allow smooth viewport resolutions within configured bounds, and fit again only on explicit fit or a new map identity. Small-map minZoom=maxZoom remains valid.

Changing styles replaces the source/background without recreating View. The parent owns selected style identity; viewer and panel share it. Selection initially follows defaultStyleId, retains any surviving displayable selection after mutations, and falls back to the current default when needed.

Alternative ngx-openlayers has an Angular 22 compatibility entry, but adds a second wrapper API for a small imperative surface. Direct ol keeps custom tile-grid and lifecycle behavior explicit without a version-coupled Angular dependency.

### Dedicated editor route and layout

Compose /games/:gameId/maps/:mapId/edit at the admin application level before the generic game editor route. Keep it outside game settings tabs while retaining the persistent shell and Games navigation. This avoids nested game-editor forms/header actions and lets the map use the available content height. Provide Maps services/query options at the route; obtain game breadcrumb labels through application composition without cross-domain feature imports. Enable Open editor links in the existing map list.

Use the supplied screenshot for proportions: independently scrollable left style panel and flexible central viewport. On narrow screens expose styles through a labelled collapsible/sheet panel. Do not reproduce placeholder account, publication actions or category/marker content from the sketch.

### Spartan controls and style dialogs

Reuse installed Helm components, shared ui-button, MediaThumbnail, ConfirmationDialog, MediaPickerDialog, semantic theme tokens and Material Symbols Rounded. Inspect Spartan catalog and generate missing entrypoints only if needed. Use Signal Forms for name/backgroundColor and existing media-picker port for source selection; picker upload, if used, remains library-owned. Validate supported raster type and exact dimensions when metadata is available; server errors remain authoritative. Default the new background to #FFFFFF as an editable presentation default.

Add and edit use dialogs, with source selection only for creation. Editing covers name and background color; getDirtyValues supplies changed fields and avoids empty patches. Confirmation explains that removing a style preserves library assets and map content. Pending submissions retain layout and prevent duplicates. Search/filter appear disabled with accessible explanations. Retry processing, minimap and scale are deferred.

### Query and mutation reconciliation

Extend MapsQueryOptions with updateStyle/deleteStyle. Use existing generated transport and toPromise, preserving HTTP errors. Make metadata updates optimistic when a baseline exists, with cancellation, rollback and authoritative style replacement. Serialize related per-map writes with TanStack mutation scopes; do not create a custom queue.

Creation returns a complete map, so write detail/list snapshots directly. Metadata PATCH returns one style: replace it in known map caches without reordering. DELETE returns no map: after success remove the style from known detail/list caches and derive the earliest remaining ready default from existing server-ordered styles. Narrowly invalidate only when a complete authoritative baseline is unavailable or concurrent results cannot be reconciled; do not refetch just to discover processing completion. Preserve unrelated successful mutations and list metadata not changed by the response.

Configure the editor's detail query to disable polling, focus and reconnect refreshes. New route entry or explicit retry may load current metadata. Processing status stays a snapshot; omit percentages and bars. Apply the same snapshot semantics to list updates.

### Tile delivery and rendering environment

Proxy /map-tiles/** unchanged to the backend for admin development. Keep returned canonical tile paths intact; document production reverse-proxy hosting separately from /api rewriting. Public deployment remains future work. Ensure the shared viewport's browser package import and initialization do not execute during web prerendering.

## Risks / Trade-offs

- Snapshot statuses can remain processing after generation completes -> state that statuses are last loaded; do not imply live progress.
- Missing tile hosting configuration can produce an empty viewport -> add the development proxy, document production routing and verify a real tile request manually.
- Engine initialization before a visible layout can produce incorrect dimensions -> initialize in browser render hooks, observe size changes and dispose on teardown.
- Partial mutation responses cannot establish unseen concurrent server changes -> update known fields only and narrowly invalidate ambiguous caches.
- No public consumer is added yet -> keep shared dependencies platform-neutral and build web to catch SSR/import regressions; this does not establish public product behavior.

## Migration Plan

No database or API migration is required. Add libraries/dependency, integrate the admin route and style options, then document tile hosting. Rollback removes the new route/UI and dependency while preserving existing maps and styles. Verify changed Markdown, affected lint and admin/web production builds; manually inspect the specified scenarios without adding or running frontend unit/E2E suites.
