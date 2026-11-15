# web-shell Specification

## Purpose

Provide an authentication-free, server-side rendered public `web` application shell using Hono, per-request data and cookie-based theme hydration, and shared GameAtlas navigation chrome.

## Requirements

### Requirement: Serve public web routes via Hono SSR

The `web` application SHALL run with `outputMode: "server"` and `RenderMode.Server` across public routes (`**`) through a Hono server entry backed by `AngularAppEngine`, `createRequestHandler`, and `@hono/node-server`. It SHALL serve static browser assets with immutable caching for hashed bundles and trust reverse-proxy headers behind Caddy.

#### Scenario: Render public home page on first request

- **WHEN** a browser or crawler requests `/` from the SSR server
- **THEN** the server responds with HTTP 200 and server-rendered HTML containing the page shell and catalog markup
- **AND** static browser assets are served from the built browser output without Express

#### Scenario: Operate independently without authentication

- **WHEN** an unauthenticated visitor opens any public route in `web`
- **THEN** the shell renders without login prompts, user accounts, or admin workspace dependencies

### Requirement: Transfer per-request TanStack Query state from SSR to browser

Each SSR request SHALL create an isolated `QueryClient` instance rather than sharing a process-global cache. SSR requests SHALL resolve `GAMES_BASE_URL` to the backend origin (`GAME_ATLAS_API_URL` or `GAMES_SERVER_URL`), while browser requests resolve `/api`, dehydrating and rehydrating `QueryClient` via `TransferState`.

#### Scenario: Hydrate initial catalog without duplicate API fetch

- **WHEN** SSR resolves the initial `GET /public/games` query against the backend origin and returns HTML
- **THEN** the hydrated client `QueryClient` reuses the transferred query state within `staleTime` without re-fetching `/api/public/games` on initial hydration
- **AND** concurrent SSR requests remain isolated in separate `QueryClient` instances

### Requirement: Apply cookie-persisted theme before HTML flush

The `web` shell SHALL place the compact `ThemeSwitcher` (`[compact]="true"`) in the header and persist `light`, `dark`, or `system` in the `game-atlas.theme` cookie (`Path=/; SameSite=Lax`). During SSR, `ThemeSettings` SHALL read that cookie from `REQUEST` and update `color-scheme` and `theme-color` meta tags before HTML serialization.

#### Scenario: Render saved dark preference on SSR

- **WHEN** a visitor with `Cookie: game-atlas.theme=dark` requests `/`
- **THEN** the server-rendered HTML `<head>` already contains `<meta name="color-scheme" content="dark">` and updated `theme-color` media attributes
- **AND** the header theme toggle renders in its dark state during SSR without hydration flicker

#### Scenario: Default to system theme without cookie

- **WHEN** a visitor requests `/` without a valid `game-atlas.theme` cookie
- **THEN** `ThemeSettings` defaults to `'system'` with `<meta name="color-scheme" content="light dark">` so `light-dark()` tokens follow the browser's color scheme on first paint

#### Scenario: Switch theme on the client

- **WHEN** a visitor activates the compact theme switcher in the header
- **THEN** `ThemeSettings` updates the `game-atlas.theme` cookie and updates the document's `color-scheme` and `theme-color` meta tags immediately

### Requirement: Display public header and reserve /games/:slug route

The `web` shell SHALL render a semantic header landmark with the GameAtlas brand link navigating to `/`, the compact `ThemeSwitcher` on the trailing edge, a skip-to-content link, and a `<router-outlet>` inside `<main>`. The router SHALL define `/` for the home catalog and a `/games/:slug` game detail route rendered with `RenderMode.Server` for published game reads.

#### Scenario: Navigate to placeholder game route

- **WHEN** a visitor navigates directly or via a game card to `/games/elden-ring`
- **THEN** the SSR server renders the published game detail page with its name, maps and a link back to `/`

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
