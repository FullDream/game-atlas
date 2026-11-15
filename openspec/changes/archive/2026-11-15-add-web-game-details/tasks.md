# Tasks

## 1. Public map summary counts

- [x] 1.1 Extend PublicGameMapDto and GetPublicGameHandler with visible per-map marker/category aggregates and consistent game totals; verify zero maps, zero counts and preserved summary ordering through the public endpoint.
- [x] 1.2 Extend PublicReadTests and PublicReadContractTests for additive summary fields, mixed visibility at every ancestor, empty maps and matching totals; run focused backend tests and `dotnet build GameAtlas.slnx`.
- [x] 1.3 Update public-read contracts in docs/api-conventions.md and assembly guidance where needed; verify documentation matches the actual response and run the documentation checker.

## 2. Public Games transport and query options

- [x] 2.1 Include Games and Public Games in Orval generation, regenerate via the existing games-data-access:generate-api Nx target against the running backend, and export generated public contracts; verify both game services exist and no unrelated service is generated.
- [x] 2.2 Add separate PublicGamesQueryOptions and normalized public keys, cancellable list/detail reads and newest matching list placeholders with explicitly partial observer data; verify compilation and manually confirm slug isolation and a detail request despite cached card data.
- [x] 2.3 Set the web QueryClient default staleTime to Infinity without public factory overrides; verify explicit refetch still runs, hydrated complete data is reused and admin options retain their current freshness.
- [x] 2.4 Update Games data-access README and docs/frontend/data-access.md for public transport, placeholder ownership and web freshness; format and lint the affected project and run the documentation checker.

## 3. Shared declarative page progress

- [x] 3.1 Add shell-scoped loading aggregation, a lifecycle-owned declarative input directive and a shared Spartan header progress component combining routing and declared activity; verify overlapping sources, cleanup, cancelled navigation, accessible labeling and stable header dimensions manually.
- [x] 3.2 Connect existing PageHeaderPortal loading declarations to the shared mechanism, replace admin Layout progress markup and remove the redundant NavigationLoading wrapper; verify existing admin list/editor loading and header actions still work.
- [x] 3.3 Mount the shared progress host/context in the web shell and document declaration usage in shared UI README and owning UI guidance; verify a routed child can declare loading without a header portal, then format and lint affected projects and run the documentation checker.

- [x] 3.4 Fix the existing color-picker private-member and statement-spacing lint errors as explicitly approved during implementation; verify full shared-ui lint and consuming application production builds.

## 4. Public catalog and game detail page

- [x] 4.1 Migrate web catalog and card types to public query options and mapsCount, preserving URL search/sort/pagination behavior and declaring isFetching for progress; verify published-only requests and cached catalog navigation manually.
- [x] 4.2 Scaffold the web Games detail feature using repository Nx guidance, lazy-load it at /games/:slug, remove the placeholder and derive the title from loaded game data; verify direct SSR URLs and client slug changes compile and resolve correctly.
- [x] 4.3 Compose responsive breadcrumbs, hero, game counts and map cards with both marker/category counts using available Spartan/shared components; extract actual duplicated presentation/utilities into permitted shared domain boundaries and generate missing Spartan primitives only if needed; verify narrow/wide layouts, both themes and image fallbacks against the supplied reference.
- [x] 4.4 Implement truthful partial/cold skeletons, empty maps, authoritative null fields, unavailable-game errors and retry, and bind detail request activity declaratively; verify cached-card transitions, no-cache load, existing detail reuse, slug changes, errors and refreshes manually.
- [x] 4.5 Update feature README and owning frontend architecture/UI documentation for the implemented feature and states; format, lint the affected feature projects and run the documentation checker.

## 5. Integration verification

- [x] 5.1 Run affected Nx lint targets and production builds for web and admin; verify generated-client consumers and shared progress compile together, preserving existing frontend tests without adding or running unit/E2E suites.
- [x] 5.2 Verify direct SSR game details and catalog hydrate without duplicate requests, concurrent SSR caches remain isolated, and moving between catalog/detail/admin pages leaves no stale progress contribution; record runtime observations and any limitations.
- [x] 5.3 Validate the complete change with OpenSpec, run the documentation checker and git diff whitespace checks, and review the incremental diff to confirm existing user changes remain intact.

## Verification evidence

- Backend: `dotnet build GameAtlas.slnx --no-restore -v quiet` succeeded; focused PublicRead integration tests passed 13/13, including mixed visibility, per-map counts, empty maps and OpenAPI fields. Existing unrelated compiler warnings remain.
- Frontend: affected Nx lint passed for games-data-access, games-feature-catalog, games-feature-detail, shared-ui, shared-util, admin and web. Production builds passed for both applications. Existing frontend unit/E2E suites were neither changed nor run.
- Manual Edge browser review used real public SSR data and a temporary in-memory API fixture, without modifying persisted game content. At 1440px and 375px the detail composition rendered correctly without horizontal overflow; dark SSR used the saved theme cookie.
- Cached catalog navigation immediately showed the game name/images/counts, two missing-map skeletons and active header progress; one detail request completed the page. Revisit reused complete cached detail. Direct successful SSR detail hydration made no duplicate client API request.
- Empty maps, unavailable game, failed placeholder replacement and explicit Retry recovery were exercised. Leaving an in-flight detail removed progress without changing header height or leaking state into the catalog. Admin list/media loading and editor Save header actions remained functional.
- Parallel SSR catalog requests for different search strings transferred only their own query data. Multi-source registration cleanup and navigation completion/cancellation semantics were additionally reviewed in code; no frontend unit/E2E coverage is claimed.
- The explicitly approved color-picker cleanup allowed full shared-ui lint to pass. Documentation checking, strict OpenSpec validation and whitespace checking passed.
