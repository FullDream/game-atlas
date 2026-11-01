# GameAtlas administration

Browser-only Angular SPA with responsive Spartan navigation, Games listing and dedicated creation/editing pages with publication and confirmed deletion and a Media library for browsing folders, uploading files, editing metadata, moving items and confirmed deletion. Authentication is not implemented yet.

The root `App` hosts routing and toasts. `layout/layout` owns the persistent sidebar and page header around child routes, following the frontend layout and IAM feature organization in [headless-cms](https://github.com/FullDream/headless-cms/tree/main/frontend). Features contribute breadcrumbs, actions and loading state through `PageHeaderPortal`; navigation loading is tracked in the layout. Media is lazy loaded from `@game-atlas/media/feature-list`.

Run from `frontend`:

```sh
npm exec nx -- serve admin
npm exec nx -- build admin
npm exec nx -- test admin
npm exec nx -- lint admin
```

Development uses `http://localhost:4201`. `/api/**` is proxied to `http://localhost:5041` with `/api` stripped. Thus `/api/games` calls the backend while `/games` is a page route. Do not proxy page routes directly. The shell needs no backend connection and does not display invented records or a simulated user.

Production output is `dist/apps/admin/browser`. Configure the host to serve `index.html` for application routes, including direct `/games` and `/media` navigation. Keep missing assets as 404. There is no production Node.js renderer. Configure a reverse proxy for `/api/` separately; the development proxy is not included in production output. A different deployment can change the `provideGamesBaseUrl` provider in application configuration without regenerating clients.

`npm exec nx -- run admin:serve-static` previews production files on port 4301 with an SPA fallback. Use it for a local production preview.

Production size budgets warn at 1.2 MB and fail at 1.5 MB for the initial bundle, and warn at 6 KB and fail at 8 KB for each component stylesheet. These thresholds accommodate the current roughly 1 MB administration bundle and 5.5 KB component styles while retaining growth checks. The map engine remains lazy loaded.

The sidebar collapses to icons on desktop and opens as a dismissible overlay on narrow screens. Menu selection closes the mobile overlay; Escape dismisses it and returns focus. Keyboard users can reach a skip-to-content link. Query caching is supplied once at the application root; no domain library owns a global QueryClient.

The sidebar theme control offers Light, Default (system) and Dark. `ThemeSettings` uses ngxtension `injectLocalStorage` to persist the raw `game-atlas.theme` value and synchronize changes between tabs. The color-scheme meta tag is `light`, `light dark` or `dark`; CSS `light-dark()` tokens follow the active scheme, including live system changes. A synchronous script at the beginning of `<body>`, before `<app-root>`, reads the stored preference before Angular bootstraps. Unknown saved values fall back to system mode.

Matching `theme-color` hints suggest white or dark zinc for supporting browser toolbars. System mode uses media queries; explicit Light/Dark activates only the selected hint. Both the early startup script and runtime preference changes keep the hints synchronized. Browsers decide which surrounding UI, if any, uses the suggested color.

## Games routes

/games hosts the searchable, sortable, paginated catalog. /games/new creates a draft; /games/:id loads its metadata directly through GET /games/{id}, including after reload. Cover selection uses the existing Media library. Status changes are saved with metadata. Deletion confirms removal of all game-owned maps and content while keeping library media. Public web remains separate work.

## Maps management

Admin composes /games/:id/maps from maps-feature-list inside the Games editor; neither domain imports the other. Add map replaces Save changes on Maps, and Save changes appears only on General. Modal creation takes name and dimensions, editing renames with read-only dimensions, and deletion confirms cascading map-content removal while retaining library assets. Rows show created/updated dates and style snapshots. There is no socket connection or background status refresh; Open editor leads to /games/:gameId/maps/:mapId/edit outside the Games settings tabs, with a shared OpenLayers viewport, style list and modal creation/editing plus confirmed style deletion. Search and filtering remain placeholders. Admin binds Maps API addressing in the lazy Maps route. Its mutation-cache callback invalidates only the exact game-detail query after successful map create/update/delete. The Maps list and Games list are not refetched by this callback.

## Map tile hosting

Development forwards /map-tiles/** unchanged to http://localhost:5041, just like canonical media paths. Production must forward /map-tiles/ and /media/files/ unchanged to the backend, separately from /api/ prefix removal. Do not send tile requests to the SPA fallback. Returned tile templates are authoritative and immutable.

The map editor displays processing snapshots without progress percentages, sockets, polling or automatic focus/reconnect refresh. Reload or return to the editor to load a later snapshot.
