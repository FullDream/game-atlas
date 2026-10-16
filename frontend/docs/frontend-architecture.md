# Frontend architecture and deployment

Game Atlas uses Nx 23, Angular 22, TypeScript 6, Tailwind CSS 4 and Spartan UI.
Run commands from `frontend` with npm. The installed Angular build tools require
Node.js `^22.22.3 || ^24.15.0 || >=26.0.0`; Node 24.13 is below that supported range.

## Static production website

`npm exec nx build web` produces `dist/apps/web/browser`. Deploy the contents of
that directory to static hosting. Node.js is used for the build and prerendering,
but neither Node.js nor ASP.NET Core is required by the deployed website.
`main.server.ts`, `app.config.server.ts` and `@angular/ssr` remain build-time
prerendering infrastructure; there is no production request handler.

The home catalog page is prerendered by the existing `RenderMode.Prerender` catch-all.
Client hydration and event replay remain enabled. Public assets go in
`apps/web/public` and are copied into the deployment directory. Published map
content will be loaded from static JSON files in that directory.

The catalog is lazy-loaded at `/` from `games-feature-catalog`. Its JSON is served
at build time by the app's prerender-only HTTP backend and transferred to the
browser through Angular's HTTP cache. Rebuild HTML and JSON together on publish.
There are no game-detail or map routes yet.

When game-detail and map features exist, add their lazy routes to `app.routes.ts`.
Parameterized routes need explicit server-route entries before the catch-all,
with `getPrerenderParams()` reading the published content manifest at build time.
Do not rely on the local editor API for production builds. Choose client or no
fallback deliberately for parameters absent from the manifest; static hosting
cannot perform request-time server rendering.

For direct navigation, configure the host to serve each generated
`<route>/index.html`. If client-only paths are introduced, configure an SPA
fallback to the generated `index.csr.html` shell for application paths that were
not prerendered; keep `index.html` as the prerendered home page. Keep missing JSON, images and
other assets as real 404 responses rather than rewriting them to HTML. Hosts
without rewrite support need prerendered files for every directly navigable URL
(or a separately chosen hash-routing strategy). Subdirectory deployments also
need the matching Angular base URL and asset paths.

Preview the static build with `npm exec nx run web:serve-static`. Its local
SPA fallback is a development convenience, not a configuration applied to the
hosting provider. Playwright uses this target to exercise production files.

The current preview target falls back to the home page. If client-only routes
are added, update that preview fallback to the CSR shell as well.

## Browser-only functionality

Future OpenLayers rendering and Dexie progress storage must initialize in the
browser. Use `afterNextRender()` for DOM work and dynamically import packages
that access browser globals there, rather than importing them at module scope.
Keep the initial prerendered and client templates consistent for hydration.
Load public JSON independently of local user progress; browser state is not
available during prerendering. These packages are not installed yet.

## Planned libraries

The two `games` libraries exist. The `maps` and `progress` directories remain
plans, not a required initial scaffold; create libraries only when needed:

```text
libs/
├── games/
│   ├── feature-catalog/
│   └── data-access/
├── maps/
│   ├── feature-viewer/
│   ├── data-access/
│   ├── ui/
│   └── util/
└── progress/
    └── data-access/
```

Applications configure routing and compose features. Features implement user
workflows, data-access libraries retrieve/persist data, UI libraries contain
presentation components, and utility libraries provide pure helpers. Prefer
ordinary non-buildable internal libraries. Add a separate build only for a
concrete publishing or distribution requirement; no model libraries are needed
yet.

Each project needs one supported type tag and one supported scope tag:

| Source type        | Allowed dependency types       |
| ------------------ | ------------------------------ |
| `type:app`         | feature, data-access, ui, util |
| `type:feature`     | data-access, ui, util          |
| `type:data-access` | data-access, util              |
| `type:ui`          | ui, util                       |
| `type:util`        | util                           |

| Source scope   | Allowed dependency scopes |
| -------------- | ------------------------- |
| `scope:web`    | web, shared               |
| `scope:editor` | editor, shared            |
| `scope:shared` | shared                    |

Both dimensions apply together. ESLint uses Nx `allSourceTags` constraints so a
source missing either dimension has no matching permission for cross-project
imports. Targets must satisfy both dimensions too. Circular dependency checks
remain enabled. This checks imports, not unused project metadata; it is not a
general-purpose validator of tags or non-TypeScript dependencies.

The future editor application uses `type:app` and `scope:editor` and talks to a
separate local ASP.NET Core API backed by SQLite. The public web application
cannot import editor-only libraries. Shared map UI/utilities use `scope:shared`
so both applications can reuse them. Map/progress browser storage and viewer
workflows normally use `scope:web`; choose scopes by consumers, not directory
names. Shared code must not import either application's data-access layer.

## Validation

```sh
npm exec nx build web
npm exec nx lint web
npm exec nx test web
npm exec nx e2e web-e2e
```

Install Playwright browser binaries when setting up a new environment:
`npm exec playwright install`. E2E runs Chromium, Firefox and WebKit against the
static build on port 4200; stop any unrelated server on that port first.

Nx preserves caching for build, lint, unit and E2E tasks. Project inputs include
project configuration and styles; the root PostCSS file is a shared input. The
root flat ESLint config is an explicit lint input, and project ESLint configs are
covered by project inputs. Nx also hashes relevant root TypeScript configuration
and package dependencies. Include future root config imports in task inputs when
they are added.

Keep ECMAScript `#private`, the ban on leading underscores, explicit function
return types and the existing strict application TypeScript settings. Spartan's
theme tokens are defined in `styles.css`; its preset maps those tokens to
Tailwind utilities. Generate Helm components only for an actual requested UI.
