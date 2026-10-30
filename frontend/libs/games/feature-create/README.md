# games-feature-create

Admin-only game creation (`type:feature`, `scope:games`, `platform:admin`). Import `GAME_CREATE_ROUTES` from `@game-atlas/games/feature-create`; admin mounts them at `/games/new`.

`FeatureCreate` owns its Signal Forms state, POST mutation and unsaved-change guard. It composes the reusable fields and schema from [games-ui](../ui/README.md) and formats API errors through [games-data-access](../data-access/README.md). Creation seeds the persisted detail cache and navigates to the new game's General page.

New games are drafts with a name, explicit slug and image selected through the shared media picker. Unsupported preview fields are never sent to the API. The feature imports no other feature library.

Verify with Nx lint and the admin production build. Frontend unit/E2E work remains deferred.
