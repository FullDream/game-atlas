# Spec Delta

## MODIFIED Requirements

### Requirement: Transfer per-request TanStack Query state from SSR to browser

Each SSR request SHALL create an isolated `QueryClient` instance rather than sharing a process-global cache. SSR requests SHALL resolve `GAMES_BASE_URL` to the backend origin (`GAME_ATLAS_API_URL` or `GAMES_SERVER_URL`), while browser requests resolve `/api`, dehydrating and rehydrating `QueryClient` via `TransferState`.

#### Scenario: Hydrate initial catalog without duplicate API fetch

- **WHEN** SSR resolves the initial `GET /public/games` query against the backend origin and returns HTML
- **THEN** the hydrated client `QueryClient` reuses the transferred query state within `staleTime` without re-fetching `/api/public/games` on initial hydration
- **AND** concurrent SSR requests remain isolated in separate `QueryClient` instances

### Requirement: Display public header and reserve /games/:slug route

The `web` shell SHALL render a semantic header landmark with the GameAtlas brand link navigating to `/`, the compact `ThemeSwitcher` on the trailing edge, a skip-to-content link, and a `<router-outlet>` inside `<main>`. The router SHALL define `/` for the home catalog and a `/games/:slug` game detail route rendered with `RenderMode.Server` for published game reads.

#### Scenario: Navigate to placeholder game route

- **WHEN** a visitor navigates directly or via a game card to `/games/elden-ring`
- **THEN** the SSR server renders the published game detail page with its name, maps and a link back to `/`

## ADDED Requirements

### Requirement: Default web query freshness to infinity

All web queries SHALL default to infinite stale time. Successfully loaded queries SHALL not refetch merely due to elapsed time, remount or window focus. Explicit refetch and invalidation SHALL remain effective. This policy SHALL not change admin freshness or prevent a first complete detail request over a partial placeholder.

#### Scenario: Hydrate or revisit loaded content

- **WHEN** complete query data is hydrated or its page is revisited while it remains cached
- **THEN** freshness alone causes no duplicate request
- **AND** explicitly refetching still retrieves current data

#### Scenario: Render a detail URL directly

- **WHEN** SSR handles a published game detail URL
- **THEN** it resolves the complete public detail before serialization and transfers that result to the browser
- **AND** browser hydration reuses it without an immediate duplicate detail request
