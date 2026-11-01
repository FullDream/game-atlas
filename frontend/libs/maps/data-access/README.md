# Maps data access

Shared Maps HTTP transport and handwritten query/mutation options. Frontend processing subscriptions are deferred; the SignalR package and WebSocket proxy configuration remain available for the later client implementation. The admin screen lives independently in maps/feature-list; no streamedQuery integration is included.

## Configure and consume

Provide `provideMapsBaseUrl('/api')` alongside HttpClient and TanStack Query in the consuming application. For a separate API host, provide its absolute URL. Generation normalizes the embedded fallback to `/api`; the OpenAPI input server's local port does not configure deployment.

`MapsQueryOptions` exposes `getList`, `getDetail`, `create`, `update(gameId, mapId)`, `delete(gameId, mapId)`, `addStyle(gameId, mapId)`, `updateStyle(gameId, mapId)`, `deleteStyle(gameId, mapId)` and `retryStyle`. Use `injectQuery` for reads and `injectMutation` for writes. Preserve cache callbacks when adding feature-owned notifications. Query keys are exported as `mapQueryKeys`.

Style metadata updates are optimistic when a baseline exists, with rollback on failure. Success replaces the style without reordering. Deletion waits for server success, removes the style from known detail/list snapshots and derives the earliest remaining ready default. Per-map scopes serialize style add/update/delete. Detail queries disable automatic focus/reconnect refresh and retries; editor entry deliberately reads metadata. Style actions do not request processing progress.

Creation and style-attachment responses update known map caches directly. Retry replaces the style in cached map details and invalidates affected lists. HTTP reads expose the status/progress available at request time; no live progress subscription or separate processing cache is installed.

## Verification and generation

From `frontend/`:

```sh
npm exec nx -- run maps-data-access:generate-api
npm exec nx -- run maps-data-access:typecheck
npm exec nx -- run maps-data-access:lint
npm exec nx -- build admin --configuration=production
```

Generation is explicit and uncached; `GAME_ATLAS_OPENAPI_URL` overrides its input. Builds do not fetch OpenAPI. Only the Maps tag is generated. The server's SignalR Hub contract remains documented in the [API Maps README](../../../../src/GameAtlas.Api/Maps/README.md), separately from REST generation. No frontend unit/E2E suite is added or run under the current verification policy.

The admin development proxy retains `/api` prefix removal and `ws:true`. A future frontend client can use `/api/hubs/map`; production forwarding must be configured independently.

Map rename uses optimistic known cached names with rollback and then the complete PATCH response. Delete retains rows until server success, then removes detail/list data. Both reconcile without a duplicate list GET. Admin invalidates only the exact game-detail query after successful map create/update/delete through its mutation-cache callback. Maps never imports Games; the game list cache is left unchanged. The admin list disables mount/focus/reconnect refetches and polling; HTTP data remains a snapshot until an explicit read.
