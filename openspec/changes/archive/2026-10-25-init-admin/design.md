# Design

## Context

See proposal.md for motivation and scope. The workspace already uses Angular 22, Nx 23, TypeScript 6, Tailwind 4 and Spartan UI. Only `web` exists; it is statically prerendered. `games-data-access` currently validates a UUID-v4-only static JSON catalog, and its test builder compiles through the web application. The web server config has a catalog-specific HTTP backend. No Orval or TanStack dependency is currently declared.

The prepared ESLint configuration combines type/domain/platform tags. Applications use `scope:app`; games/maps/media can use shared utilities, and games/maps may consume media. The data-access library is presently `platform:web`, which must change to `platform:shared`.

The backend document at `http://localhost:5041/openapi/v1.json` is available and reports OpenAPI 3.1.1. Its tags are `Games`, `Maps`, `Category groups`, `Categories`, `Markers`, and `Media`. Games has list/create/update/delete operations, UUID-v7 server identities, string status/sorting values, optional merge-patch fields and ProblemDetails failures. No backend contract change is required.

## Goals / Non-Goals

**Goals:** Establish a small runnable application, repeatable contract generation and one usable shared query-options class, keeping generated transport separate from handwritten caching policy. Support later resource features without creating their libraries prematurely.

**Non-Goals:** Implement resource screens or mutations in the UI, introduce repositories/base-query classes/global stores, preserve the prototype catalog, or turn this foundation into a full design-system project. Backend persistence and deployment are outside this change. E2E tests are deferred by explicit user decision; use focused unit tests, lint, builds and live generation/proxy checks.

## Decisions

### 1. Add a browser-only administration application

Create `apps/admin` with Nx targets for serve, build, lint and tests. Use `type:app`, `scope:app`, `platform:admin`. The application owns bootstrap, HTTP/query providers, routes and its initial layout; a separate shell library provides no reuse yet and is unnecessary. Use lazy route entry points where useful, without introducing a `type:shell` tag.

Do not copy the public site's SSR/prerender or static-catalog backend. Admin is an SPA with a documented hosting fallback for application routes. Initial local assumptions are a separate development port (4201), English UI labels and the existing neutral color palette. Localization remains out of scope.

Alternative: extend web or reuse its static bootstrap. Rejected because administration uses a live API and has no current publication/prerender requirement.

### 2. Compose an accessible Spartan shell

Use the configured Spartan style and semantic tokens. Use Material Symbols Rounded SVG icons through `@ng-icons/material-symbols/round`, including internal icons in owned Helm components; register only the icons used. Read the installed CLI's component information and schemas before generating only the sidebar and its required supporting components. Keep reusable Helm code under the configured `libs/shared/ui` destination with `scope:shared`, `platform:shared`, as one non-buildable `ui-helm` Nx project with `type:ui`. Keep the Nx `src/lib` layout: owned Spartan code lives in `src/lib/helm`, our reusable components belong directly in `src/lib`, and theme CSS lives in `src/theme`. Configure `componentsPath: libs/shared/ui/src/lib/helm`, `generateAs: entrypoint` and retain separate `@spartan-ng/helm/*` entrypoints without component-specific project/configuration files. Check compatibility of the pinned CLI/Brain versions rather than copying selectors from a newer website blindly.

The global sidebar has Games and Media. The header contains its toggle, breadcrumbs and the route's title. Desktop can collapse to icons; narrow viewports use the sidebar overlay with focus handling and Escape dismissal. There is no invented account menu or dashboard. Games and Media initially render honest foundation pages, not tables of fake records. `/` redirects to `/games`; `/media` is the second destination, and unknown application routes get a small not-found state.

Maps later belong inside the selected game; groups/categories/markers belong inside the selected map. Do not add those routes or independent global entries now.

The header includes a controlled shared native radio-group theme switcher with browser-provided arrow-key and Tab navigation for Light, Default (system), and Dark. Admin owns a small ThemeSettings service using ngxtension `injectLocalStorage` with raw `game-atlas.theme` values (`light`, `system`, `dark`) and its default cross-tab synchronization. Update `<meta name="color-scheme">` to `light`, `light dark`, or `dark` in a render effect. CSS `light-dark()` tokens select the palette; Tailwind dark variants use the same meta value and the system media query only in system mode. Do not toggle a `.dark` class or implement a separate JavaScript system-theme listener. A minimal guarded script at the beginning of `<body>`, before `<app-root>`, reads the same key before Angular bootstraps; it never writes storage. Two theme-color meta tags suggest `#ffffff` and `#09090b` for browser chrome. In system mode the browser selects a tag through its media query; forced modes enable only the selected tag with `all` and disable the other with `not all`. The startup script and runtime service update the same tags without another system-theme listener. Browser UI coloring depends on browser/platform support. Invalid stored values fall back to system mode. The switcher and type are shared UI; browser storage stays in admin.

### 3. Replace the existing games data-access library in place

Retain `libs/games/data-access`, project name `games-data-access`, and `@game-atlas/games/data-access`. Retag it `type:data-access`, `scope:games`, `platform:shared`. Delete its obsolete model/parser/static loader and their tests. Export the generated game contracts/services and the handwritten query-options class through the library public API.

Suggested layout:

```text
libs/games/data-access/
  orval.config.ts
  project.json
  src/
    index.ts
    lib/
      generated/
        games/
        models/
      games-queries.ts
```

Remove or minimally replace obsolete public catalog components, routes, static JSON, prerender interception, tests and path aliases when they depend on deleted exports. Do not leave broken references or a stale GamesCatalog implementation solely to preserve the prototype. Keep any remaining web project buildable with a minimal neutral entry; no public-web redesign or backend integration is included.

Alternative: create `data-access-admin` alongside the static library. Rejected by the user's explicit request for one shared API library. Future web use is permitted by tags, not implemented here.

### 4. Generate transport with Orval, scoped by backend tags

Add compatible pinned Orval tooling. The initial library config selects `Games` from the live OpenAPI URL, uses `client: angular`, `mode: tags-split`, and explicitly selects HttpClient-based retrievals. Put service/model output under `src/lib/generated`. Use `useTypeOverInterfaces: true` and `enumGenerationType: union`; do not generate TanStack or httpResource wrappers. Runtime constants are handwritten only when a real consumer needs them, outside generated files.

Provide `games-data-access:generate-api`, runnable through npm/Nx. Generation is explicit, uncached because its HTTP input is not a tracked local file, and limited to owned generated paths. Commit generated sources and lockfile changes; normal builds must never invoke the network generator. Suppress changing generated dates/headers and verify regeneration against the unchanged document. A fetch/parse failure must produce a useful nonzero exit rather than fall back to the former fixture.

Keep generated code covered by type checking and formatting. If required, narrowly exclude only generated files from handwritten style rules incompatible with generator output; do not weaken boundary checks or linting for handwritten code globally. Generated output is changed through settings/regeneration, never manual patches.

Document the future tag ownership without creating unused clients: games selects `Games`; maps selects `Maps`, `Category groups`, `Categories`, `Markers`; media selects `Media`. Start with only the Games config/command. Referenced model duplication between future scopes is not solved by adding a speculative universal contracts library now.

### 5. Keep query options handwritten and application-neutral

Register HTTP and TanStack Query in admin. An injectable GamesQueries class injects the generated HTTP service and exposes list options using `queryOptions`. Keys include normalized request parameters, respecting backend defaults and unsorted behavior. There is no get-by-ID query because the backend does not expose that operation. CRUD mutation options and invalidation policy belong to the later CRUD change.

Convert the generated Observable to a Promise at the query boundary; preserve HTTP failures and ProblemDetails. Connect TanStack's AbortSignal to unsubscription, using a small helper only if it avoids actual duplication. Verify cancellation and settled subscriptions. Do not put query hooks, component state or additional repositories around generated transport. Do not layer httpResource caching over TanStack caching.

TanStack's Angular package is currently named `@tanstack/angular-query-experimental`; pin a compatible release and keep its coupling in handwritten option factories/providers. Configure QueryClient defaults in the application, not the shared domain library. Tests can disable retries without inventing a global production retry policy.

### 6. Separate contract-generation addressing from runtime addressing

The development OpenAPI URL is an input to generation only. Runtime addressing is consumer-configurable using supported Orval Angular configuration/DI; generated sources must not embed a permanently administration-specific origin. Prefer the generator's supported base-URL integration over a global interceptor that rewrites all outgoing URLs.

Admin uses `/api` as a development same-origin prefix, proxied to `http://localhost:5041` with the prefix stripped. Thus `/api/games` calls backend `/games`, while browser navigation to `/games` remains an SPA page. Configure only the dedicated API prefix in the proxy. Document production reverse-proxy/base-URL configuration rather than claiming that a development proxy ships with static output. Shared clients themselves do not require the proxy or admin providers when consumed by another application.

### 7. Capture conventions during implementation

Update `frontend/AGENTS.md` and `frontend/docs/frontend-architecture.md` with generated-versus-handwritten boundaries, tag ownership, query-option classes, union/type-alias defaults and explicit HTTP generation. Correct the existing global UUID-v4 wording: backend entities retain authoritative server IDs; admin must not generate or reject UUID-v7 identities. Describe remaining browser-only local IDs separately if needed.

Add concise frontend technology/context notes to root `openspec/config.yaml`, keeping detailed frontend coding rules in frontend AGENTS and preserving backend conventions. These implementation tasks are planned here; proposing this change does not edit those files.

## Risks / Trade-offs

- Live generation depends on backend availability and current state -> explicit uncached command, visible failure, committed generated sources and pinned tooling. A checked-in OpenAPI snapshot is deliberately deferred per user choice.
- Orval's handling of OpenAPI 3.1 nullable/optional models and polymorphic shared errors may differ by version -> inspect actual Games output and test PATCH payloads and ProblemDetails; fix generator configuration before considering custom transforms.
- Angular Query is marked experimental -> pin compatible dependencies and isolate integration in handwritten classes rather than generating application logic.
- Replacing the shared library invalidates the static prototype -> remove dependent obsolete pieces together and keep remaining project references coherent. Do not silently retain old contracts.
- Browser calls need appropriate API routing -> development proxy avoids cross-origin changes; document production routing separately. Do not change backend CORS or hardcode a deployment host.
- No authentication yet -> the shell does not imply an access-control boundary or simulate a login flow. Authentication remains separate work.

## Migration Plan

1. Add admin and required dependencies/components; preserve the prepared boundary changes.
2. Replace games data access and generate current Games contracts from the HTTP document.
3. Remove obsolete prototype dependents, wire the admin shell/providers and verify shared library isolation.
4. Validate build/lint/query behavior, regenerate without drift, and document commands, base URL and SPA hosting.
5. No database migration or data modification occurs. Rollback is reverting frontend source/dependency/config changes together; generated sources are included in that rollback.

## References

- [Orval Angular services](https://orval.dev/docs/guides/angular/)
- [Orval input tag filtering](https://orval.dev/docs/reference/configuration/input/)
- [Orval output and type-generation options](https://orval.dev/docs/reference/configuration/output/)
- [TanStack Angular query options](https://tanstack.com/query/latest/docs/framework/angular/guides/query-options)
- [TanStack Angular HttpClient integration](https://tanstack.com/query/latest/docs/framework/angular/angular-httpclient-and-other-data-fetching-clients)
- [Spartan sidebar](https://spartan.ng/components/sidebar)

## Implementation verification

- Admin shell: 13 unit tests passed, including mobile destination selection, Escape dismissal, focus return, theme restoration, persistence and cross-tab synchronization. Shared Games API/query tests: 12 passed. Retained web entry: 1 passed.
- Scoped Nx lint passed for admin, Games data access, retained web projects and the shared Helm project. The actual Nx boundary rule allowed both consumers of shared Games and rejected cross-platform, missing-tag, type and scope violations.
- Admin and web production builds passed with an unavailable OpenAPI generation URL; ordinary builds use generated source files. Admin has no SSR runtime output. The admin initial bundle is approximately 595 kB and triggers the existing 500 kB warning threshold; the threshold was not raised.
- Two HTTP generations produced identical sources and preserved handwritten code. An unavailable OpenAPI document correctly failed generation; generated sources were then restored by a successful generation. Orval cleans generated output before fetching, so developers must regenerate or restore tracked output after a failed generation.
- Live development checks confirmed `/api/games?offset=0&limit=1` returns backend JSON with unchanged parameters, while `/games` and `/media` serve SPA HTML. No backend source, migrations or persistent data were changed.
- E2E coverage is explicitly deferred. The newly scaffolded admin E2E project was removed; verification uses unit tests, lint, builds and HTTP checks.
