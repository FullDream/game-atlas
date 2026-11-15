# Design: Public Web SSR Shell and Home Game Catalog

## Context

`apps/web` currently ships only a static prerendered placeholder (`outputMode: "static"`, `RenderMode.Prerender`, hardcoded `<html class="dark">`, no HTTP client, no TanStack Query, and no routes). In contrast, the repository's backend API, `@game-atlas/games/data-access`, shared theme tokens in [`libs/shared/ui/src/theme/styles.css`](file:///D:/projects/game-atlas/frontend/libs/shared/ui/src/theme/styles.css), compact [`ThemeSwitcher`](file:///D:/projects/game-atlas/frontend/libs/shared/ui/src/lib/theme/theme-switcher.ts), [`MediaThumbnail`](file:///D:/projects/game-atlas/frontend/libs/shared/ui/src/lib/media-thumbnail/media-thumbnail.ts), and Spartan Helm primitives (excluding `card`) are already implemented. See `proposal.md` for motivation and scope.

## Goals / Non-Goals

**Goals:**

- Run `apps/web` as a true request-time SSR application (`outputMode: "server"`, `RenderMode.Server`) hosted on **Hono** (`hono` + `@hono/node-server`) behind Caddy.
- Render initial catalog data (`GET /games`), page titles, and user theme preference completely on the server before flushing HTML, with zero duplicate client fetch or theme icon flicker during hydration.
- Keep strict Nx module boundaries between `platform:web` and `platform:admin`, reusing shared `@game-atlas/games/data-access`, `@game-atlas/shared/ui`, and `@spartan-ng/helm/*` libraries.

**Non-Goals:**

- Filtering `draft` games on the backend or frontend (`GET /games` results are shown as returned; status badges are omitted on public cards).
- Implementing the full `/games/:slug` detail page or public map viewer (only a minimal SSR placeholder route is added).
- Adding multi-format `<picture>` hero variants or admin-managed hero media in this change (single static `.webp` + blur placeholder).

## Decisions

### 1. Hono SSR Runtime Behind Caddy (`server.ts`)

- **Decision**: Use `Hono` + `@hono/node-server` wired to Angular 22's `AngularAppEngine` and `createRequestHandler(app.fetch)`.
- **Rationale**: Angular 22's `AngularAppEngine` natively consumes Web Standard `Request` and returns Web Standard `Response`. Hono aligns directly with that interface without Node-to-Express or Fastify adapter boilerplate. Because Caddy sits in front in production to terminate TLS, apply `zstd`/`gzip` compression, and reverse-proxy `/api`, `/media/files`, and `/map-tiles` to ASP.NET Core, Node does not need compression or HTTP-proxy middleware.
- **Standalone Node Serving & Caching**: When executed as the main module (`isMainModule(import.meta.url)`), Hono serves `dist/apps/web/browser` via `serveStatic`, sets `Cache-Control: public, max-age=31536000, immutable` on hashed asset paths (`.js`, `.css`, media assets under browser output excluding `index.html`), and delegates remaining routes to `angularApp.handle(c.req.raw)`.
- **Proxy Awareness**: Instantiate `AngularAppEngine` with `trustProxyHeaders` / `allowedHosts` configurable via `NG_ALLOWED_HOSTS` (with `localhost` / `127.0.0.1` permitted in development) so forwarded host and protocol headers from Caddy resolve accurately.
- **Dev Proxy**: Add `apps/web/proxy.conf.cjs` matching `apps/admin/proxy.conf.cjs` (`/api/**`, `/media/files/**`, `/map-tiles/**` -> `http://localhost:5041`) and attach it to `web:serve` in `apps/web/project.json`.
- **Alternatives considered**: Express (legacy middleware API, not installed) and Fastify (stronger when Node itself proxies APIs or serves precompressed static files directly, unnecessary overhead behind Caddy).

### 2. Per-Request `QueryClient`, Base URL Resolution, and `TransferState` Hydration

- **Decision**:
  - Register `provideHttpClient(withFetch())` in `apps/web/src/app/app.config.ts`.
  - Provide `QueryClient` using an `InjectionToken<QueryClient>` factory (`new QueryClient(...)`) via `provideTanStackQuery(WEB_QUERY_CLIENT)` instead of a module-level singleton so concurrent SSR requests never share query cache state.
  - Provide `provideGamesBaseUrl('/api')` in `app.config.ts` and override it in `apps/web/src/app/app.config.server.ts` with `provideGamesBaseUrl(process.env['GAME_ATLAS_API_URL'] ?? GAMES_SERVER_URL)` (overriding `GAMES_BASE_URL` directly because `api.base-url.ts` gives direct values precedence over `GAMES_BASE_URL_RESOLVER`).
  - Dehydrate `QueryClient` on the server via `TransferState.onSerialize` (`@tanstack/query-core`'s `dehydrate`) and rehydrate on client initialization (`hydrate`) inside an environment initializer.
- **Rationale**: Angular's built-in `HttpTransferCache` keys cached responses by full request URL. Because SSR requests target `http://localhost:5041/games?...` while the browser targets `/api/games?...`, `HttpTransferCache` alone would miss on hydration and trigger a redundant `/api/games` request. Dehydrating the TanStack `QueryClient` by query key (`['games', 'list', normalizedParams]`) bridges the URL difference cleanly and preserves `staleTime: 30_000`.

### 3. Pre-Flush Cookie-Driven SSR Theme (`game-atlas.theme`)

- **Decision**: Create `apps/web/src/app/theme-settings.ts` reading/writing `game-atlas.theme` (`'light' | 'dark' | 'system'`, default `'system'`) in cookies (`Path=/; SameSite=Lax; Max-Age=31536000`, non-`HttpOnly`).
- **SSR Execution**: Inject `REQUEST` (`@angular/core`, optional) on the server to read the incoming `Cookie` header during `ThemeSettings` construction and immediately update `<meta name="color-scheme">` and `<meta id="theme-color-light|dark" name="theme-color" media="...">` via `Meta`. In the browser, read/write `document.cookie` (and mirror to `localStorage` only as passive compatibility with `admin`), plus keep a tiny blocking `<script>` in `web/src/index.html` solely as fallback if `localStorage` has a value before any cookie exists.
- **Rationale**: Shared theme tokens in `styles.css` use `light-dark(...)` and `@custom-variant dark` driven by `<meta name="color-scheme">`. Evaluating the cookie on the server guarantees both the right CSS palette (`<head>`) and the right [`ThemeSwitcher`](file:///D:/projects/game-atlas/frontend/libs/shared/ui/src/lib/theme/theme-switcher.ts) icon in SSR HTML before first paint, avoiding hydration flicker across all browsers.

### 4. Nx Module Topology and Shared Helm Card Generation

- **Decision**:
  - Generate `@spartan-ng/helm/card` via Spartan CLI into `libs/shared/ui-helm/card` (`type:ui, scope:shared, platform:shared`).
  - Create feature library `libs/games/feature-catalog` (`type:feature, scope:games, platform:web`, selector prefix `games`) rather than modifying or relaxing module boundaries on `libs/games/feature-list` (`platform:admin`).
- **Rationale**: Keeps `platform:admin` and `platform:web` strictly isolated under `frontend/eslint.config.mjs` while sharing `@game-atlas/games/data-access`, `@game-atlas/shared/ui`, and `@spartan-ng/helm/*`.

### 5. Home Catalog Layout, Debounced Search, and Pagination

- **Decision**:
  - Generate and optimize the approved hero key-art illustration into a single `.webp` asset under `apps/web/public/` accompanied by a compact base64 blur placeholder (`data:image/webp;base64,...`) rendered with `NgOptimizedImage` (`priority` / `fill` / `[placeholder]`) and directional `bg-background` gradients for legibility in both light and dark modes.
  - Omit the `Multilingual` feature callout; display `Unlimited markers` and `Multiple maps`.
  - Synchronize `search`, `offset`, `limit` (`[24, 48, 72, 96]`, default `24`), `sortBy` (`name | createdAt | updatedAt`), and `sortDirection` (`asc | desc`) with URL query params using `linkedQueryParam`, mirroring [`FeatureList`](file:///D:/projects/game-atlas/frontend/libs/games/feature-list/src/lib/feature-list.ts).
  - Debounce (`250ms`) interactive typing from the hero search input before writing `search` and resetting `offset` to `0`, keeping the displayed input responsive without firing intermediate API calls per keystroke.
  - Render the grid with `1 -> 2 -> 3 -> 4` columns so full pages of `24`, `48`, `72`, or `96` cards leave zero trailing grid holes at every column count.
  - Build `games-catalog-card` with `HlmCard`, top `banner` (`ui-media-thumbnail` or styled gradient + Material icon fallback when `null`), bottom `cover` + title + `<maps.length> map(s) · <markersCount> markers` + trailing icon link to `['/games', game.slug]`, and no status badge.

## Risks / Trade-offs

- **Draft games visible publicly** → Accepted for this phase per requirement; `GET /games` returns both draft and published games, and hiding drafts cleanly belongs to a future backend spec/change so server `total` and pagination stay accurate.
- **Placeholder `/games/:slug` destination** → Clicking a card navigates to a minimal SSR placeholder showing the game slug and a back-to-catalog link until the public game/map page is built.
- **Empty browser hydration without duplicate fetch relies on TanStack Query `TransferState`** → Ensure non-error initial queries dehydrate cleanly and verify in a browser/SSR check that `GET /api/games` is not immediately re-requested on initial load.
- **Reverse-proxy caching of personalized HTML** → If Caddy HTML caching is ever added later, configure `Vary: Cookie` (or bypass cache when `game-atlas.theme` is present); by default dynamic `RenderMode.Server` responses are uncached.
