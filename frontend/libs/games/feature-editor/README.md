# games-feature-editor

Admin-only game settings (`type:feature`, `scope:games`, `platform:admin`). Import `GAME_EDITOR_ROUTES` from `@game-atlas/games/feature-editor`; admin mounts them at `/games/:id`.

General is the default child route. Maps, SEO and Danger zone are separate lazy-loaded child components. Spartan Line tabs follow router navigation and preserve unsaved form values through `routerOutletData`. The page owns detail queries, optimistic PATCH mutations, confirmed deletion and the unsaved-change guard.

The page composes `GameFields` and its form schema from [games-ui](../ui/README.md). HTTP errors are formatted by [games-data-access](../data-access/README.md). It does not import other feature libraries. Initial loading uses the form's detailed skeleton mode; failed background refreshes preserve data and edits and show a Spartan Alert with Retry.

Only name, slug, cover and publication status are persisted. Description, banner, release date, developer, maps and SEO remain frontend previews. Media selection uses the shared picker, provided through the application's Media adapter.

Verify with Nx lint and the admin production build. Frontend unit/E2E work remains deferred.
