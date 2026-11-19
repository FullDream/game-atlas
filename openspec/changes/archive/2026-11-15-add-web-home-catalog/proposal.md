# Proposal: Public Web SSR Shell and Home Game Catalog

## Why

The `apps/web` application is currently a placeholder static-prerendered shell without routing, SSR execution, theme switching, or data access. Launching the public GameAtlas experience requires enabling request-time SSR (`RenderMode.Server`) across all public routes and building the public home page so visitors can discover, search, sort, and paginate games with immediate server-rendered HTML and zero flash of incorrect theme or missing data.

## What Changes

- Configure `apps/web` for full SSR (`outputMode: "server"`, `RenderMode.Server` on `**`) using a lightweight **Hono** (`hono` + `@hono/node-server`) server entry (`server.ts`) backed by `AngularAppEngine`, static browser asset delivery, and trusted proxy headers for deployment behind Caddy.
- Configure `apps/web` HTTP, TanStack Query, Spartan UI, and routing foundations:
    - Per-request isolated `QueryClient` with `TransferState` dehydration/hydration so SSR API reads (`GAME_ATLAS_API_URL` / `http://localhost:5041`) transfer cleanly to browser hydration (`/api` via dev `proxy.conf.cjs` or Caddy in production).
    - Cookie-backed `ThemeSettings` (`game-atlas.theme`) evaluated on the server via Angular `REQUEST` so `<meta name="color-scheme">`, `<meta name="theme-color">`, and header `ThemeSwitcher` (`[compact]="true"`) render accurately before HTML is flushed.
    - Public top header (logo + `GameAtlas` wordmark + compact `ThemeSwitcher`), `WebTitleStrategy`, and a minimal SSR placeholder route `/games/:slug` for card navigation.
- Generate the missing `@spartan-ng/helm/card` primitive in `libs/shared/ui-helm/card`.
- Add `libs/games/feature-catalog` (`type:feature, scope:games, platform:web`) consuming `GamesQueryOptions.getList`:
    - Hero banner with a static `.webp` key-art asset + blur placeholder, theme-adaptive gradient fade, headline/subtitle, two feature callouts (`Unlimited markers`, `Multiple maps`), and a floating pill search field (`HlmInputGroup`) with debounced URL-synced search.
    - Games catalog section with URL-synced (`linkedQueryParam`) search, all 6 API sort options (`HlmSelect`), responsive 1/2/3/4-column card grid (`HlmCard`, `MediaThumbnail`, styled fallback when `banner === null`, map/marker counts, chevron link to `/games/:slug`, no draft/published status badges), loading skeletons (`HlmSkeleton`), empty/error states (`HlmEmpty`, `ErrorState`), and `HlmNumberedPaginationQueryParams` with `[24, 48, 72, 96]` page-size presets.

### Out of Scope

- Public API publication filtering (`status=published` backend changes or client-side filtering — the home catalog displays all results returned by existing `GET /games`).
- Full `/games/:slug` game overview or public interactive map viewer implementation (covered only by a placeholder SSR route so card links do not 404).
- Multi-language selector / `Multilingual` hero feature, authentication/accounts, and admin-managed hero banners.
- Automated unit or E2E test suites unless explicitly requested.

## Capabilities

### New Capabilities

- `web-shell`: Public SSR application shell (`apps/web`) powered by Hono + `AngularAppEngine`, per-request SSR data/theme hydration via cookies and `TransferState`, top navigation bar with compact `ThemeSwitcher`, development API/media proxy, and `/games/:slug` placeholder route.
- `web-games`: Public home page game catalog (`libs/games/feature-catalog` and shared `@spartan-ng/helm/card`), including hero banner with static `.webp` + placeholder, debounced search, 6-option sorting, `[24, 48, 72, 96]` pagination, URL query-parameter sync, and public `GameCard` presentation without publication badges.

### Modified Capabilities

<!-- None: existing backend game-catalog and admin specs are unchanged. -->

## Impact

- **Dependencies**: Add `hono` and `@hono/node-server` in `frontend/package.json`.
- **Apps**: `frontend/apps/web/` (`project.json`, `server.ts`, `proxy.conf.cjs`, `index.html`, `app.ts`, `app.html`, `app.config.ts`, `app.config.server.ts`, `app.routes.ts`, `app.routes.server.ts`, `theme-settings.ts`, `web-title-strategy.ts`, `public/` hero `.webp`).
- **Libraries**: New `frontend/libs/shared/ui-helm/card/` (`@spartan-ng/helm/card`), new `frontend/libs/games/feature-catalog/` (`@game-atlas/games/feature-catalog`), and corresponding updates in `frontend/tsconfig.base.json`.
- **API / Backend**: Zero backend or OpenAPI changes; consumes existing `GET /games` via `@game-atlas/games/data-access`.
