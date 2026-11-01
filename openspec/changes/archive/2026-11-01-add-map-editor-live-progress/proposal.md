# Proposal

## Why

The map editor only shows processing snapshots, so operators cannot see when tiles become available or recover failed styles from their cards. The existing map hub and retry endpoint already support live progress and recovery without polling.

## What Changes

- Add reusable `provideSignalR` defaults and an Observable transport wrapper, with `provideHub` configuring an independently managed concrete hub.
- Add a Maps subscription service that joins the editor map, reconciles notifications into existing TanStack detail/list caches, rejoins and resynchronizes after reconnect, and releases membership on exit or map change.
- Show queued, processing percentage, ready and failed states in style cards; distinguish stale connection state from processing failure.
- Offer retry for failed styles through the existing endpoint, preventing duplicate submissions and obsolete progress from overriding a new attempt.
- Replace the editor's snapshot-only requirements while retaining snapshot-only behavior for the standalone map list.
- Update owning frontend documentation when implementation lands.

Out of scope: backend protocol changes, polling, live subscriptions on the map list or public web, category/marker collaboration, authentication, source replacement, and new frontend unit/E2E work.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `admin-maps`: Live editor membership, safe cache reconciliation, progress/status cards and failed-style retry; retain list snapshots.
- `frontend-api-clients`: Configurable Observable SignalR transport and independently scoped concrete hubs.

## Impact

- `frontend/libs/shared/data-access`: separate configuration/provider/Observable hub files under signalr using installed `@microsoft/signalr`.
- `frontend/libs/maps/data-access`: handwritten hub contract, subscription orchestration and mutation/cache reconciliation alongside MapsQueryOptions.
- `frontend/libs/maps/feature-editor`: editor lifecycle and failure-only connection feedback; MapStyles child owns cards, dialogs, ordering and retry.
- `frontend/apps/admin`: composition and `/api/hubs/map` configuration; existing proxy supports WebSockets.
- Existing REST generation, server hub methods/events and backend persistence remain unchanged. No additional state store or cross-domain cache access is introduced.
