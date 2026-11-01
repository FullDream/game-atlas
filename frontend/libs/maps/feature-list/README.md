# Maps feature list

Admin-only map management at `/games/:id/maps`, composed by admin inside the Games editor. This feature depends only on Maps data access and shared libraries; it does not import Games. Admin supplies the route game ID to the `id` input.

The workspace header offers Add map. Rows display dimensions, preview/style snapshots and server creation/modification dates. Create and edit use a Spartan dialog; editing changes only name and displays fixed dimensions. Deletion requires confirmation and retains library media. Open editor links to `/games/:gameId/maps/:mapId/edit`.

The list is an HTTP snapshot: no sockets, intervals, mount/focus/reconnect refreshes or processing-status invalidation. Known mutation responses reconcile cached data directly; a failed initial read offers explicit Retry. Admin refreshes the game detail after successful map create/update/delete; the Maps list itself is not refetched and the Games list cache is left unchanged.

Verify with `npm exec nx -- run maps-feature-list:lint` and `npm exec nx -- build admin --configuration=production`. Frontend unit/E2E work is deferred.
