# games-feature-editor

Admin-only game settings (`type:feature`, `scope:games`, `platform:admin`). Admin composes the editor shell, its guard and child components at `/games/:id` through the library public API. The Maps child is supplied independently by admin; Games never imports Maps.

General is the default child route. SEO and Danger zone are lazy child components; Maps belongs to maps-feature-list. Spartan Line tabs follow navigation and preserve unsaved General form values through routerOutletData. General owns the native form element with the stable game-editor-form ID; the editor owns its model, query, mutations and leave protection.

Save changes appears only on General. The Maps feature contributes Add map with inherited game breadcrumbs through the shared page-header portal; destroying that contribution restores parent context. The page composes GameFields and its schema from games-ui and HTTP options from games-data-access.

Name, slug, cover, optional banner, release date, developer, plain-text description and publication status are persisted. Formatting controls and SEO remain deferred. Media selection uses the shared picker provided through the application Media adapter. After successful map create/update/delete, admin invalidates the game detail to refresh map summaries and counts while preserving unsaved form values. The Games list cache is not invalidated.

Verify with Nx lint and the admin production build. Frontend unit/E2E work remains deferred.
