# frontend-api-clients Specification

## Purpose

Provide frontend consumers with regenerable backend contracts, domain-specific HTTP clients and reusable query options without coupling shared data access to either application.

## Requirements

### Requirement: Reuse shared game data access

The existing games data-access library and its public import path SHALL be retained and converted to backend API access usable by both admin and web. Its models SHALL follow the backend contract rather than the previous static catalog shape. It SHALL NOT import application code or administration-only UI.

#### Scenario: Consume the library from either platform

- **WHEN** an admin or web consumer imports shared game data access
- **THEN** dependency boundaries permit the import without depending on the other application
- **AND** the same generated game contract is available to both consumers

#### Scenario: Replace prototype contracts

- **WHEN** the conversion is complete
- **THEN** game contracts expose backend name/image/status fields and server-owned identifiers and timestamps
- **AND** the obsolete title/coverUrl/mapsCount model, static catalog parser and UUID-v4-only validation are not part of this library

### Requirement: Generate only the selected domain API

Each API data-access library SHALL expose a `generate-api` command that reads the running backend OpenAPI document over HTTP and selects operations by domain tag. The initial Games generation SHALL use `http://localhost:5041/openapi/v1.json` and the Games tag. Generated outputs SHALL be confined to that library and SHALL NOT contain unrelated resource services.

#### Scenario: Generate shared game clients

- **WHEN** Games generation runs against the available backend document
- **THEN** game operations and their referenced contracts are generated beneath the library's `generated/` directory
- **AND** Media, Maps, Categories, Category groups and Markers services are absent

#### Scenario: Fail generation visibly

- **WHEN** the configured document cannot be fetched or parsed
- **THEN** generation exits unsuccessfully with an actionable diagnostic
- **AND** it does not silently substitute old static catalog data or rewrite handwritten query classes

### Requirement: Separate generated transport from handwritten query options

Generation SHALL produce Angular HTTP service classes with tag-split output. Handwritten injectable classes SHALL expose reusable query options with colocated keys and request functions. Generated code SHALL NOT produce TanStack query hooks or option factories, and feature components SHALL NOT duplicate transport logic.

#### Scenario: Obtain game-list query options

- **WHEN** a consumer requests options for listing games with specified pagination, search or sorting
- **THEN** the options use the generated game-list operation and a key distinguishing the relevant parameters
- **AND** equal parameters identify the same cache entry while changed parameters identify another entry

#### Scenario: Preserve failures and cancellation

- **WHEN** a game-list request fails or a consuming query is cancelled
- **THEN** failure remains a rejected query with available HTTP ProblemDetails rather than successful empty data
- **AND** cancellation ends the underlying request subscription without an unhandled rejection

### Requirement: Generate lightweight faithful contracts

Generated data models SHALL prefer type aliases over interfaces. Enum schemas SHALL default to string union types matching API values. Runtime constant objects SHALL be introduced only for a demonstrated consumer need. Contracts SHALL preserve optional versus nullable values, PATCH field presence and response-only fields.

#### Scenario: Generate status and sorting values

- **WHEN** game status and sorting schemas are generated
- **THEN** their string values match the API and are represented as union types
- **AND** no runtime enum or constant object is emitted merely to represent those unions

#### Scenario: Preserve PATCH meaning

- **WHEN** a consumer constructs an update using generated request types
- **THEN** omitted mutable properties remain distinct from explicitly supplied nullable values
- **AND** server-owned response fields do not become required update inputs

### Requirement: Build from committed generated sources

Generated sources SHALL be version-controlled and reproducible with pinned tooling for an unchanged backend document. Normal frontend builds SHALL use those sources without fetching OpenAPI. Generation SHALL be explicit and SHALL NOT be automatically served from the Nx cache when the remote contract might have changed.

#### Scenario: Build while the backend is offline

- **WHEN** a developer builds administration from a checkout containing generated sources
- **THEN** compilation does not fetch the OpenAPI document or require a running backend

#### Scenario: Regenerate an unchanged contract

- **WHEN** explicit generation runs twice against the same backend document and pinned toolchain
- **THEN** the second run has no meaningful source differences or generated timestamp churn
- **AND** handwritten files remain unchanged

### Requirement: Configure runtime API addressing independently

The running application's API base URL SHALL be configurable independently of the OpenAPI generation URL. Shared clients SHALL NOT hardcode an administration-only origin. Administration development SHALL support requests through a same-origin API prefix distinct from page routes, preserving generated endpoint paths and query parameters.

#### Scenario: Use development API routing

- **WHEN** a shared client lists games from administration development
- **THEN** its request reaches backend `/games` through the configured API prefix
- **AND** opening the administration `/games` page still serves the SPA rather than backend JSON

#### Scenario: Use a different deployment origin

- **WHEN** a consumer provides another API base URL
- **THEN** client requests use that address without regenerating or manually editing generated source files

### Requirement: Consume canonical public schema names

Existing frontend API clients SHALL use public OpenAPI schema names without DTO suffixes or parent-prefixed concrete media variants. Handwritten imports, exports, and type annotations SHALL migrate to those generated names without legacy aliases. Generation SHALL continue to read the running backend document over HTTP.

#### Scenario: Regenerate existing domain clients

- **WHEN** Games and Media clients are regenerated from the updated backend document
- **THEN** they expose canonical resource types and discriminated media unions
- **AND** video previews reference the shared nullable image type
- **AND** handwritten consumers compile with updated imports and names
- **AND** normal frontend builds still use committed generated files without fetching OpenAPI

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
