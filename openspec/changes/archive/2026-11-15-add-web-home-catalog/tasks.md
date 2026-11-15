# Tasks: Public Web SSR Shell and Home Game Catalog

## 1. Dependencies, Shared Card Primitive, and Hero Asset

- [x] 1.1 Install `hono` and `@hono/node-server` in `frontend/package.json` and verify `npm ls hono @hono/node-server` succeeds from `frontend/`
- [x] 1.2 Generate the Spartan Helm card library (`@spartan-ng/helm/card`) under `frontend/libs/shared/ui-helm/card` matching `components.json` and existing `ui-helm` tags/build configuration, and verify `npm exec nx -- build card` and `npm exec nx -- lint card` pass
- [x] 1.3 Generate `libs/games/feature-catalog` (`type:feature`, `scope:games`, `platform:web`, selector prefix `games`) with `tsconfig.base.json` mapping `@game-atlas/games/feature-catalog`, and verify `npm exec nx -- lint games-feature-catalog` passes
- [x] 1.4 Convert the approved hero key-art illustration into a static `.webp` asset in `frontend/apps/web/public/` alongside its compact base64 blur placeholder constant, and verify the file builds into `dist/apps/web/browser`

## 2. Hono SSR Server and `apps/web` Foundations

- [x] 2.1 Create `frontend/apps/web/proxy.conf.cjs` (`/api/**`, `/media/files/**`, `/map-tiles/**` -> `http://localhost:5041`), update `frontend/apps/web/project.json` for `"outputMode": "server"`, `ssr.entry: "apps/web/src/server.ts"`, and dev `serve.options.proxyConfig`, and set `RenderMode.Server` on `**` in `frontend/apps/web/src/app/app.routes.server.ts`; verify `apps/web/project.json` parses cleanly
- [x] 2.2 Implement `frontend/apps/web/src/server.ts` using `Hono`, `AngularAppEngine` (with Caddy proxy header / `NG_ALLOWED_HOSTS` support), `createRequestHandler(app.fetch)`, and `@hono/node-server` (`serve` + `serveStatic` with `immutable` caching for hashed browser assets); verify `npm exec nx -- build web` emits `dist/apps/web/server/server.mjs`
- [x] 2.3 Configure `frontend/apps/web/src/app/app.config.ts` and `app.config.server.ts` with `provideHttpClient(withFetch())`, per-request `QueryClient` token factory + `TransferState` dehydration/hydration, `provideSpartanHlm()`, `WebTitleStrategy`, browser `provideGamesBaseUrl('/api')`, and server `provideGamesBaseUrl(process.env['GAME_ATLAS_API_URL'] ?? GAMES_SERVER_URL)`; verify `npx tsc --noEmit -p apps/web/tsconfig.app.json` passes
- [x] 2.4 Implement cookie-backed `frontend/apps/web/src/app/theme-settings.ts` (`game-atlas.theme`, reading `REQUEST` cookie during SSR and `document.cookie` on the client, updating `<meta name="color-scheme">` and `<meta name="theme-color">` before HTML serialization) and update `frontend/apps/web/src/index.html` metadata + optional `localStorage` compatibility fallback; verify SSR output renders the cookie-selected `<meta name="color-scheme">` directly in `<head>`
- [x] 2.5 Update `frontend/apps/web/src/app/app.ts`, `app.html`, and `app.routes.ts` with the semantic top header (GameAtlas logo link to `/` + compact `ThemeSwitcher`), skip-to-content link, `<router-outlet>` in `<main>`, `/` catalog route, and minimal SSR `/games/:slug` placeholder component; verify `npm exec nx -- lint web` passes

## 3. Public Home Game Catalog (`games/feature-catalog`)

- [x] 3.1 Build the Hero section in `libs/games/feature-catalog` displaying the static `.webp` background + blur placeholder, theme-adaptive `bg-background` gradient fade, headline, subtitle, `Unlimited markers` and `Multiple maps` callouts (without `Multilingual`), and the pill `HlmInputGroup` search input with `250ms` debounce resetting `offset` to `0`; verify typing updates the `search` query param after debounce
- [x] 3.2 Implement catalog query state and toolbar using `GamesQueryOptions.getList`, `linkedQueryParam` (`search`, `offset`, `limit` from `[24, 48, 72, 96]` defaulting to `24`, `sortBy`, `sortDirection`), and `HlmSelect` with all 6 sort options; verify URL query params round-trip on SSR and client updates
- [x] 3.3 Implement `games-catalog-card` using `@spartan-ng/helm/card` (`hlm-card`), `MediaThumbnail` for `banner` with a styled gradient/icon fallback when `banner === null`, `cover` thumbnail, game title, `<maps.length> map(s) · <markersCount> markers` metadata, trailing link to `['/games', game.slug]`, and zero status badges or client-side status filtering; verify both bannered and bannerless cards render cleanly in light and dark modes
- [x] 3.4 Wire the responsive `1 -> 2 -> 3 -> 4`-column grid, `hlm-skeleton` loading placeholders, `ui-error-state` retry banner, `hlm-empty` zero-result state with search clear button, and `HlmNumberedPaginationQueryParams` (`[pageSizes]="[24, 48, 72, 96]"`) with out-of-range offset guard; verify `npm exec nx -- lint games-feature-catalog` passes

## 4. Documentation and End-to-End Verification

- [x] 4.1 Update `docs/frontend/architecture.md` to document the public `web` Hono SSR architecture, cookie-driven SSR theme synchronization, `@spartan-ng/helm/card`, and `libs/games/feature-catalog`, and verify `node scripts/check-docs.mjs` passes
- [x] 4.2 Run formatting, lint, TypeScript checks, and production SSR build (`npm exec prettier -- --check ...`, `npm exec nx -- lint web`, `npm exec nx -- lint games-feature-catalog`, `npm exec nx -- lint card`, `npm exec nx -- build web`) from `frontend/`, and verify live Hono SSR rendering (`/`, `/?search=...`, `/games/:slug`, cookie `game-atlas.theme=dark`, hydration without duplicate `GET /api/games` fetch) against a running backend
