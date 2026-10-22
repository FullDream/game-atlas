# game-catalog Specification

## Purpose

Maintain the game catalog by creating identifiable draft games with validated metadata and stable, unique client-supplied slugs.

## Requirements

### Requirement: Create a draft game
The system SHALL accept unauthenticated POST requests to `/games` with `name`, `slug`, `image`, and optional nullable `mapId`. Successful creation SHALL persist one game and return HTTP 200 with `id`, `name`, `slug`, `image`, `mapId`, `createdAt`, `updatedAt`, and string `status`. The server SHALL assign Draft status, serialized as `draft`, regardless of any additional status input.

#### Scenario: Create without a map
- **WHEN** a client submits valid name, slug, and image without authentication or mapId
- **THEN** the response is HTTP 200 with the stored game, a null mapId, and status `draft`
- **AND** exactly one game is persisted

#### Scenario: Create with a map identifier
- **WHEN** a client submits a valid request with a mapId GUID
- **THEN** the response and persisted game contain that mapId without checking whether a map exists

#### Scenario: Explicitly null map
- **WHEN** a client submits a valid request with mapId set to null
- **THEN** creation succeeds and mapId is null in storage and the response

#### Scenario: Client cannot choose publication status
- **WHEN** an otherwise valid request includes an additional status property with value `published`
- **THEN** the created game's status is still `draft`

### Requirement: Server-generated identity and timestamps
The system SHALL generate each game's ID as UUID v7 and set createdAt and updatedAt to the same UTC instant during creation. Client input SHALL NOT control those fields.

#### Scenario: Generated creation metadata
- **WHEN** a game is created successfully
- **THEN** its ID is a server-generated UUID v7
- **AND** createdAt and updatedAt represent the same UTC instant and are returned in the response

### Requirement: Normalize and validate game metadata
The system SHALL trim leading and trailing whitespace from name, slug, and image before validation and persistence. Each SHALL be a nonempty string after trimming. Name and slug SHALL have at most 200 characters; image SHALL have at most 2048 characters. Image SHALL NOT require URL syntax. Invalid metadata SHALL return HTTP 422 validation ProblemDetails with field errors and SHALL NOT create a game.

#### Scenario: Trim accepted input
- **WHEN** valid strings include surrounding whitespace
- **THEN** their trimmed values are stored and returned

#### Scenario: Accept an image string without URL syntax
- **WHEN** image is `cover-image` and all other fields are valid
- **THEN** creation succeeds without URL validation

#### Scenario: Reject absent or empty required strings
- **WHEN** name, slug, or image is missing, null, empty, or whitespace-only
- **THEN** the response is HTTP 422 validation ProblemDetails identifying invalid fields
- **AND** no game is created

#### Scenario: Enforce length limits after trimming
- **WHEN** trimmed name or slug exceeds 200 characters, or trimmed image exceeds 2048 characters
- **THEN** the response is HTTP 422 validation ProblemDetails and no game is created

#### Scenario: Accept exact length limits
- **WHEN** name has 200 characters, a syntactically valid unique slug has 200 characters, and image has 2048 characters
- **THEN** creation succeeds

### Requirement: Validate client-supplied slug syntax
The system SHALL accept only slugs made of lowercase ASCII letters or digits separated by single hyphens, matching `^[a-z0-9]+(-[a-z0-9]+)*$` after trimming. It SHALL NOT generate slugs or silently lowercase or repair invalid slugs.

#### Scenario: Accept a valid slug
- **WHEN** a valid request supplies the unique slug `elden-ring-2`
- **THEN** the slug is persisted unchanged after trimming

#### Scenario: Reject invalid slug syntax
- **WHEN** a request supplies a slug containing uppercase letters, spaces, non-ASCII letters, repeated hyphens, or a leading or trailing hyphen
- **THEN** the response is HTTP 422 validation ProblemDetails for slug and no game is created

### Requirement: Enforce slug uniqueness
The system SHALL permit at most one game per normalized slug. A duplicate slug SHALL return HTTP 409 ProblemDetails without creating another game, including when requests race during persistence.

#### Scenario: Existing slug
- **WHEN** a client submits an otherwise valid request with a slug already stored by another game
- **THEN** the response is HTTP 409 ProblemDetails identifying the slug conflict
- **AND** the existing game remains unchanged

#### Scenario: Concurrent creation
- **WHEN** simultaneous otherwise valid requests attempt to create games with the same normalized slug
- **THEN** at most one succeeds and any request rejected for duplicate slug receives HTTP 409 ProblemDetails
- **AND** exactly one game with that slug exists after a successful creation

### Requirement: Safe API error responses
The system SHALL return ProblemDetails for request failures. Malformed JSON or an invalid mapId representation SHALL return HTTP 400. Unexpected database or library failures SHALL return HTTP 500 without exposing exception details, SQL, or connection information. Unrelated database failures SHALL NOT be reported as slug conflicts.

#### Scenario: Malformed request
- **WHEN** the request contains malformed JSON or a mapId that cannot be parsed as a GUID
- **THEN** the response is HTTP 400 ProblemDetails and no game is created

#### Scenario: Unexpected persistence failure
- **WHEN** persistence fails for a reason other than the recognized duplicate-slug constraint
- **THEN** the response is HTTP 500 ProblemDetails without internal exception details

### Requirement: Durable game metadata
Created games SHALL survive application restarts, preserving all returned fields. Status SHALL be stored as a string rather than an enum number.

#### Scenario: Reload persisted game
- **WHEN** a successfully created game is loaded from storage after reopening the database
- **THEN** its ID, normalized metadata, nullable mapId, timestamps, and Draft status match the creation response
- **AND** the stored status is a string

### Requirement: Browse games with complete metadata
The system SHALL expose unauthenticated GET `/games`, including games in Draft and Published status. Each item SHALL expose the same fields and representations as the creation response: id, name, slug, image, nullable mapId, createdAt, updatedAt, and string status.

#### Scenario: Return drafts and published games
- **WHEN** the catalog contains Draft and Published games and a client requests the list without authentication
- **THEN** both statuses are eligible for the response
- **AND** every returned item contains all eight game fields using the creation response representations

### Requirement: Offset and limit pagination
The list SHALL accept integer query parameters offset and limit, defaulting to 0 and 20 when omitted. Valid offset SHALL be at least 0 and valid limit SHALL be from 1 through 100. HTTP 200 responses SHALL contain items, limit, offset, and total; total SHALL count all matches before pagination and items SHALL contain at most limit entries after skipping offset matches.

#### Scenario: Use default pagination
- **WHEN** a client omits offset and limit
- **THEN** the response contains offset 0, limit 20, and at most 20 items
- **AND** total counts all matching games, including those outside this page

#### Scenario: Request a page
- **WHEN** a client requests offset 2 and limit 3 with an explicit sort and seven matching games
- **THEN** the response contains the third through fifth games in that order, offset 2, limit 3, and total 7

#### Scenario: Empty catalog or no search matches
- **WHEN** no games match the request
- **THEN** the response is HTTP 200 with items [], total 0, and the effective offset and limit

#### Scenario: Offset beyond the result set
- **WHEN** offset is greater than or equal to the number of matching games
- **THEN** the response is HTTP 200 with items [] and total equal to the number of matches

#### Scenario: Accept pagination boundaries
- **WHEN** offset is 0 and limit is 1 or 100
- **THEN** the pagination values are accepted

### Requirement: Search game names by literal substring
The list SHALL accept optional search, trim its surrounding whitespace, and treat an absent, empty, or whitespace-only value as no search filter. A nonempty value SHALL match a literal substring of name without regard to ASCII letter case. Cyrillic input SHALL remain permitted, but Unicode case-insensitive matching is not guaranteed in this version.

#### Scenario: ASCII case-insensitive substring
- **WHEN** a stored game is named `Elden Ring` and search is ` RING `
- **THEN** the game matches the filter
- **AND** total counts only games matching the trimmed search

#### Scenario: Empty search
- **WHEN** search is omitted, empty, or contains only whitespace
- **THEN** the response has no name filter

#### Scenario: Literal pattern characters
- **WHEN** search contains `%` or `_`
- **THEN** those characters are matched literally rather than as search wildcards

#### Scenario: Accept Cyrillic input
- **WHEN** search contains Cyrillic characters
- **THEN** it is not rejected merely because of its alphabet
- **AND** matching across different Cyrillic letter cases is not promised

### Requirement: Optional game list sorting
The list SHALL accept sortBy values name, createdAt, or updatedAt and sortDirection values asc or desc, ignoring their letter case. With sortBy and no sortDirection, direction SHALL default to asc. Explicit sorting SHALL precede pagination. When sortBy is omitted, no explicit ordering SHALL be applied and result order SHALL be unspecified.

#### Scenario: Sort names ascending by default
- **WHEN** sortBy is name and sortDirection is omitted
- **THEN** matching games are ordered by name ascending before selecting the page

#### Scenario: Sort every supported field in both directions
- **WHEN** sortBy is name, createdAt, or updatedAt and sortDirection is asc or desc
- **THEN** matching games are ordered by the chosen field in that direction before pagination
- **AND** timestamp sorting compares the full stored UTC instants

#### Scenario: Case-insensitive sorting options
- **WHEN** a client sends sortBy=CreatedAt and sortDirection=DESC
- **THEN** the response uses descending creation-time ordering

#### Scenario: No explicit sorting
- **WHEN** both sorting parameters are omitted
- **THEN** pagination is applied without an explicit default sort
- **AND** the API does not promise insertion order or stable ordering across requests

### Requirement: Validate list query parameters
Negative offset, limit outside 1 through 100, unsupported sortBy or sortDirection, and sortDirection supplied without sortBy SHALL produce HTTP 422 validation ProblemDetails with field errors. Query values that cannot bind to integers SHALL produce HTTP 400 ProblemDetails. Invalid requests SHALL NOT return a successful page.

#### Scenario: Invalid pagination ranges
- **WHEN** offset is negative, limit is zero or negative, or limit exceeds 100
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid fields

#### Scenario: Unknown sorting options
- **WHEN** sortBy is `slug`, or sortDirection is `sideways`
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid sorting fields

#### Scenario: Direction without a field
- **WHEN** sortDirection is supplied without sortBy
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the missing sorting field

#### Scenario: Empty sorting options
- **WHEN** sortBy or sortDirection is explicitly supplied as an empty or whitespace-only value
- **THEN** the response is HTTP 422 validation ProblemDetails for the supplied invalid option

#### Scenario: Non-integer pagination input
- **WHEN** offset or limit contains a non-integer or a value outside the supported integer range
- **THEN** the response is HTTP 400 ProblemDetails
