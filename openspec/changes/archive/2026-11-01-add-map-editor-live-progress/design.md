# Design

## Context

The existing map hub exposes JoinMap/LeaveMap, full StyleUpdated payloads and ProcessingUpdated deltas. Full styles omit mapId and attemptId; notifications have no replay or atomic snapshot version. One editor-owned connection joins one map at a time. See proposal.md for scope.

## Goals / Non-Goals

Keep the reactive data direction explicit, TanStack as the resource cache, REST query options independent of socket sessions, and editor layout independent of style-list actions. Avoid a global session registry, retained event journal, polling, a second resource store or backend protocol changes.

## Decisions

### Shared transport boundary

Create the non-buildable shared/data-access library with type:data-access, scope:shared and platform:shared. Its signalr folder separates provide-signalr.ts (defaults/configuration), observable-hub.ts (Observable connection and event wrapper) and provide-hub.ts (DI ownership). provideHub registers its own DestroyRef.onDestroy to stop the connection, including startup in flight. Applications configure addressing; domains configure group methods/events. Shared util remains transport-independent.

### REST and realtime ownership

MapsQueryOptions owns generated HTTP transport, query/mutation factories and normal optimistic mutation reconciliation. It does not retain realtime sessions, attempts, patches or event buffers. map-cache.ts contains only pure resource merge/default transformations usable by HTTP and event paths.

MapsLive owns its subscription-local attempt/deletion metadata and direct QueryClient changes. Its returned stream is connect -> connection state -> JoinMap -> snapshot and notifications -> LeaveMap/stop. Each reconnect switches out the previous event/read stream and rejoins. Component teardown or changing map inputs unsubscribes and serializes old disconnect before a new connect, so unscoped StyleUpdated events cannot leak into another map.

### Replace obsolete reads instead of journaling events

Handlers are registered before JoinMap and the first snapshot. Full lifecycle events that require authoritative map geometry/default/order cancel an obsolete snapshot and request a new one after the committed event. Percentage ticks patch existing style values directly; a first-seen or conflicting attempt identity requests a scoped snapshot. Snapshot acceptance merges cached newer timestamps/progress and filters deleted IDs. Query cancellation plus deletion tombstones prevent stale reads from restoring removed data. No event journal, revision counter or cross-service session binding is retained.

Map mutations defer event-triggered reads while pending; the realtime stream observes their completion and reconciles when needed. Pending style metadata patches are derived from mutation variables when applying events, preserving optimistic name/color without copying them into a service registry. REST rollback changes only fields still owned by that optimistic write, preserving progress delivered in the meantime.

### Attempt identity limits

Observe attempt IDs per style and retire known IDs on retry. Conflicting IDs are quarantined and reconciled rather than ordered by arrival. Full HTTP/style payloads cannot prove historical attempt identity, so this remains best-effort recovery rather than exactly-once synchronization. Retry uses the existing per-map mutation scope and endpoint.

### Editor and style-list responsibilities

FeatureEditor owns the left aside, mobile visibility, viewport, live subscription and selected ready style. MapStyles is its smart child with gameId/map inputs and a selectedStyleId model. It owns the list header, cards, dialogs, mutations, retry and CDK ordering; drag CSS belongs to the child. Selection flows back to the editor without moving the camera.

Queued, processing percentage, ready and failed cards remain accessible. A healthy connection shows no extra message. Reconnecting/stale feedback and explicit recovery remain in the editor. Transport failure never marks a processing job failed.

## Risks / Trade-offs

- Unversioned payloads and missing attempt identity: use timestamp/progress merging, retired attempts, cancellation and scoped HTTP reconciliation; complete cross-client ordering remains unavailable.
- Lifecycle/read overlap: restarting an obsolete read can issue another HTTP request, while normal percentage ticks remain request-free.
- Mutation overlap: preserve pending field ownership and verify rollback, default/order changes and retry with browser-local fixtures.
- Component extraction: retain aside layout, parent selection and child drag styling; verify desktop/mobile selection and dialogs.

## Migration Plan

No backend or persistence migration is needed. Generate the internal shared library through Nx, move transport files and imports, separate REST/realtime responsibilities, and extract MapStyles. Validate affected lint, admin production compilation and browser interaction checks; preserve the frontend test deferral. Rollback is a frontend revert and does not cancel server processing.
