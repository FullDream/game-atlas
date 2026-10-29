# Proposal

## Why

Map processing currently polls SQLite for queued work and cleanup every second, persists intermediate percentages, and rereads complete map representations every second for each SSE subscriber. Replace this recurring database traffic with an in-process queue, transient progress, and on-demand SignalR notifications while preserving durable lifecycle states and file cleanup.

## What Changes

- Dispatch committed style creation and manual retry directly to an in-memory queue; continue processing one style at a time independently of viewers.
- Keep percentages in .NET memory. Persist queued, processing, ready, and failed transitions plus safe error information, without percentage-driven saves or timestamp changes.
- Restore queued styles once at startup. **BREAKING**: interrupted processing becomes failed and its derived output is removed instead of automatically requeuing; retry remains explicit.
- **BREAKING**: replace the map SSE endpoint with a SignalR hub using ordinary events sent directly to map groups and compact per-style deltas, with typed style updates and no event-history replay.
- Retain Maps HTTP query/mutation options, the Microsoft SignalR dependency, and WebSocket proxy forwarding. Frontend processing-client implementation, subscription lifecycle, and live processing-cache integration are deferred by the user.
- Retain durable cleanup intents, wake cleanup after commits, and retry known failures at their due time without scanning the database periodically.
- Out of scope: a map viewer/editor screen, changes to tile geometry or encoding, source-file deletion, multiple API writers, external brokers, general application-wide notifications, category/marker collaboration, frontend unit/E2E test work, and automatic rediscovery of other users' jobs while disconnected.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `maps`: event-driven processing and cleanup, transient progress, interruption-to-failure recovery, SignalR-managed map group memberships without a custom connection registry, HTTP cache ownership, and revised API documentation.

## Impact

- Backend: MapStyle processing state, mapping/query handlers, AddMapStyle and RetryMapStyle handlers, map/style/game deletion, MapProcessor, MapProcessingService, persistence configuration and an additive EF migration, and API composition/contracts.
- Transport: remove GET /games/{gameId}/maps/{mapId}/events and its OpenAPI operation; add a scoped SignalR hub and explicit event documentation. Existing REST metadata/retry and static WebP delivery remain available.
- Frontend: add the Microsoft SignalR client dependency and a shared Maps data-access integration using the repository's handwritten query-options conventions; enable WebSocket proxy forwarding where needed. No existing frontend SSE consumer was found.
- Verification: revise backend processing, recovery, fault, SQL-count, lifecycle, acceptance, and SSE contract tests; add focused SignalR coverage. Frontend verification uses formatting, lint, builds, under the existing test deferral; the earlier client runtime exercise is historical evidence only.
- Operations: coordinate the transport/schema rollout, run EF migrations explicitly, retain one API writer per database/storage pair, and document recovery and proxy requirements.
