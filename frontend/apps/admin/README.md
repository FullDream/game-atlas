# GameAtlas administration

Browser-only Angular SPA. The first version provides Games and Media foundation pages, responsive Spartan navigation and the API/query infrastructure. Resource CRUD, media browsing and authentication are not implemented yet.

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

The sidebar collapses to icons on desktop and opens as a dismissible overlay on narrow screens. Menu selection closes the mobile overlay; Escape dismisses it and returns focus. Keyboard users can reach a skip-to-content link. Query caching is supplied once at the application root; no domain library owns a global QueryClient.

The header theme control offers Light, Default (system) and Dark. `ThemeSettings` uses ngxtension `injectLocalStorage` to persist the raw `game-atlas.theme` value and synchronize changes between tabs. The color-scheme meta tag is `light`, `light dark` or `dark`; CSS `light-dark()` tokens follow the active scheme, including live system changes. A synchronous script at the beginning of `<body>`, before `<app-root>`, reads the stored preference before Angular bootstraps. Unknown saved values fall back to system mode.

Matching `theme-color` hints suggest white or dark zinc for supporting browser toolbars. System mode uses media queries; explicit Light/Dark activates only the selected hint. Both the early startup script and runtime preference changes keep the hints synchronized. Browsers decide which surrounding UI, if any, uses the suggested color.
