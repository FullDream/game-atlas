# Frontend architecture

Paths and commands in frontend documents are relative to `frontend/` unless stated otherwise. See the [system architecture](../architecture.md), [Angular conventions](angular.md), [data access](data-access.md) and [UI guidelines](ui.md).

The workspace uses Angular 22, Nx 23, TypeScript 6, Tailwind CSS 4 and Spartan UI. Use npm from `frontend` and a Node.js version supported by the installed Angular build tools.

## Applications

`admin` is a browser SPA for content management. It has its own bootstrap, providers, routes and shell under `apps/admin`, independent of web. Development uses port 4201 and proxies `/api/**` and canonical `/media/files/**` to the backend at `http://localhost:5041`, removing the prefix. Page navigation to `/games` must never be proxied as an API request.

Build with `npm exec nx -- build admin`. Deploy `dist/apps/admin/browser` to static hosting with an `index.html` fallback for application routes and real 404s for missing assets. Production needs an explicit reverse-proxy or application base-URL configuration; the development proxy is not deployed. No production Node.js renderer is required.

Games remains a foundation destination. Media provides folder browsing, upload, metadata editing, cover selection, moving and confirmed deletion through the library API. The sidebar supports desktop collapse and mobile overlays. There is no dashboard, fake account or authentication flow. Maps will be reached through a game, with groups/categories/markers inside a selected map; those screens are not scaffolded yet.

`web` retains static prerendering and hydration, but currently has only a neutral entry. The earlier catalog component, fixture, parsing model and prerender HTTP interception have been removed. Public product features and publication strategy are separate work. Do not import administration-only code into web.

## Module boundaries

- Tag each project with exactly one type (`type:app`, `type:feature`, `type:ui`, `type:data-access`, or `type:util`), one domain scope, and one platform. Do not use `type:shell`.
- Domain scopes are `scope:games`, `scope:maps`, `scope:media`, and `scope:shared`. Category groups, categories, and markers belong to `scope:maps`. Reserve `scope:app` for application composition roots.
- Platforms are `platform:admin`, `platform:web`, and `platform:shared`. Keep admin and web libraries isolated; platform-shared libraries depend only on other platform-shared libraries.
- Games and maps may depend on media and domain-shared libraries; media depends only on media and domain-shared libraries. Domain-shared libraries depend only on other domain-shared libraries.
- Follow the dependency directions in `eslint.config.mjs`; type, scope and platform constraints all apply. A project missing any dimension has no permission for cross-project imports.
- Prefer non-buildable internal libraries. Create libraries only when needed.
- Admin is a browser SPA in `apps/admin`. Web remains a separate statically prerendered neutral entry while its product features are rebuilt. Do not restore the removed static GamesCatalog, its fixture or UUID-v4-only parser.

## Feature layout

- Place a multi-file component used by only one parent in its own folder inside that parent's functional folder. Keep components shared by multiple parents in a common functional area, each in its own folder when they have multiple files. Do not add a redundant grouping directory to a data-access library when its handwritten files already form one feature; place them directly in src/lib beside generated.
- Group frontend files by functionality. When a component or feature has multiple related files (including templates and tests), colocate them in a dedicated functional folder. Avoid flat resource directories and type-based folders such as `components`, `services` or `models`.

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

## Dependency matrix

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
