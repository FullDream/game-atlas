# Design

## Context

See proposal.md for motivation. The web catalog currently consumes management `Game` models and `GamesQueryOptions`, including a local 30-second stale time. The web QueryClient also defaults to 30 seconds and already supports per-request SSR state transfer. The game route is an application-local placeholder.

`PublicGameCardDto` has images and counts but no description or map summaries. `PublicGameMapDto` lacks counts. `GetPublicGameHandler` already filters published maps and computes game-level visible category/marker counts. Admin renders Spartan Progress directly in Layout, combining `NavigationLoading` with `PageHeader.loading`; features supply the latter through `uiPageHeaderPortal`.

Existing web-games/web-shell specs explicitly describe management catalog reads and placeholder routing. Their modified requirements deliberately replace those behaviors. Management detail initialization from full management lists remains unchanged.

## Goals / Non-Goals

**Goals:** Keep public transport shared across platforms, partial loading truthful, SSR isolation intact and progress declarations scoped to their owners.

**Non-Goals:** No database migration, public Maps client generation, map viewer, global HTTP loading interceptor, new cache service or change to admin freshness. Map previews, names and arrow actions navigate to a reserved componentless `/games/:slug/maps/:mapSlug` route. This intentionally leaves the content outlet empty until the map viewer is implemented, while preserving the destination URL instead of redirecting home.

## Decisions

### Assemble public counts per map in bounded queries

Extend `PublicGameMapDto` with integer markersCount/categoriesCount. Refactor `GetPublicGameHandler` to aggregate published categories under published groups by map and published markers under those categories by map. Use server-side grouping or a fixed number of batched projections, never one query per map. Map absent aggregates to zero; derive game totals from the same per-map results so visibility rules agree. Preserve map ordering and default-ready-preview selection. No persistence change is needed. Counting in the browser would require downloading unnecessary map content and risks inconsistent visibility.

### Separate public query options, retain one shared transport library

Include exact tags `Games` and `Public Games` in Orval filters, regenerate Angular HttpClient transport through the existing uncached Nx target and export the public models. Add handwritten `PublicGamesQueryOptions` with `getList` and `getDetail`, normalized public-list keys and slug detail keys rooted separately from management keys. Reuse common sort/parameter utilities only where contracts agree. Use the existing `toPromise` cancellation bridge. Set Infinity only as the web QueryClient default; public factories omit local stale time. Keep explicit refetch/invalidation usable and leave admin factories alone.

### Model partial placeholders explicitly

Choose the newest successful cached public list containing the exact requested slug. Return a card-shaped placeholder without inventing description, maps, releaseDate or developer. The observer data type must admit partial detail fields without casting a fabricated object to a complete generated response; the query function still returns the complete generated public detail. Use `isPlaceholderData()` to distinguish partial data from authoritative null/empty values and `isLoading()` for a cold initial fetch. Do not use `initialData`: Infinity could then suppress the complete request. Never reuse the previous slug's placeholder or replace existing complete detail. Error rendering takes precedence over partial display.

### Keep page composition in a web feature

Create a web-scoped Games detail feature consistent with existing Nx boundaries, lazy-loaded from `/games/:slug`, and remove the placeholder. Use route input binding for slug. Preserve SSR PendingTasks/dehydration and derive a descriptive page title from resolved data. Compose a responsive hero and map grid from Spartan Card, Breadcrumb, Skeleton and Empty plus shared MediaThumbnail, Button and ErrorState. Use semantic theme tokens and readable overlays; support null images and optional metadata. Known mapsCount determines first-load map skeleton count; zero shows no invented cards. Use actual available assets, not reference artwork.

Extract repeated count formatting/presentation and image fallback structure only where catalog/detail duplication warrants it, into the narrowest permitted Games UI/util boundary. Do not reuse admin form-oriented components solely because they share a domain. Generate missing Spartan primitives using repository guidance if needed; the identified primitives already exist.

### Share progress with lifecycle-owned declarations

Add an application-shell-scoped loading context in shared UI that aggregates independently registered reactive boolean sources. A declarative loading directive registers its input and unregisters on destruction, without requiring a portal. Integrate `PageHeaderPortal` loading with the same registration mechanism, preserving its existing `[loading]` API, header actions and breadcrumbs. Provide the context at each shell ancestor of its outlet, never as process-global mutable state.

A shared header progress component combines the context and Router.currentNavigation(), renders the existing thin indeterminate Spartan bar and has an accessible loading label without a numeric percentage. Both headers use it; remove the redundant admin-only NavigationLoading wrapper. Catalog and detail declarations bind isFetching(), covering placeholders and refreshes. Keep progress absolutely positioned so header dimensions do not change. A global HTTP interceptor was rejected because features already express meaningful loading declaratively.

## Risks / Trade-offs

- Infinite freshness can retain old published metadata for the cache lifetime: retain explicit retry/refetch and invalidation; Infinity does not imply infinite gcTime or permanent storage.
- Placeholder queries report success before complete data exists: distinguish partial state explicitly rather than relying on isLoading alone.
- Aggregation may accidentally count hidden descendants or multiply marker joins: keep category and marker aggregations separate and verify mixed visibility and empty maps in backend tests.
- Shared progress can leak or clear another owner's activity: use independent registration identities, destroy cleanup and per-shell context.
- Existing working-tree changes overlap these files: preserve them and review only the implementation's incremental diff.

## Migration Plan

Ship additive backend fields first, regenerate committed Games clients, then migrate public catalog/detail consumers and both progress hosts. Normal builds remain offline with respect to OpenAPI. Rollback frontend consumers and progress extraction before removing backend fields; no data migration is required.

Verify backend public-read contracts and behavior with focused existing integration coverage and a solution build. Frontend test work remains deferred: use formatting, affected Nx lint and production builds for both apps, plus manual browser/SSR checks for cached transitions, direct URLs, errors, themes and loading cleanup. Update owning API/frontend guidance with implemented behavior.

## Additional approved cleanup

During implementation the user explicitly approved fixing the existing shared-ui color-picker lint failures so the full verification could pass. The cleanup replaces three private members with ECMAScript private syntax and fixes statement spacing/formatting; picker behavior remains unchanged.
