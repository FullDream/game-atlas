# Tasks

## 1. Observable transport

- [x] 1.1 Add and export provideSignalR defaults and a domain-independent Observable wrapper in shared/data-access; verify lazy invocation, observable errors and typed connection/event surfaces compile with the installed SignalR version.
- [x] 1.2 Add provideHub with explicit typed tokens, independent URLs and connection instances; verify multiple event subscriptions reuse one connection and unsubscribing removes only their handlers.
- [x] 1.3 Implement start/reconnect/stop ownership, initial failure recovery and late-start disposal; manually verify release during startup leaves no active connection, and recovery after unavailable server works.
- [x] 1.4 Document provider composition, Observable teardown and domain membership responsibility in shared/data-access README; format affected files and run the resolved shared-data-access lint target through Nx.

## 2. Maps membership and reconciliation

- [x] 2.1 Define the handwritten map hub contract outside generated sources and configure `/api/hubs/map` at admin composition; verify it matches JoinMap, LeaveMap, StyleUpdated and nullable ProcessingUpdated fields in the server contract, without editing generated REST models.
- [x] 2.2 Add a component-owned Maps live session service supporting changing gameId/mapId, handler registration before JoinMap, cleanup and generation guards; manually verify entry joins once, switching releases the prior map, and leaving stops the connection.
- [x] 2.3 Integrate join-before-query synchronization with existing detail query options, cancellation of obsolete snapshot reads and direct event cache updates, reconnect rejoin and HTTP fallback on startup failure; verify there is no competing unconditional editor read and no polling in the network log.
- [x] 2.4 Reuse colocated cache acceptance helpers for HTTP, live events and Maps mutations; verify detail and existing list entries update together, ticks make no HTTP requests, and new styles/lifecycle completion reconcile authoritative geometry, order and default selection.
- [x] 2.5 Add attempt tracking, conservative handling of unknown/conflicting attempts, retired-attempt guards and deletion tombstones; manually verify obsolete ticks cannot restore a failed prior attempt, and delayed reads cannot resurrect deleted styles or maps.
- [x] 2.6 Preserve socket progress during optimistic rollback and successful field updates; verify rename, default change and ordering during processing retain accepted progress and newer successful values.
- [x] 2.7 Align retryStyle with per-map mutation scope and shared acceptance paths, including HTTP 409 reconciliation; verify pending retry blocks conflicting operations and successful retry updates detail/list without stale failure restoration.
- [x] 2.8 Update Maps data-access README and frontend data-access guidance for membership, payload limitations and reconciliation; format files and run the resolved maps-data-access lint target through Nx.

## 3. Editor cards and lifecycle

- [x] 3.1 Bind the live session to editor component teardown and reactive map inputs, replacing independent refetch-on-mount orchestration; verify navigation, direct load and rapid map switches use only the current session.
- [x] 3.2 Replace snapshot-only card copy with queued waiting, accessible processing bar/percentage, ready and safe failed states; manually verify only ready styles are selectable and default/selected style plus viewport camera remain stable during progress and completion.
- [x] 3.3 Add keyboard-accessible failed-card retry using injectMutation outside the disabled selection button; verify pending state, duplicate prevention, success reset, error feedback and conflict recovery.
- [x] 3.4 Add editor-level synchronizing/live/reconnecting/stale feedback and an explicit recovery action; verify failed hub startup still allows HTTP-backed editing and transport loss never changes style processing status to failed.
- [x] 3.5 Update frontend architecture guidance and affected editor/admin READMEs to distinguish live editor behavior from standalone list snapshots; format files and run resolved maps-feature-editor and admin lint targets through Nx.

## 4. Integration verification

- [x] 4.1 Run affected Nx lint targets and `npm exec nx -- build admin --configuration=production`, plus web production compilation if shared provider changes affect its build; record results without adding or running deferred frontend unit/E2E suites.
- [x] 4.2 Manually exercise queued-to-ready, processing-to-failed, retry, server outage/reconnect, exit during startup, remote style/map deletion, overlapping metadata/order mutations and snapshot-only standalone map listing; record observed behavior and any unavailable runtime checks.
- [x] 4.3 Run formatting checks, `node scripts/check-docs.mjs` from repository root, `git diff --check` and strict OpenSpec validation; verify documentation matches implemented behavior and record remaining limitations.

## Verification notes

- Maps data-access typecheck passed. Affected lint targets: shared-util, maps-data-access, maps-feature-editor and admin. Admin production compilation passed without Angular diagnostics; web production build reused its valid Nx cache.
- Browser inspection used installed Edge and the existing local API/admin servers. Real connection traffic confirmed JoinMap before the map GET and socket closure on editor exit, with no JavaScript errors.
- Browser-local HTTP responses and WebSocket events exercised processing percentages, nondecreasing ticks with zero subsequent tick-triggered GETs, processing failure, retry reset, obsolete attempt suppression, pending retry locking, HTTP 409 feedback, style deletion and map deletion. Fixtures did not write existing backend maps.
- Forced transport closure produced a second JoinMap and restored live state. Blocking negotiation preserved HTTP-backed editor data; unblocking and explicit recovery restored membership. Delayed negotiation plus editor exit produced no JavaScript error.
- Delayed failed rename/order writes preserved socket progress (55% and 66%) while rolling back the changed fields/order. A successful default change retained 72% progress. A deleted style stayed absent after a delayed stale snapshot returned.
- These focused browser experiments do not establish real worker throughput, exhaustive cross-client ordering, or complete historical attempt identity. The backend protocol has no attemptId on full styles and no replay/versioned snapshots; conflicting identities use conservative reconciliation. No frontend unit/E2E suites were added or run.

## Refactor verification

- Removed the healthy connection message; failure/reconnecting feedback remains in the editor.
- Generated shared-data-access through Nx and split SignalR configuration, wrapper and provider into its signalr folder. provideHub still owns DestroyRef teardown.
- Removed realtime sessions and event acceptance from MapsQueryOptions. MapsLive updates QueryClient independently; map-cache contains pure resource transforms. Replaced event journals/revisions with cancellation and scoped lifecycle snapshots.
- Extracted smart MapStyles with card actions, dialogs, retry and ordering; the editor retains its aside, viewport and selection. Selecting the already selected style closes the mobile panel.
- Browser checks after extraction verified parent/child selection, dialog opening, percentages without steady-tick GETs, retry and retired attempts, reconnect, style/map deletion and rename rollback preserving 62% progress. Event scenarios used browser-local fixtures without backend writes.
- Affected five project lint targets, Maps typecheck and admin production build passed. Frontend unit/E2E suites remain deferred.
