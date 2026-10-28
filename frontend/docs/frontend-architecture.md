# Frontend architecture and deployment

The workspace uses Angular 22, Nx 23, TypeScript 6, Tailwind CSS 4 and Spartan UI. Use npm from `frontend` and a Node.js version supported by the installed Angular build tools.

## Applications

`admin` is a browser SPA for content management. It has its own bootstrap, providers, routes and shell under `apps/admin`, independent of web. Development uses port 4201 and proxies `/api/**` and canonical `/media/files/**` to the backend at `http://localhost:5041`, removing the prefix. Page navigation to `/games` must never be proxied as an API request.

Build with `npm exec nx -- build admin`. Deploy `dist/apps/admin/browser` to static hosting with an `index.html` fallback for application routes and real 404s for missing assets. Production needs an explicit reverse-proxy or application base-URL configuration; the development proxy is not deployed. No production Node.js renderer is required.

Games remains a foundation destination. Media provides folder browsing, upload, metadata editing, cover selection, moving and confirmed deletion through the library API. The sidebar supports desktop collapse and mobile overlays. There is no dashboard, fake account or authentication flow. Maps will be reached through a game, with groups/categories/markers inside a selected map; those screens are not scaffolded yet.

`web` retains static prerendering and hydration, but currently has only a neutral entry. The earlier catalog component, fixture, parsing model and prerender HTTP interception have been removed. Public product features and publication strategy are separate work. Do not import administration-only code into web.

## API generation and query ownership

The existing `libs/games/data-access` is shared by both platforms. Keep the public alias `@game-atlas/games/data-access`. Orval generates Angular HttpClient services with tag splitting from the live OpenAPI document into `src/lib/generated`; it does not generate TanStack integration. The library's uncached `generate-api` target selects only Games and referenced schemas.

Generated sources are version-controlled and formatted by a post-generation hook. Normal builds never fetch OpenAPI; regeneration requires an available backend. Override `GAME_ATLAS_OPENAPI_URL` for another document source. Runtime API addressing is independently supplied through generated base-URL DI providers. Models prefer type aliases and enum unions; runtime constants require an actual consumer need.

The non-buildable `libs/shared/util` library (`type:util`, `scope:shared`, `platform:shared`) exports `toPromise` for Observable-to-Promise conversion at transport boundaries. Queries and uploads pass their AbortSignal; cancellation rejects with signal.reason and unsubscribes HTTP. Keep dialogs and guards as Observables.

Handwritten query-options classes colocate keys and request functions and use generated transport. Angular Observables are converted to Promises at this boundary, with cancellation unsubscription and unchanged HttpErrorResponse/ProblemDetails. Applications own QueryClient configuration. No generic repository, base-query framework, additional state store or overlapping httpResource cache is introduced.

Future maps clients select Maps, Category groups, Categories and Markers tags. Media selects Media. Add their libraries when needed; do not generate the whole backend into every domain. UUIDs and timestamps for backend entities remain authoritative server values; do not recreate server identifiers or enforce the prototype's UUID-v4 restriction.

Media uses a compact grid, context menus and folder drag/drop, a right-side details drawer, header breadcrumbs with overflow, an independently scrolling center and bottom pagination. Query and mutation options live in `MediaQueryOptions`; UI uses `injectQuery`/`injectMutation` without a separate cache service.

Media now uses shared `libs/media/data-access` and lazy admin-only `libs/media/feature-list`.
The feature follows the IAM folder structure in headless-cms: `src/lib/media.routes.ts` sits beside
the root `feature-list.ts`, `feature-list.html`, `feature-list.css` and `feature-list-query-params.ts` files and
component folders `media-details`, `media-card`, `media-upload-card`, `media-thumbnail`,
`folder-editor-dialog`, `confirmation-dialog` and `media-picker-dialog`. Each component's template,
styles and local helpers stay with that component. Shared feature helpers remain directly in `src/lib`.
Admin's root component hosts routing and toasts; `layout/layout` composes navigation and the header
around child routes. Pages supply breadcrumbs, actions and request loading through `PageHeaderPortal`.
Its mutation responses update query cache directly where list membership/order is known; complex
filtered or paginated changes narrowly invalidate affected lists. Admin configures XHR to report file
upload progress. Committed previews use NgOptimizedImage with API placeholders and the default loader;
custom image transformations are deferred. See each Media library README for operation limitations.

The Games library test builder uses the admin development build as a compilation harness, not as a runtime dependency. Shared source cannot import app code.

## Library boundaries

Every project has one type, domain scope and platform tag. These are independent dimensions enforced together by `eslint.config.mjs`.

| Source type | Allowed target types           |
| ----------- | ------------------------------ |
| app         | feature, data-access, ui, util |
| feature     | data-access, ui, util          |
| data-access | data-access, util              |
| ui          | ui, util                       |
| util        | util                           |

| Source scope | Allowed target scopes      |
| ------------ | -------------------------- |
| app          | games, maps, media, shared |
| games        | games, media, shared       |
| maps         | maps, media, shared        |
| media        | media, shared              |
| shared       | shared                     |

Reserve `scope:app` for composition roots. Category groups, categories and markers belong to `scope:maps`. A resource endpoint does not require an independent frontend scope.

| Source platform | Allowed target platforms |
| --------------- | ------------------------ |
| admin           | admin, shared            |
| web             | web, shared              |
| shared          | shared                   |

Use tags such as `type:data-access`, `scope:games`, `platform:shared`. A source missing a dimension has no matching cross-project import permission, and targets must satisfy every dimension. Circular checks remain enabled. These rules validate imports rather than unused metadata or tag cardinality.

Shared Helm primitives live under `libs/shared/ui` as one non-buildable Nx project, `ui-helm`, tagged `type:ui`, `scope:shared`, `platform:shared`. Spartan components and UI-specific helpers live under `src/lib/helm` as separate entrypoints such as `@spartan-ng/helm/button` and `@spartan-ng/helm/utils`; they do not have separate project/configuration files. Our own reusable components live under `src/lib`, grouped into functional folders when related files exist (no generic `components` directory) and are exported through `src/index.ts` as `@game-atlas/shared/ui`. Theme tokens live in `libs/shared/ui/src/theme/styles.css`, registered before application CSS in each app’s `project.json` build `styles` array. Use semantic colors and existing component variants. Admin owns a three-way theme preference, persisted through ngxtension `injectLocalStorage`. It updates a head color-scheme meta tag; shared `light-dark()` tokens and the Tailwind dark variant honor forced light/dark and live system mode. The shared `ThemeSwitcher` remains a controlled UI component. Browser-only packages such as the future map viewer initialize in browser render hooks.

## Verification

Media rename and move use optimistic TanStack snapshots with authoritative success reconciliation,
rollback and feature-owned Spartan error toasts. Concurrent changes are preserved through scoped refreshes.
Dialog/guard streams remain Observables. Templates longer than three lines are external HTML files;
multiline properties and TanStack declarations are separated by blank lines.

```sh
npm exec nx -- run-many -t lint
npm exec nx -- run-many -t test --projects=admin,games-data-access,web
npm exec nx -- test media-feature-list
npm exec nx -- run-many -t build --projects=admin,web
npm exec nx -- run games-data-access:generate-api
```

Test work is currently paused by the user's instruction. Do not add or run unit/E2E suites until explicitly requested for a task. Verify current changes through formatting, lint and production builds; do not treat those checks as interactive or unit-test verification.

Keep Nx caching for normal build/lint/test tasks. HTTP generation is explicitly uncached; do not add it as a prerequisite of build. Root ESLint and relevant project configurations remain task inputs. Prettier and ESLint follow frontend conventions; any narrow generated-source exceptions leave handwritten checks and domain/platform boundaries intact.

Deletion waits for server success before removing cached items; refusals keep the item visible and show an error toast.
