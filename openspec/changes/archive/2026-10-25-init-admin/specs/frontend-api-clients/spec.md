# Frontend API Clients Spec Delta

## Purpose

Provide frontend consumers with regenerable backend contracts, domain-specific HTTP clients and reusable query options without coupling shared data access to either application.

## ADDED Requirements

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
