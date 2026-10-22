# Spec Delta

## ADDED Requirements

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
