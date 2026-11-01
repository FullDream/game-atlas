# Spec Delta

## ADDED Requirements

### Requirement: Configure reusable observable hub clients

Frontend consumers SHALL be able to configure shared real-time transport defaults and independently addressed hub clients. Hub clients SHALL expose connection state, server method calls and server events as Observables, preserve invocation failures, and share one connection across event subscriptions for the same client. Shared transport SHALL remain independent of product domains and UI.

#### Scenario: Configure independent hubs

- **WHEN** an application provides two concrete hub clients with different addresses
- **THEN** each uses its configured endpoint and has independent connection state and lifecycle
- **AND** changing deployment addresses requires no generated REST source changes

#### Scenario: Observe events and failures

- **WHEN** a consumer subscribes to several server events and invokes a hub method
- **THEN** events use the same client connection and invocation success or failure is observable
- **AND** each unsubscribed event handler is removed without interrupting other active consumers

### Requirement: Release and recover hub transport resources

Hub clients SHALL expose initial connection, reconnecting, connected and closed/failure states and support recovery after initial start failure or exhausted reconnect. Releasing the final owner SHALL stop the connection and prevent delayed connection work from reviving it. Domain membership restoration SHALL remain the domain consumer's responsibility.

#### Scenario: Leave during connection startup

- **WHEN** the final consumer releases a client while its connection is starting or reconnecting
- **THEN** pending completion cannot leave an orphan connection or deliver events to that consumer

#### Scenario: Recover an unavailable hub

- **WHEN** connection establishment fails or automatic reconnect is exhausted
- **THEN** the failure remains observable and an explicit recovery can establish a new connection
- **AND** transport recovery does not silently claim domain group membership has been restored
