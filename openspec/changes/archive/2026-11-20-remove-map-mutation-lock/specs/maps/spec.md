# Spec Delta

## MODIFIED Requirements

### Requirement: Stream current per-style progress through one map subscription

The system SHALL replace the map SSE route with a typed MapHub using JoinMap and LeaveMap to manage SignalR group membership by mapId. Handlers and the worker SHALL explicitly publish StyleUpdated with full committed styles and ProcessingUpdated with percentage/deletion deltas containing mapId, styleId, attemptId, status, progress, error, and deleted. Queue admission, database transaction coordination, and file cleanup SHALL NOT implicitly publish resource notifications. Lifecycle transitions SHALL be sent through StyleUpdated without a duplicate ProcessingUpdated event. The Hub SHALL NOT retain application-level subscriptions, snapshots or connection registries. Joining SHALL NOT read the database, validate resource existence, or send initial state. No streaming-query client is required.

#### Scenario: Connect during or after processing

- **WHEN** a client joins a map group
- **THEN** it obtains the current map and styles through the existing HTTP read after joining
- **AND** subsequent committed style changes and transient progress are sent directly to the group without database polling
- **AND** HTTP state and buffered events require client reconciliation rather than an atomic Hub snapshot guarantee

#### Scenario: Reconnect without interrupting work

- **WHEN** a viewer disconnects and reconnects while processing continues
- **THEN** it explicitly rejoins and refetches current HTTP state rather than replaying historical events
- **AND** disconnecting or never connecting does not cancel processing
- **AND** stale attempts, deleted resources, and delayed metadata require reconciliation without regressing cached state

#### Scenario: Observe independently

- **WHEN** two clients join the same map
- **THEN** SignalR sends the same change to both group members
- **AND** leaving one map preserves a connection's other memberships
- **AND** notification failures are bounded and cannot turn committed work into a failure
