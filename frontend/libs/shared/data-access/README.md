# Shared data access

Non-buildable Angular transport library tagged type:data-access, scope:shared and platform:shared. Import through @game-atlas/shared/data-access. Nx generates its source alias; this integrated workspace does not create separate npm workspace packages.

SignalR lives in src/lib/signalr:

- provide-signalr.ts defines common connection options and reconnect delays.
- observable-hub.ts wraps one connection with lazy Observable connect/disconnect/invoke/event operations and state$.
- provide-hub.ts creates a token-bound hub and stops it through its own DestroyRef.onDestroy, including startup in flight. Provide it at component scope when a component owns the connection.

Consumers own domain groups and HTTP reconciliation. Event observers share the hub connection and remove their own handlers on unsubscribe. Serialized connection transitions prevent old cleanup from stopping a newly started connection; disconnect can await a bounded leave operation before stopping. Defaults retain negotiation and fallback transports. Initial connection failure or exhausted reconnect is recoverable through connect().

Verify with npm exec nx -- run shared-data-access:lint and the consuming application production build. Frontend unit/E2E work remains deferred.
