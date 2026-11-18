# Tasks

## 1. API Generation & Public Data Access

- [x] 1.1 Add `'Public Maps'` tag to `libs/maps/data-access/orval.config.ts`, run Orval generation, and verify generated public maps service and models are created.
- [x] 1.2 Implement `PublicMapsQueryOptions` in `libs/maps/data-access` for `GET /public/games/{gameSlug}/maps/{mapSlug}` and verify type-check passes.

## 2. Core Map Engine Decoupling (`libs/maps/ui`)

- [x] 2.1 Update `MapMarker` in `libs/maps/ui/src/lib/map-viewport-types.ts` to support optional `opacity?: number` and generic `badges?: readonly MarkerBadge[]`.
- [x] 2.2 Update `MapMarkers` vector styling in `libs/maps/ui/src/lib/map-markers.ts` to apply custom `opacity` to icon and fallback pin styles.
- [x] 2.3 Update admin adapter `map-marker-presentation.ts` in `libs/maps/feature-editor` to adapt admin draft states to generic badges and verify admin editor builds without errors.

## 3. Client Storage with Dexie

- [x] 3.1 Add `dexie` to `package.json`, install dependencies, and verify clean installation.
- [x] 3.2 Define tables for completed markers and map preferences backed by IndexedDB using Dexie.
- [x] 3.3 Create reactive Angular signal wrappers for querying completed markers, calculating category progress, and persisting hidden category/group states.

## 4. Public Map Feature Scaffolding & Routing

- [x] 4.1 Scaffold `FeaturePublicMap` component (in `libs/maps/feature-public-map` or `apps/web`) integrating `MapViewport`.
- [x] 4.2 Wire route `games/:slug/maps/:mapSlug` in `apps/web/src/app/app.routes.ts` and verify navigation loads published map data.

## 5. Category & Group Filter Panel

- [x] 5.1 Build the floating desktop filter card with Game icon, Game name, Map title, search input, and collapse button `<`.
- [x] 5.2 Implement accordion tree of category groups and categories with counts and individual/group visibility toggles.
- [x] 5.3 Implement sticky footer with "Show all", "Hide all", and "Hide completed" switch bound to Dexie persistence.
- [x] 5.4 Implement mobile responsive mode opening the filter panel as a bottom drawer (`hlm-drawer`).

## 6. Desktop Marker Inspection (Hybrid Popup & Side Drawer)

- [x] 6.1 Create `public-marker-presentation.ts` to map public markers to `MapMarker` with dimmed `opacity: 0.35` for completed items.
- [x] 6.2 Build OpenLayers anchored popup component displaying marker header, short preview, expand action (`⤢`), and quick-completion checkbox at the bottom edge.
- [x] 6.3 Build right-side inspection drawer for expanded mode showing full description and media gallery.

## 7. Mobile Non-Modal Interactive Bottom Sheet

- [x] 7.1 Build non-modal peek bottom sheet (~120px) allowing map interaction (pan, zoom, tap) while open.
- [x] 7.2 Implement peek header with drag handle, marker name, category badge, and prominent right-aligned completion switch.
- [x] 7.3 Implement touch gestures for expanding to ~85vh full screen and swiping down to close.

## 8. Integration & Verification

- [x] 8.1 Run `nx build web` and `nx lint web` to verify clean compilation without lint errors.
- [x] 8.2 Verify offline-first persistence across page reloads in IndexedDB for both completed markers and category visibility.

## 9. Viewer review refinements

- [x] 9.1 Hide the global header on the map route and place theme selection and home/game navigation below the filter panel identity.
- [x] 9.2 Project the filter toggle into the viewport toolbar immediately before the style selector.
- [x] 9.3 Resolve a real tile URL for public style thumbnails and select the default ready style on initial load with a first-ready fallback.
- [x] 9.4 Compose desktop marker inspection with Spartan Popover, bounded scrollable full description, and a manual media carousel.
- [x] 9.5 Replace the custom mobile filter overlay with Spartan Drawer.
- [x] 9.6 Verify the refinements with formatting, affected lint, and the production web build.

## 10. System UI and mobile inspection refinements

- [x] 10.1 Replace public-map custom buttons, search input, accordion, breadcrumbs, and resource feedback with installed shared UI and Helm primitives; translate interface copy to English.
- [x] 10.2 Share complete marker media, description, coordinates, and completion controls between desktop inspection and mobile Drawer.
- [x] 10.3 Add opt-in snap points to the owned Helm Drawer, initially one-third height with a scrollable body, expand upward to full height, collapse downward before dismissal, and provide a keyboard resize alternative.
- [x] 10.4 Keep the mobile marker Drawer open during map interaction and marker switching, and reveal selected markers above its measured height without changing zoom.
- [x] 10.5 Remove the redundant mobile category-panel collapse arrow while preserving desktop collapse and native mobile Drawer dismissal.
- [x] 10.6 Run formatting, affected lint, consuming application production builds, and documentation checks.

## 11. Persistent layout header

- [x] 11.1 Restore the global header on the map route and size the map to the remaining viewport height.
- [x] 11.2 Verify formatting, affected lint, the production web build and documentation.

## 12. Panel breadcrumb refinement

- [x] 12.1 Remove duplicate panel theme selection and its unused shell bindings; display home icon, linked game and current map in Spartan breadcrumbs.
- [x] 12.2 Verify formatting, affected lint, production web build and documentation.
