## ADDED Requirements

### Requirement: Seed detail data without refreshing its age

A game detail read SHALL use a matching cached list item as initial data when available, preserving that list's data update time. With multiple matching lists it SHALL select the most recently updated source. Existing detail data SHALL NOT be replaced by list initialization.

#### Scenario: Seed a fresh list item

- **WHEN** a detail is opened with a matching cached list item and no existing detail
- **THEN** the item appears immediately with the source list's update timestamp used for freshness

#### Scenario: Stale or unavailable list

- **WHEN** no matching item exists or its source list is stale
- **THEN** retrieval by ID runs as required by freshness rather than treating the item as newly fetched
- **AND** request cancellation cancels transport and failures remain errors

### Requirement: Reconcile optimistic game editing

Known game detail data SHALL reflect submitted editable metadata optimistically, roll back on failure, and be replaced by the authoritative mutation response on success. Missing baselines SHALL NOT produce fabricated games. Reconciliation SHALL prevent older reads or failed writes from overwriting newer successful changes.

#### Scenario: Failed edit

- **WHEN** a metadata or status update fails after an optimistic change
- **THEN** its optimistic changes are rolled back without discarding another successful update

#### Scenario: Replace a cover

- **WHEN** a submitted image ID resolves to a known complete library image
- **THEN** the detail can preview that image optimistically
- **AND** unknown image metadata is not fabricated and the server response resolves the final image

### Requirement: Populate created game details

Successful creation SHALL populate the detail cache using the returned server identity and complete representation before navigation. Failed creation SHALL NOT leave a fabricated persisted detail.

#### Scenario: Create and navigate

- **WHEN** a game is successfully created
- **THEN** its detail is available immediately under the returned ID without requiring an immediate duplicate read

### Requirement: Remove deleted game details after persistence

Game deletion SHALL retain cached game data until success. Success SHALL cancel obsolete reads and remove the deleted detail, preventing stale lists from reseeding it before reconciliation. Failure SHALL preserve cached details.

#### Scenario: Failed deletion

- **WHEN** the backend rejects deletion
- **THEN** the previously cached detail remains available

#### Scenario: Successful deletion

- **WHEN** deletion succeeds
- **THEN** its detail is removed and stale cached lists cannot resurrect it

### Requirement: Reconcile parameterized game lists

Mutations SHALL address all affected parameterized list entries using consistent normalized keys. Known safe updates SHALL use authoritative responses; uncertain filtered, ordered or paginated membership SHALL trigger scoped list invalidation rather than corrupt items, totals or ordering.

#### Scenario: Mutate with several cached pages

- **WHEN** a game changes while several searches or pages are cached
- **THEN** affected entries are reconciled or invalidated without relying on an undefined-parameter key
- **AND** unrelated domain queries are not invalidated
