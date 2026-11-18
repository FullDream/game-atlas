# Design

## Context

The backend already exposes `GET /public/games/{gameSlug}/maps/{mapSlug}` returning complete public map data (`PublicMapDetailsDto`), but the frontend OpenAPI generator (`orval.config.ts`) was filtered to exclude `Public Maps`. The public web app (`apps/web`) has a reserved route at `games/:slug/maps/:mapSlug` with an empty children array. The shared map engine in `libs/maps/ui` currently couples marker presentation with admin draft lifecycle properties (`draftStates`, `pending`, `failed`, `DRAFT_BADGE_COLORS`).

## Goals / Non-Goals

**Goals:**

- Provide a responsive public map page supporting both wide desktop displays and mobile touch devices.
- Decouple the reusable `libs/maps/ui` map engine from domain-specific lifecycle states into pure visual presentation primitives.
- Persist player marker completion and category visibility preferences offline in IndexedDB via `@tanstack/db`.
- Deliver rapid marker completion workflows: an anchored popup with a bottom toggle adjacent to the pin on desktop, and a non-modal peek bottom sheet on mobile.

**Non-Goals:**

- Server-side synchronization or user authentication for progress (saved strictly to local browser storage).
- Marker or category authoring/editing in the public web app (exclusive to the admin app).
- Offline tile caching via Service Workers (standard browser HTTP caching is used).

## Decisions

### 1. Refactor `MapMarker` to Generic Visual Primitives

**Decision:** Replace application-specific properties (`draftStates`, `pending`, `failed`) in `libs/maps/ui/src/lib/map-viewport-types.ts` with generic presentation primitives:

- `opacity?: number` (e.g. 1.0 default, 0.35 for completed, 0.5 for pending)
- `badges?: readonly MarkerBadge[]` (generic badges with `color`, optional `text` or `icon`)
- `selected?: boolean`

**Rationale:** Keeps the OpenLayers viewer library pure and reusable across both admin and web contexts. Domain-specific mapping logic moves to adapters:

- Admin uses `map-marker-presentation.ts` to map drafts to badge indicators and pending mutations to 0.5 opacity.
- Web uses `public-marker-presentation.ts` to map completed state to 0.35 opacity.

_Alternatives considered:_

- Adding `completed?: boolean` directly to `MapMarker`: rejected because it pollutes the shared UI library with client-specific tracking concepts.

### 2. Local-First Persistence with TanStack DB (`@tanstack/db` + IndexedDB)

**Decision:** Use `@tanstack/db` with `createIndexedDB` and `createCollection` with `indexedDBCollectionOptions`.
Two collections are defined:

1. `completedMarkers`:
   ```ts
   interface CompletedMarker {
     id: string; // markerId
     categoryId: string; // categoryId
     mapId: string; // mapId
     gameId: string; // gameId
     completedAt: string; // ISO date
   }
   ```
2. `mapPreferences`:
   ```ts
   interface MapVisibilityPreference {
     id: string; // categoryId or groupId
     mapId: string; // mapId
     type: "group" | "category";
     hidden: boolean;
   }
   interface MapDisplaySettings {
     mapId: string;
     hideCompleted: boolean;
   }
   ```

**Rationale:** Including `categoryId` directly in `CompletedMarker` enables instant category progress counts (`completed / total`) without requiring expensive joins across the marker dataset. `@tanstack/db` preloads data into memory for synchronous signal reads and handles asynchronous write-behind to IndexedDB.

_Alternatives considered:_

- Direct `localStorage`: rejected due to 5MB storage limits, lack of indexing by mapId, and blocking synchronous serialization.
- Plain Dexie without TanStack DB: rejected because `@tanstack/db` offers standardized reactive live queries and cross-tab synchronization.

### 3. Desktop Hybrid Marker Inspection (Popup + Right Drawer)

**Decision:** When a marker is clicked on desktop, an OpenLayers anchored `Overlay` popup appears directly over the marker:

- Header: Category icon, marker name, close button, and an "Expand to side panel" (`⤢`) button.
- Body: Brief description and media thumbnail preview.
- Footer: Prominent completion checkbox directly at the bottom edge adjacent to the pin so cursor travel is minimal.
- Expanding (`⤢`) opens a full-height right-side drawer for deep inspection (scrollable full text, lightbox galleries, metadata).

**Rationale:** Gamers exploring interactive maps want fast, low-friction checklist completion. Anchoring the checkbox right next to the pin minimizes cursor movement. For complex guides, the expandable drawer prevents viewport clutter.

_Alternatives considered:_

- Right drawer only: rejected because moving the mouse to the right screen edge for every marker is tiring.
- Anchored popup only: rejected because large multi-image guides overflow the map viewport.

### 4. Mobile Non-Modal Interactive Bottom Sheet

**Decision:** On mobile, selecting a marker displays a bottom sheet in a non-modal peek state (~120px):

- Top half of the viewport has `pointer-events: none` on the overlay, allowing gestures directly into OpenLayers (pan, zoom, select other markers).
- Header contains the drag handle, marker name, and a completion switch positioned on the right for one-thumb reach.
- Swiping up expands to full height (~85vh) displaying description and media. Swiping down from peek dismisses the sheet.

**Rationale:** Modal sheets in standard UI libraries block map interactions, forcing users to open and close sheets repeatedly. A non-modal peek sheet lets users navigate the world while keeping marker context visible.

### 5. Floating Collapsible Left Panel on Desktop

**Decision:** The category filter panel on desktop is rendered as a floating card over the map:

- Sticky header contains Game Icon, Game Title, Map Title, and a collapse toggle (`<`).
- Middle scrollable section contains searchable category groups and categories with counts.
- Sticky footer contains bulk visibility buttons ("Show all" / "Hide all") and the "Hide completed" switch.
- Collapsing the panel slides it off-screen to the left, leaving a floating `[ ☰ ]` button and freeing 100% of the screen for map immersion. On mobile, this same content is housed in a dedicated bottom drawer opened via a floating filters button.

## Risks / Trade-offs

- **[Risk] High marker count impact on reactivity** → **Mitigation:** In-memory `Set<string>` of completed marker IDs keyed by `mapId` updated reactively from `@tanstack/db`, enabling $O(1)$ visibility and dimming checks during marker rendering.
- **[Risk] OpenLayers canvas events blocked by floating UI** → **Mitigation:** Strict isolation with Tailwind `pointer-events-none` on container layers and `pointer-events-auto` on interactive floating cards.
- **[Risk] Multiple open tabs modifying completion state** → **Mitigation:** `@tanstack/db` broadcast sync automatically updates collections across open tabs.

## Viewer review refinements

The public map route suppresses the global application header to give the map the full dynamic viewport height. The filter identity area provides home/game navigation and the shell-controlled theme selector. The filter action is projected into the shared viewport toolbar before style selection. Public style previews resolve a concrete minimum-zoom tile URL rather than loading the tile template as an image. Initial style resolution prefers the requested selection, then the default ready style, then the first ready style.

Desktop marker inspection uses Spartan Popover anchored to the selected marker pixel, refreshed after map rendering. It contains the full plain-text description and a manual Spartan Carousel in a height-bounded scrollable body, retaining the completion action and expanded side panel. Mobile category filters use the installed Spartan Drawer with its native dismissal and focus behavior. The nonmodal mobile marker peek sheet remains separate.

## Mobile inspection and system UI review

All public-map interface copy is English. Category navigation uses installed Helm Accordion, Input Group, Breadcrumb, Switch, Empty and shared Button controls. Desktop-only collapse is omitted from mobile category filters.

Mobile marker inspection uses the owned Helm Drawer without a backdrop or outside-pointer dismissal. Its complete media carousel, description, coordinates and completion controls share the desktop inspection content. The body scrolls independently from the fixed header and completion footer. An opt-in Drawer snapPoints API provides initial 0.34 and full 1.0 dynamic viewport heights. Dragging the resize handle up expands; dragging down from full height returns to the initial size; dragging down again dismisses. Clicking or keyboard-activating the handle toggles sizes. Existing drawers without snapPoints retain native Spartan drag dismissal. ResizeObserver emits the measured height; the viewer minimally pans the selected marker above this region, with unchanged zoom. Switching markers updates the open Drawer without resetting its height. This supersedes the previous 120px peek sheet and body-hidden-until-expanded behavior.

The nonmodal marker inspector directly projects owned Helm Drawer content with nonModal enabled instead of opening a modal CDK dialog portal. This preserves map accessibility and keyboard interaction as well as pointer access; modal category filters continue to use Spartan Portal. Snap-point dismissal emits dismissed in nonmodal mode.

## Persistent layout header

The global web layout header remains visible on the public map route, superseding the earlier header-suppression decision. The map fills the remaining dynamic viewport height below the 4rem header without adding page scrolling. Route-specific header visibility detection is removed.

## Map panel breadcrumbs

The category panel omits its duplicate theme switcher; theme selection remains in the persistent layout header. Spartan breadcrumbs below the identity header show a home icon linking to the catalog, the game name linking to its details page, and the current map name marked as the current page. Theme inputs and router-outlet theme coupling are removed. This supersedes the earlier panel theme-selection decision.
