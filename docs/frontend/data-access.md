# Frontend data access

Applies to generated API configuration, handwritten query options and their feature consumers. Paths are relative to `frontend/`. Read [API conventions](../api-conventions.md) when changing HTTP contracts or patch payloads.

## Transport, generation and cache ownership

- Shared backend API libraries use `platform:shared`. The existing `libs/games/data-access` serves both platforms; do not create a parallel admin-specific Games client.
- Generate Angular HttpClient services with Orval into a library-local `src/lib/generated` directory, using tag filtering and `tags-split`. Do not generate TanStack integration or httpResource wrappers.
- Run generation through the library's uncached `generate-api` Nx target. The current input is `http://localhost:5041/openapi/v1.json`; `GAME_ATLAS_OPENAPI_URL` can override it. Keep generated sources in version control. Ordinary builds must not fetch OpenAPI.
- Never edit generated sources manually. Change generator configuration and regenerate; narrow generated-only lint exceptions must not weaken handwritten code or module boundaries.
- Set Orval's output-level `namingConvention: 'kebab-case'`; regenerate files rather than renaming generated output manually. This setting controls filenames, not API property names or TypeScript type names.
- Name handwritten API option classes `*QueryOptions` and declare query/mutation factories with `CreateQueryOptions` and `CreateMutationOptions`. Consume writes through `injectMutation`; keep Observable-to-Promise conversion inside transport/query or mutation functions. Do not add a separate cache service for TanStack state.
- Consume public schema names such as `Game`, `MediaItem`, `ImageAsset`, and `PaginatedResultOfMediaItem` directly. Media unions share the canonical folder/image/video models, and video previews use nullable `ImageAsset` without a single-variant union. When schema identifiers change, regenerate existing domain clients and migrate imports, exports, annotations, and existing fixtures together; do not retain DTO aliases.
- Use `toPromise` from `@game-atlas/shared/util` to bridge HTTP Observables at TanStack query/mutation boundaries. Pass the query or upload AbortSignal when available; avoid duplicating abort listeners and Promise conversion in options classes.
- Data-access must remain UI-agnostic: do not encode toast, dialog or navigation instructions in mutation meta (for example errorToast), and do not invoke UI services from options. Features own notifications, dialogs, routing and editor reactions through mutation callbacks: onSuccess for successful UI actions and onError for error presentation. Pass per-call callbacks or compose options without overwriting data-access cache reconciliation/rollback.
- Keep query keys and query functions together in handwritten injectable query-options classes. Use generated services as transport and preserve HTTP errors/ProblemDetails. Unsubscribe HTTP requests when a consuming query is cancelled.
- Keep mutation-cache cleanup inside data-access options. Features invoke exposed operation actions and must not obtain MutationCache or remove its entries directly; retry cleanup belongs to the mutation lifecycle.
- Render concurrent pending/error mutation items through TanStack `injectMutationState` filtered by mutationKey. Derive files, destination, status and errors from mutation variables/state; do not mirror them in a separate card/state service. Keep only transport metadata absent from TanStack, such as progress by upload key, in options.
- Update mutation variables contain only the resource ID and patch data, never a previous DTO supplied by the caller. Resolve any optimistic baseline from cache inside data-access and keep rollback snapshots internal. If the baseline is unavailable, skip optimistic changes and invalidate relevant resource queries after success.
- Apply authoritative mutation responses directly to affected query caches when membership, ordering, totals and pagination can be maintained correctly. Do not unconditionally refetch the current listing after every mutation. Use narrowly scoped invalidation when filtered or incomplete results cannot be reconstructed safely.
- Configure QueryClient and runtime API base URLs in consuming applications. The generation URL does not define the deployment URL. Admin development proxies `/api/**` and canonical `/media/files/**` to the backend so page routes such as `/games` remain SPA routes.
- Domain tag ownership: games selects `Games`; maps selects `Maps`, `Category groups`, `Categories` and `Markers`; media selects `Media`. Add clients only when needed.

## Optimistic changes and errors

- Make rename and move mutations optimistic when a known item can be updated safely: data-access cancels affected reads, snapshots affected query data, applies the local change, replaces it with the authoritative server response on success and rolls back on error. The consuming feature presents the error through its mutation callback, including any toast. Preserve concurrent successful writes rather than overwriting them with stale snapshots; narrowly invalidate ambiguous filtered, paginated or concurrent results. Wait for server success before removing deleted items from cache because deletion can be refused by reference constraints. Uploads and creations without server IDs stay outside the optimistic policy unless explicitly designed otherwise.
- Format HTTP errors with a descriptive helper such as `getMediaErrorMessage`; use Angular `HttpStatusCode` names for standard HTTP statuses. Status `0` is a transport failure, not an HTTP enum member.

## Entity identifiers

- Backend entity identifiers and timestamps are authoritative server values. Accept UUID v7 IDs and preserve them unchanged; admin does not create IDs for backend resources.
- Do not derive entity identity from slugs, array indices or incremental counters. Use `crypto.randomUUID()` only for an explicitly browser-owned local identifier, never as a replacement for a server entity ID.

## Current libraries and integration

Keep the Games public alias `@game-atlas/games/data-access`. Its `generate-api` target selects only Games and referenced schemas. Add maps and media clients as needed rather than generating the whole backend into every domain.

Generated sources are formatted by a post-generation hook. Runtime API addressing is independently supplied through generated base-URL DI providers. See [TypeScript style](../styleguide.md#typescript-and-angular-templates) for model typing conventions.

The non-buildable `libs/shared/util` library (`type:util`, `scope:shared`, `platform:shared`) exports `toPromise` for Observable-to-Promise conversion at transport boundaries. Cancellation rejects with signal.reason and unsubscribes HTTP. See [Angular conventions](angular.md) for dialog and guard Observables.

Do not introduce a generic repository, base-query framework, additional state store or overlapping httpResource cache. Apply the cache rules above to Media rename, move and deletion, with feature-owned error toasts. Refused deletions keep the item visible. Template layout and multiline declaration spacing are defined in the [style guide](../styleguide.md#typescript-and-angular-templates).
