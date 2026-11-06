# Maps data access

Shared Maps HTTP transport, handwritten query/mutation options and editor-owned SignalR processing subscriptions. The admin list remains independent in maps/feature-list; no streamedQuery integration is included.

## Configure and consume

Provide `provideMapsBaseUrl('/api')` alongside HttpClient and TanStack Query in the consuming application. For a separate API host, provide its absolute URL. Generation normalizes the embedded fallback to `/api`; the OpenAPI input server's local port does not configure deployment.

`MapsQueryOptions` exposes `getList`, `getDetail`, `create`, `update(gameId, mapId)`, `delete(gameId, mapId)`, `addStyle(gameId, mapId)`, `updateStyle(gameId, mapId)`, `deleteStyle(gameId, mapId)` and `retryStyle`. Use `injectQuery` for reads and `injectMutation` for writes. Preserve cache callbacks when adding feature-owned notifications. Query keys are exported as `mapQueryKeys`.

Style metadata updates are optimistic when a baseline exists, with rollback on failure. Success replaces the style without reordering. Deletion waits for server success, removes the style from known detail/list snapshots and preserves a surviving ready default or derives the first remaining ready style in display order. Per-map scopes serialize style add/update/delete. Detail queries disable automatic focus/reconnect refresh and retries; editor entry deliberately reads metadata. Known mutation results update caches directly; live lifecycle events and ambiguous overlaps trigger scoped HTTP reconciliation.

Creation and style-attachment responses update known map caches directly. `retryStyle(gameId, mapId)` uses the same per-map mutation scope, accepts the reset style into detail/list caches, quarantines prior attempt progress, and reconciles conflicts. HTTP reads and notifications share cache acceptance rules; there is no separate processing resource store.

## Category query options

`CategoriesQueryOptions` exposes `getList(gameId, mapId)` for the complete group/category tree, category `create()`, `update()` and `delete()`, and group `createGroup()`, `updateGroup()` and `deleteGroup()`. Mutation variables include route IDs and generated request fields; update/delete also require `id`. Category mutations retain `groupId` for the current route. Import options, variable types, `getCategoryErrorMessage` and `getGroupsQueryKey` from `@game-atlas/maps/data-access`.

Successful mutations await exact invalidation of that map's tree, refreshing membership, order, icons and marker counts without affecting other maps or style caches. Failed mutations do not change cached data. This stage intentionally uses invalidation only; optimistic updates remain deferred. Features own notifications and prevent conflicting submissions while pending. Placement uses generated `beforeId` inputs rather than numeric order.

## Editor live updates

Admin configures `provideMapHubUrl('/api/hubs/map')` and shared `provideSignalR()` defaults. The editor provides `MapsQueryOptions`, `MapsLive` and `provideMapHub()` at component scope. `MapsLive.watch(gameId, mapId)` returns synchronization state; subscribing registers handlers, connects, joins the map, then fetches its detail through query options. The editor observes that query with automatic fetching disabled, avoiding an independent startup read. Switching inputs or disposing the stream leaves and disconnects; the hub provider independently stops on destruction.

StyleUpdated reconciles complete styles by updatedAt; ProcessingUpdated applies progress without per-tick requests. First-seen or conflicting attempt identities require a scoped snapshot because full style payloads omit attemptId. Retired attempts are ignored. Lifecycle events cancel obsolete reads and trigger a new snapshot, while percentage ticks patch the cache directly. Deletion tombstones prevent late snapshots from restoring removed styles. MapsLive owns all subscription metadata, snapshot merging and direct QueryClient updates; MapsQueryOptions owns only REST queries/mutations and their normal cache reconciliation. Pure map-cache functions merge resource values without socket sessions or callbacks. Notifications do not synchronize map names, ordering or unrelated source metadata changed elsewhere.

## Verification and generation

From `frontend/`:

```sh
npm exec nx -- run maps-data-access:generate-api
npm exec nx -- run maps-data-access:typecheck
npm exec nx -- run maps-data-access:lint
npm exec nx -- build admin --configuration=production
```

Generation is explicit and uncached; `GAME_ATLAS_OPENAPI_URL` overrides its input. Builds do not fetch OpenAPI. Maps, Categories and Markers tags are generated; group and category operations share CategoriesService. The server's SignalR Hub contract remains documented in the [API Maps README](../../../../src/GameAtlas.Api/Maps/README.md), separately from REST generation. No frontend unit/E2E suite is added or run under the current verification policy.

The admin development proxy retains `/api` prefix removal and `ws:true` for `/api/hubs/map`; production forwarding must be configured independently.

Map rename uses optimistic known cached names with rollback and then the complete PATCH response. Delete retains rows until server success, then removes detail/list data. Both reconcile without a duplicate list GET. Admin invalidates only the exact game-detail query after successful map create/update/delete through its mutation-cache callback. Maps never imports Games; the game list cache is left unchanged. The admin list disables mount/focus/reconnect refetches and polling; HTTP data remains a snapshot until an explicit read.

Map creation accepts only name; geometry fields are nullable until the first style attachment. Style responses expose native tileGeometry. Map update also accepts defaultStyleId and the complete styleOrder array, with optimistic presentation changes and rollback. A surviving ready default is preserved when changing or deleting other styles.

## Marker query options

`MarkersQueryOptions` follows the category layout under `src/lib/markers`: `getList(gameId, mapId)`, `create()`, `update(id)` and `delete(id)`, with separate variable types, query keys and error messages. Generated MarkersService is transport only. Reads use the complete map-scoped array; writes upsert authoritative responses in createdAt/id order after cancelling stale reads. Deletion removes only the persisted item. Creation/deletion/category changes invalidate the exact category tree to refresh group/category counts; successful category/group cascades invalidate the marker list.

Mutation retries are disabled. A lost POST response can follow a committed create: refresh and inspect the map before another create, since duplicate names are valid and the API has no idempotency token. Features own draft state, pending locks and notifications; they do not maintain a second persisted marker store.

Create variables may include a client-only requestGroup correlation key; it is omitted from HTTP payloads. The feature uses it to keep repeated-placement success counts stable across mutation-cache expiry.
