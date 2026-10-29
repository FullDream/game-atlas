# Spec Delta

## MODIFIED Requirements

### Requirement: Expose processing state directly on each style

Style representations SHALL retain id, name, backgroundColor, source, status, progress, error, tilesUrlTemplate, createdAt, and updatedAt. Status and safe error information SHALL be durable; intermediate progress SHALL be transient. source SHALL resolve the referenced shared image asset. Tile templates SHALL remain null until successful publication. Percentage changes alone SHALL NOT change persisted timestamps.

#### Scenario: Report queued and ready styles

- **WHEN** an accepted style has not completed processing
- **THEN** its status and transient progress describe its own processing attempt and tilesUrlTemplate is null
- **AND** after publication it becomes ready with progress 100 and a style-addressed tile template

#### Scenario: Report processing failure safely

- **WHEN** a style's processing fails
- **THEN** tilesUrlTemplate remains null and status is failed
- **AND** error exposes only a safe code and message, never paths, stack traces, or library diagnostics
- **AND** the last percentage is available only while its transient state is retained; absent transient progress is represented as 0

#### Scenario: Resolve source metadata without retiling

- **WHEN** a source asset is renamed, moved, or given new alt text
- **THEN** subsequent HTTP map/style representations resolve that metadata without changing style timestamps, attempts, pixels, or tile URLs
- **AND** processing notifications do not synchronize source metadata

### Requirement: Deliver immutable style-addressed tiles

The system SHALL serve complete WebP tiles statically at /map-tiles/{mapId}/{styleId}/{z}/{x}/{y}.webp without Mediator or database access. IDs SHALL use UUID N format. GET/HEAD and cache revalidation SHALL be supported. Missing files or malformed paths SHALL return HTTP 404. Retained files MAY remain available after metadata changes or deletion until cleanup; caches MAY retain them longer.

#### Scenario: Locate and cache a published tile

- **WHEN** a valid tile URL is requested for a complete retained style
- **THEN** the correct WebP bytes and a cache validator are returned
- **AND** immutable style URLs address columns increasing right and rows down from tileOrigin
- **AND** revalidating unchanged content can return HTTP 304 without changing processing state or querying the database
- **AND** HEAD returns the same headers without a response body

#### Scenario: Keep original and incomplete files private

- **WHEN** a client requests a legacy original path, staging file, work tile, manifest, directory, or nonexistent tile coordinate
- **THEN** 404 preserves private working paths and only complete pyramids exist under the public tile root
- **AND** committed originals are intentionally available through their separate shared /media/files/{id} endpoint

#### Scenario: Serve complete files independently of metadata

- **WHEN** a validated pyramid has moved into the public tile root but its ready commit has not completed, or its metadata was deleted before cleanup
- **THEN** its existing WebP files may be served without checking database state
- **AND** static delivery does not recheck current ownership, processing state, or database existence
- **AND** metadata advertises its tile template and notifications advertise ready only after a successful ready commit
- **AND** startup recovery or cleanup eventually removes failed or unreferenced derived output without deleting ready output

### Requirement: Process admitted styles independently and sequentially

At most one style SHALL be tiled at a time in the initial deployment. Admitted work SHALL transition from queued to processing and then ready or failed. Committed admission and manual retry SHALL trigger processing without periodic database discovery or a connected viewer. Failure of one style SHALL NOT roll back admitted siblings or prevent their processing. Duplicate dispatch SHALL NOT duplicate a processing attempt.

#### Scenario: Continue after one style fails

- **WHEN** Atlas and Roads are admitted independently to a map and Atlas fails during processing
- **THEN** Atlas becomes failed and Roads can still become ready
- **AND** the original successful upload response and saved originals remain valid

#### Scenario: Avoid concurrent image processing

- **WHEN** several styles are queued for one or more maps and games
- **THEN** only one tiling operation runs at once while other work remains queued

#### Scenario: Dispatch after a successful commit

- **WHEN** style admission or retry commits successfully
- **THEN** its current attempt becomes eligible for processing without a periodic database scan
- **AND** a rolled-back transaction produces no runnable work
- **AND** request cancellation after commit does not discard committed work

### Requirement: Recover interrupted processing after restart

Accepted queued work SHALL survive application restart and be restored once at startup. Styles left processing SHALL become failed with a safe interruption error and durable derived-output cleanup, without automatic retry. Ready styles and shared originals SHALL be preserved. Recovery SHALL finish before normal processing and subscriptions start and SHALL NOT require a viewer or recurring database scans.

#### Scenario: Restart with queued and interrupted work

- **WHEN** the application stops with queued and processing styles and later starts again
- **THEN** queued styles become eligible for sequential processing
- **AND** interrupted styles become failed with progress 0 and require explicit manual retry
- **AND** their work and tile files are removed, with durable retry if filesystem cleanup fails
- **AND** other ready styles and all shared originals are preserved

#### Scenario: Crash after moving a complete pyramid

- **WHEN** the server stops after moving output into public storage but before committing ready
- **THEN** startup treats the persisted processing style as interrupted and removes that output instead of completing or requeuing it

#### Scenario: Crash between admission and dispatch

- **WHEN** a queued style commits but the process stops before dispatching it in memory
- **THEN** startup discovers and dispatches that queued attempt once without requiring a client retry

### Requirement: Stream current per-style progress through one map subscription

The system SHALL replace the map SSE route with a typed MapHub using JoinMap and LeaveMap to manage SignalR group membership by mapId. Handlers and the worker SHALL explicitly publish StyleUpdated with full committed styles and ProcessingUpdated with percentage/deletion deltas containing mapId, styleId, attemptId, status, progress, error, and deleted. Queue admission, SQLite mutation coordination, and file cleanup SHALL NOT implicitly publish resource notifications. Lifecycle transitions SHALL be sent through StyleUpdated without a duplicate ProcessingUpdated event. The Hub SHALL NOT retain application-level subscriptions, snapshots or connection registries. Joining SHALL NOT read the database, validate resource existence, or send initial state. No streaming-query client is required.

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

### Requirement: Report progress without promising unavailable exact work counts

Progress SHALL be transient, approximate, and nondecreasing within an attempt, reset to 0 on manual retry, and remain below 100 until ready commits. Intermediate changes SHALL NOT trigger database writes. Changed percentages SHALL be notified at most about once per second per map; committed lifecycle transitions SHALL bypass percentage throttling. Ready SHALL report 100; other states without retained transient progress SHALL report 0.

#### Scenario: Finish and restart progress accurately

- **WHEN** processing approaches completion but publication is not committed
- **THEN** reported progress remains below 100 until publication succeeds
- **AND** manual retry starts a new attempt at 0 and stale callbacks cannot update it
- **AND** failed progress is not promised to survive process restart

#### Scenario: Keep an idle subscription valid

- **WHEN** a subscribed map has no queued or processing styles
- **THEN** the client derives completion from style statuses while membership remains active until LeaveMap or disconnect
- **AND** no application-level idle heartbeat or recurring database read is retained, and later style changes are delivered to the same group

### Requirement: Delete a map and its complete owned content

Map DELETE SHALL retain empty 204, ownership cascades, sibling isolation, and durable derived-output cleanup. Active processing subscribers SHALL receive ProcessingUpdated with deleted:true and let clients leave the group. Its style source links, category icon links, and marker attachment rows SHALL disappear with their resources, but shared assets, preview links owned by surviving videos, and original bytes SHALL remain. No asset deletion SHALL follow automatically from deleting a map or game.

#### Scenario: Delete one of several maps

- **WHEN** a map containing groups, categories, markers, media, and processing styles is deleted
- **THEN** only that ownership hierarchy disappears and its active processing subscribers receive ProcessingUpdated with deleted:true and let clients leave the groups
- **AND** sibling maps, their content/files/counts/coordinates, and game timestamps remain unchanged

#### Scenario: Delete an empty map or recreate a name

- **WHEN** a map without styles is deleted
- **THEN** deletion succeeds and frees its game-scoped name
- **AND** a later map using that name receives a new identity and does not inherit deleted content or subscriptions

### Requirement: Remove map files durably and prevent stale publication

Map/style/game deletion and processing failure SHALL durably clean only map-owned tiles and work, never shared originals. Cleanup SHALL start from committed operations, retry known failures at their due time, and resume after restart without periodic table scans. Stale processing SHALL NOT restore deleted metadata or advertise output. No source-file leases SHALL be introduced; removing a style SHALL NOT independently block eligible asset deletion.

#### Scenario: Delete during image processing

- **WHEN** a style, map, or game is deleted during tiling
- **THEN** resource metadata disappears after commit and stale output cannot be advertised
- **AND** source assets remain unless explicitly deleted through the library, and only derived files are eventually removed
- **AND** native writing stops before its output is removed

#### Scenario: Retry filesystem cleanup

- **WHEN** derived-output deletion fails and the application restarts
- **THEN** cleanup resumes without deleting shared originals or unrelated outputs
- **AND** source assets remain discoverable even after their final style is gone
- **AND** once known cleanup completes, the idle worker does not repeatedly query for more cleanup

#### Scenario: Retry while prior cleanup remains pending

- **WHEN** a failed style is retried before its previous cleanup succeeds
- **THEN** old cleanup cannot delete output of the new attempt
- **AND** processing unrelated styles can continue

### Requirement: Return safe errors across HTTP and SSE phases

HTTP failures SHALL retain ProblemDetails and safe unexpected HTTP 500 responses. Missing maps SHALL be reported through HTTP reads; group joins SHALL perform no resource lookup. Notification failures SHALL be logged without exposing internal diagnostics or changing committed work. Processing errors SHALL remain durable failed style state. Deletion SHALL emit ProcessingUpdated with deleted:true.

#### Scenario: Fail before and after response start

- **WHEN** a client joins a group for a missing map and requests its HTTP representation
- **THEN** HTTP returns the existing safe not-found response
- **AND** unexpected notification failures do not change a successful commit and current state remains recoverable through HTTP

#### Scenario: Surface a native processing failure safely

- **WHEN** an image decoder or tile write fails after admission
- **THEN** the style becomes failed and its original remains available for retry
- **AND** subscribers receive a safe code and message after failure commits, without exception details or physical paths

### Requirement: Document maps content ownership and rendering metadata

OpenAPI SHALL describe the eight remaining map REST operations, explicit map scope, complete map/style representations, geometry, retry, and static WebP delivery, without the removed SSE operation. Separate documentation SHALL define SignalR methods, events, lifecycle, and proxy requirements. AddMapStyle SHALL retain JSON name/backgroundColor/sourceAssetId. Documentation SHALL explain transient progress, interruption failure, and shared-original preservation.

#### Scenario: Discover independent map and style creation

- **WHEN** a client reads OpenAPI and map transport documentation
- **THEN** CreateMap remains name/width/height JSON and AddMapStyle remains name/backgroundColor/sourceAssetId JSON with no file picker
- **AND** maps remain complete arrays/DTOs while file uploads occur only through the library and legacy routes remain absent
- **AND** the old events route returns 404 and SignalR negotiation is not presented as a generated REST streaming operation
