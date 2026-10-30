## Context

See proposal.md for motivation. Admin Games is a foundation route; Media already owns a folder-aware image picker. The user has prepared GamesQueryOptions with list/create/update/delete operations. Lists use normalized parameter keys, while the current key helper appends undefined when called without parameters, so current invalidation and optimistic deletion miss parameterized entries. There is no single-game read. Existing specs explicitly keep Games a foundation destination; the admin-shell delta replaces that constraint.

## Goals / Non-Goals

Goals: directly addressable editors, reuse of prepared transport/options, correct detail freshness and cache reconciliation across list variants.
Non-goals: client-generated persistent IDs, a second client library, new cache services, embedded map data, or changes to backend persistence.

## Decisions

### Backend detail operation

Add Games/GetGame request, handler and owned not-found error using IApplicationDbContext with a no-tracking query and existing game/image mapping. Add a thin endpoint in GameAtlas.Api/Games/Endpoints.cs returning OutcomeResult<GameDto>. Use the existing GUID binding and safe error integration. Reuse the seven-field DTO and canonical image; do not introduce another schema. A list search by ID would not support reliable direct navigation, so expose a dedicated read instead.

### Routes and feature layout

Generate one non-buildable admin Games feature tagged type:feature, scope:games, platform:admin. Lazy-load its routes at /games, placing new before :id. Keep list, editor and shared editor form in functional folders. Use a responsive cover-card list as the initial presentation assumption; a table is not required by the behavior contract. Store search/sort/pagination in URL query parameters using existing project patterns, reset offset when filters change, and explicitly correct out-of-range pages after deletion. Default to name ascending and 20 items for predictable browsing.

Use one Signal Forms editor for creation and editing. Keep slug explicit and validate its API syntax; automatic slug generation is outside this change. Use shared getDirtyValues for PATCH, await mutateAsync in submission.action and preserve input on failure. Protect unsaved edits using existing confirmation/guard conventions; background refetch must not reset a dirty form. On success, reset to authoritative metadata. Publication uses the same submission pipeline so it does not silently discard or bypass pending metadata edits. Use the existing page-header portal and progress indicator.

A dialog is simpler for four fields but does not provide a durable game URL or a natural future entry point for maps. Dedicated pages are the agreed choice.

### Shared media selection boundary

Use ready-to-use MediaPickerDialog, ConfirmationDialog and MediaThumbnail in shared/ui. The picker owns its queries and mutations via MEDIA_PICKER_SOURCE from shared/util. MediaPickerAdapter implements the port in Media data-access and is wired with useExisting in admin app configuration. Games has no imports from Media. No media-ui library or picker session/view wrappers remain. Domain isolation is enforced in ESLint.

### Detail initialization and freshness

Retain GamesQueryOptions and extend its public API with getById(id). Centralize root, list-prefix, normalized-list and detail keys; a prefix never includes an undefined parameter slot. Search successful cached lists for the ID, choose the greatest dataUpdatedAt, and return both the Game and that same timestamp through initialData and initialDataUpdatedAt callbacks. Use the existing 30-second stale time; stale seeds refetch, fresh seeds remain fresh only for their original lifetime. Respect an existing detail entry, and do not seed from pending/failed mutation artifacts. The query function calls regenerated transport with cancellation support.

### Mutation lifecycle

Keep cache ownership in GamesQueryOptions and UI callbacks in features. Cancel reads that could overwrite optimistic changes. Use per-game TanStack mutation scopes together with editor submission guards. Do not maintain a custom promise queue or query-client Injector accessor. Snapshot the detail internally and optimistically apply supplied name, slug and status. Keep the user-selected cover preview in the editor and resolve the authoritative image from the server response; never inspect the Media cache from Games. Preserve authoritative timestamps until the server responds. Where no detail baseline exists, seed only from a known cached game or skip optimism.

Replace detail data with the complete mutation response on success and restore the owned snapshot on failure. Coordinate same-game update/delete actions so deletion cannot race a pending edit. Update known list items only when membership, ordering, pagination and totals remain correct; invalidate affected list variants when those properties are uncertain. Do not unconditionally invalidate the freshly written detail.

Creation cannot optimistically create a persisted detail without server identity: seed the detail with the returned Game and current success timestamp before feature navigation. Reconcile affected lists.

Follow the current project deletion policy: retain the detail/list item while deletion is pending, cancel obsolete reads before successful cleanup, remove the detail after success, and remove or invalidate stale list sources before any detail can be seeded again. Failed deletion leaves cached data intact. This replaces the prepared delete operation's ineffective optimistic list removal. These semantics distinguish optimistic editing from authoritative creation/deletion.

## Risks / Trade-offs

- A list seed is a snapshot, not an authoritative new read: preserve its age and refetch when stale.
- Refetch can overwrite unsaved form input: initialize/reset on entity changes or explicit successful save/discard, preserving dirty edits.
- Concurrent reads/writes can restore stale values: cancel relevant reads and serialize same-game operations.
- Picker extraction can regress Media: move only necessary code, preserve both modes and verify both consumers.
- Deletion cascades through maps: show consequences in confirmation and navigate only after success.
- Frontend automated behavior coverage is deferred: use formatting, Nx lint/build and focused manual checks; do not claim these prove runtime correctness.

## Migration Plan

No database migration. Add the endpoint first, regenerate the committed Games client explicitly from the running backend, then integrate options, picker and feature. Update owning docs and README descriptions. Deploy the backend before or together with the new frontend. Rollback the frontend independently; the additive read endpoint remains compatible.
