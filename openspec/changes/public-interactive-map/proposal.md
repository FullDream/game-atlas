# Proposal

## Why

Visitors currently can browse published games and view map summary cards on the web application, but navigating to `/games/:slug/maps/:mapSlug` yields an empty placeholder because the public interactive map page has not yet been implemented. Players need an interactive, full-screen map experience with fast client-side progress tracking (marking markers as found/done) and flexible category visibility filtering, persisted locally in their browser across reloads without requiring server authentication.

## What Changes

- Add Orval OpenAPI code generation for the backend `Public Maps` tag, generating `PublicMapsService` for `GET /public/games/{gameSlug}/maps/{mapSlug}`.
- Create `PublicMapsQueryOptions` using `@tanstack/angular-query-experimental` to query published map geometry, styles, category groups, and markers.
- Refactor the core map engine marker model (`libs/maps/ui`) to use generic visual primitives (`opacity`, `badges`) instead of admin-specific lifecycle states (`draftStates`, `pending`, `failed`), decoupling the viewer from admin-specific concepts.
- Integrate `@tanstack/db` with IndexedDB collections for client-side persistence:
  - `completedMarkers`: stores completed marker IDs with `categoryId`, `mapId`, `gameId`, and timestamp for instant category/map progress aggregation.
  - `mapPreferences`: stores hidden group/category IDs and map display settings (e.g. "hide completed markers" toggle).
- Implement the public interactive map page (`libs/maps/feature-public-map` or `apps/web` route component) at `/games/:slug/maps/:mapSlug`:
  - **Desktop layout**: 100% viewport map canvas with a collapsible floating left card for game/map identity, search, category hierarchy, and sticky footer with visibility toggles and bulk actions.
  - **Desktop marker inspection (Hybrid)**: compact OpenLayers anchored popup above the marker pin with a quick-completion checkbox at the bottom (adjacent to the pin) and an expand button (`⤢`) to open the full right details sidebar.
  - **Mobile layout**: responsive full-screen map with minimal top header; the filter panel transitions into a bottom drawer, and marker inspection uses a non-modal peek bottom sheet (~120px) with an instant thumb toggle in the header, expandable to full screen via swipe up.
  - **Completed marker rendering**: completed markers render dimmed (`opacity: 0.35`) or are completely hidden when the "Hide completed" toggle is enabled.

## Capabilities

### New Capabilities
- `web-map`: Public interactive map viewing, category/group visibility filtering, and offline-first marker completion tracking persisted via `@tanstack/db` and IndexedDB.

### Modified Capabilities
- `map-viewer`: Generalize marker presentation to support transport-independent visual primitives (`opacity`, `badges`) decoupling presentation styling from administration draft states while preserving draft indicators in admin.

## Impact

- **Frontend libraries**:
  - `libs/maps/ui`: update `MapMarker` and `MapMarkers` to support `opacity` and generic `badges`.
  - `libs/maps/feature-editor`: update `map-marker-presentation.ts` to map draft states and pending mutations to the generic `badges` and `opacity`.
  - `libs/maps/data-access`: update `orval.config.ts` to include `Public Maps` tag, re-run code generation, and add `PublicMapsQueryOptions`.
  - New public map feature library / component under `libs/maps/feature-public-map` (or `apps/web`).
- **Dependencies**:
  - Add `@tanstack/db` to `package.json` for reactive IndexedDB persistence.
- **Routing**:
  - Wire route `games/:slug/maps/:mapSlug` in `apps/web/src/app/app.routes.ts`.
