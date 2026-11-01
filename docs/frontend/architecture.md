# Frontend architecture

Paths and commands in frontend documents are relative to `frontend/` unless stated otherwise. See the [system architecture](../architecture.md), [Angular conventions](angular.md), [data access](data-access.md) and [UI guidelines](ui.md).

The workspace uses Angular 22, Nx 23, TypeScript 6, Tailwind CSS 4 and Spartan UI. Use npm from `frontend` and a Node.js version supported by the installed Angular build tools.

## Applications

`admin` is a browser SPA for content management. It has its own bootstrap, providers, routes and shell under `apps/admin`, independent of web. Development uses port 4201 and proxies `/api/**` and canonical `/media/files/**` to the backend at `http://localhost:5041`, removing the prefix. Page navigation to `/games` must never be proxied as an API request.

Build with `npm exec nx -- build admin`. Deploy `dist/apps/admin/browser` to static hosting with an `index.html` fallback for application routes and real 404s for missing assets. Production needs an explicit reverse-proxy or application base-URL configuration; the development proxy is not deployed. No production Node.js renderer is required.

Games provides a searchable, sortable, paginated catalog and dedicated /games/new and /games/:id editors, with library image selection, draft/publication controls and confirmed cascading deletion. Media provides folder browsing, upload, metadata editing, cover selection, moving and confirmed deletion through the library API. The sidebar supports desktop collapse and mobile overlays. There is no dashboard, fake account or authentication flow. Maps will be reached through a game, with groups/categories/markers inside a selected map; the map list now supports modal creation, rename and confirmed deletion while the map viewer and content editors remain deferred.

`web` retains static prerendering and hydration, but currently has only a neutral entry. The earlier catalog component, fixture, parsing model and prerender HTTP interception have been removed. Public product features and publication strategy are separate work. Do not import administration-only code into web.

## Module boundaries

- Tag each project with exactly one type (`type:app`, `type:feature`, `type:ui`, `type:data-access`, or `type:util`), one domain scope, and one platform. Do not use `type:shell`.
- Domain scopes are `scope:games`, `scope:maps`, `scope:media`, and `scope:shared`. Category groups, categories, and markers belong to `scope:maps`. Reserve `scope:app` for application composition roots.
- Platforms are `platform:admin`, `platform:web`, and `platform:shared`. Keep admin and web libraries isolated; platform-shared libraries depend only on other platform-shared libraries.
- Domain libraries depend only on their own domain and shared libraries. Games, maps and media do not import one another. Cross-domain integration is wired at the application composition root, using public query keys for cache invalidation or shared contracts and adapters where needed. Domain-shared libraries depend only on other domain-shared libraries.
- Follow the dependency directions in `eslint.config.mjs`; type, scope and platform constraints all apply. A project missing any dimension has no permission for cross-project imports.
- Prefer non-buildable internal libraries. Create libraries only when needed.
- Admin is a browser SPA in `apps/admin`. Web remains a separate statically prerendered neutral entry while its product features are rebuilt. Do not restore the removed static GamesCatalog, its fixture or UUID-v4-only parser.

## Feature layout

- Place a multi-file component used by only one parent in its own folder inside that parent's functional folder. Keep components shared by multiple parents in a common functional area, each in its own folder when they have multiple files. Do not add a redundant grouping directory to a data-access library when its handwritten files already form one feature; place them directly in src/lib beside generated.
- A feature library already provides the functional boundary for its root component. Place the root component, template, styles, route configuration and root-specific helpers directly in `src/lib`; do not add a redundant `src/lib/feature-list`, `src/lib/game-editor` or `src/lib/game-create` wrapper. Name root component files and classes after the library: `feature-list.ts` / `FeatureList`, `feature-editor.ts` / `FeatureEditor`, `feature-create.ts` / `FeatureCreate`. Keep nested components in their own functional folders beside the root files, such as `game-card` or `game-general`.
- Group frontend files by functionality. Colocate each nested or reusable multi-file component in its own functional folder; the feature library’s `src/lib` is the root component’s folder. Avoid type-based folders such as `components`, `services` or `models`.

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
| games        | games, shared              |
| maps         | maps, shared               |
| media        | media, shared              |
| shared       | shared                     |

Reserve `scope:app` for composition roots. Category groups, categories and markers belong to `scope:maps`. A resource endpoint does not require an independent frontend scope.

| Source platform | Allowed target platforms |
| --------------- | ------------------------ |
| admin           | admin, shared            |
| web             | web, shared              |
| shared          | shared                   |

Use tags such as `type:data-access`, `scope:games`, `platform:shared`. A source missing a dimension has no matching cross-project import permission, and targets must satisfy every dimension. Circular checks remain enabled. These rules validate imports rather than unused metadata or tag cardinality.

## Games management

Admin composes Games routes from three independent libraries: libs/games/feature-list at /games, libs/games/feature-create at /games/new and libs/games/feature-editor at /games/:id. All use type:feature, scope:games and platform:admin; feature libraries do not import one another. List/create export their lazy routes. Admin composes the editor shell, guard and settings children, including the independent Maps feature, at the application root. libs/games/ui (type:ui, scope:games, platform:admin) owns GameFields, its skeleton mode, the form model/schema; it depends only on shared UI/util contracts and never imports data-access. Shared libs/games/data-access remains the only Games transport/options boundary and exports getGameErrorMessage for both form features. Detail queries use a cached list item and its original dataUpdatedAt when available. Successful creation seeds the persisted detail; updates apply known detail metadata optimistically with rollback; deletion retains data until server success, then removes detail and reconciles lists. Use built-in per-game TanStack mutation scopes and feature submission guards instead of custom promise queues.

ConfirmationDialog, MediaThumbnail and the ready-to-use MediaPickerDialog live in shared/ui. The picker calls injectQuery and injectMutation internally through MEDIA_PICKER_SOURCE, whose contract lives in shared/util. MediaPickerAdapter in Media data-access implements the port; apps/admin binds it using useExisting. Domain consumers never import Media code. Do not add MediaPickerView/MediaPickerSession controllers or domain-specific UI libraries to bridge domains.

Games uses the reference card/settings layout: Grid/List views, card cover and banner area, statistic placeholders and map previews; the editor has General, Maps, SEO and Danger zone tabs. Game metadata fields are API-backed; SEO and formatting controls remain deferred. The Maps tab is owned by maps/feature-list and supports modal creation, name-only editing and confirmed deletion. Dimensions are immutable. Map rows show server dates and style status snapshots without sockets, polling or automatic status refreshes. Open editor remains disabled.

## Maps management integration

Admin composes the Games editor children and binds the inherited route game ID to maps/feature-list. General owns its native form element; the editor retains its model across tab navigation. Shared header contributions inherit parent breadcrumbs/loading and restore parent actions when removed. Save changes appears only on General; Maps contributes Add map. Admin invalidates only the exact game-detail query after successful map create/update/delete through its QueryClient mutation-cache callback. The active detail refreshes summaries/counts without resetting unsaved form values. Games list caches are left unchanged. No domain imports another domain.
