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
The list SHALL accept sortBy names name, createdAt, or updatedAt and sortDirection names asc or desc, case-insensitively. Other standard enum forms SHALL be accepted when their resulting values are defined. With sortBy and no direction, direction SHALL default to asc. Explicit sorting SHALL precede pagination. With sortBy omitted, no explicit ordering SHALL be applied and order SHALL be unspecified.

#### Scenario: Sort names ascending by default
- **WHEN** sortBy is name and sortDirection is omitted
- **THEN** matching games are ordered by name ascending before selecting the page

#### Scenario: Sort every supported field in both directions
- **WHEN** sortBy is name, createdAt, or updatedAt and sortDirection is asc or desc
- **THEN** matching games are ordered by the chosen field in that direction before pagination
- **AND** timestamp sorting compares the full stored UTC instants

#### Scenario: Case-insensitive sorting options
- **WHEN** a client sends sortBy=NAME and sortDirection=DESC
- **THEN** the sorting options are accepted as name and desc

#### Scenario: Standard enum representations
- **WHEN** a client uses a standard enum representation that resolves to a defined sorting value
- **THEN** that value is accepted, including surrounding whitespace, defined numeric values, and comma-separated names
- **AND** sortBy numeric values 0, 1, and 2 mean Name, CreatedAt, and UpdatedAt, while sortDirection values 0 and 1 mean Asc and Desc

#### Scenario: No explicit sorting
- **WHEN** both sorting parameters are omitted
- **THEN** pagination is applied without an explicit default sort
- **AND** the API does not promise insertion order or stable ordering across requests

### Requirement: Validate list query parameters
Negative offset, limit outside 1 through 100, undefined sorting enum values, and sortDirection supplied without sortBy SHALL produce HTTP 422 validation ProblemDetails with field errors. Query values that cannot bind to integers or sorting enums, including unknown enum names and empty sorting values, SHALL produce HTTP 400 ProblemDetails. Invalid requests SHALL NOT return a successful page.

#### Scenario: Invalid pagination ranges
- **WHEN** offset is negative, limit is zero or negative, or limit exceeds 100
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid fields

#### Scenario: Unknown sorting options
- **WHEN** sortBy is `slug`, or sortDirection is `sideways`
- **THEN** the response is HTTP 400 ProblemDetails

#### Scenario: Undefined sorting enum values
- **WHEN** sortBy or sortDirection is a parseable integer such as 99 that does not identify a defined enum value
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid sorting fields

#### Scenario: Direction without a field
- **WHEN** sortDirection is supplied without sortBy
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the missing sorting field

#### Scenario: Empty sorting options
- **WHEN** sortBy or sortDirection is explicitly supplied as an empty or whitespace-only value
- **THEN** the response is HTTP 400 ProblemDetails

#### Scenario: Non-integer pagination input
- **WHEN** offset or limit contains a non-integer or a value outside the supported integer range
- **THEN** the response is HTTP 400 ProblemDetails

### Requirement: Partially update an existing game
The system SHALL accept unauthenticated PATCH `/games/{id}` for an existing game and return HTTP 200 with all eight game fields in the same representation as creation. Only name, slug, image, mapId, and status SHALL be mutable. A valid patch targeting a nonexistent game SHALL return HTTP 404 ProblemDetails and SHALL NOT create a game.

#### Scenario: Update every mutable field
- **WHEN** a client patches an existing game with valid name, unique slug, image, mapId, and status
- **THEN** the response is HTTP 200 with the complete updated game
- **AND** id and createdAt remain unchanged and the updates survive reopening the database

#### Scenario: Update a missing game
- **WHEN** a nonempty valid patch targets a GUID for which no game exists
- **THEN** the response is HTTP 404 ProblemDetails and no game is created

### Requirement: Preserve omission and explicit null during game updates
Every mutable update field SHALL be optional. An omitted field SHALL retain its current value. Explicit null SHALL clear mapId, but null for name, slug, image, or status SHALL return HTTP 422 validation ProblemDetails. A supplied mapId SHALL be stored without a map existence check.

#### Scenario: Omitted fields remain unchanged
- **WHEN** a patch supplies only name for a game with a map and Published status
- **THEN** name is updated and slug, image, mapId, and status retain their current values

#### Scenario: Clear or replace a map identifier
- **WHEN** a patch supplies mapId as null or as a valid GUID
- **THEN** the stored and returned mapId is respectively null or that GUID
- **AND** no map existence check is performed

#### Scenario: Reject null required values
- **WHEN** a patch explicitly sets name, slug, image, or status to null
- **THEN** the response is HTTP 422 validation ProblemDetails identifying each invalid field
- **AND** all stored game fields, including updatedAt, remain unchanged

### Requirement: Validate supplied game update metadata
Supplied name, slug, and image SHALL be trimmed and validated using the creation metadata rules: nonempty values, maximum lengths of 200, 200, and 2048 respectively, and lowercase ASCII slug syntax with single separating hyphens. Image SHALL remain a plain string without URL validation. Invalid supplied metadata SHALL return HTTP 422 validation ProblemDetails without applying any changes.

#### Scenario: Trim update values and accept length boundaries
- **WHEN** supplied strings have surrounding whitespace and their trimmed values satisfy syntax and length limits, including exact maximum lengths
- **THEN** the trimmed values are persisted and returned

#### Scenario: Reject invalid metadata atomically
- **WHEN** a patch combines a valid name change with an empty or whitespace-only image, an overlength string, or an invalid slug
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid fields
- **AND** neither the valid change nor any other game field is persisted

#### Scenario: Preserve plain image string semantics
- **WHEN** a patch supplies image as `cover-image`
- **THEN** the value is accepted without URL validation

### Requirement: Publish and unpublish through game updates
The system SHALL permit status changes from draft to published and from published to draft through PATCH. Publication SHALL require no additional conditions beyond valid game metadata. Responses and storage SHALL represent status as strings; the API SHALL expose draft and published rather than numeric enum values.

#### Scenario: Publish a draft
- **WHEN** a client patches a draft with status `published`
- **THEN** the game is persisted and returned with status `published`

#### Scenario: Return to draft
- **WHEN** a client patches a published game with status `draft`
- **THEN** the game is persisted and returned with status `draft`

### Requirement: Reject empty patches and preserve unchanged timestamps
An empty update object SHALL return HTTP 422 validation ProblemDetails because at least one mutable field must be supplied. A nonempty valid patch whose supplied values equal the current values after normalization SHALL return HTTP 200 with the current game without changing updatedAt. An effective change SHALL set updatedAt to the server's current UTC instant while preserving id and createdAt.

#### Scenario: Reject an empty object
- **WHEN** a client submits `{}` for an existing game
- **THEN** the response is HTTP 422 validation ProblemDetails and storage is unchanged

#### Scenario: No effective change
- **WHEN** a nonempty patch supplies only existing values, including strings that differ only by surrounding whitespace
- **THEN** the response is HTTP 200 with the existing game and its original updatedAt

#### Scenario: Persist a real change
- **WHEN** at least one supplied value differs after normalization
- **THEN** the response and storage contain the changes and a server-generated UTC updatedAt
- **AND** id and createdAt remain unchanged

### Requirement: Enforce update slug uniqueness
A supplied normalized slug SHALL NOT belong to another game. Keeping the target game's own slug SHALL be allowed. A conflicting update SHALL return HTTP 409 ProblemDetails and persist none of the patch, including when a duplicate-slug constraint is encountered during saving.

#### Scenario: Preserve the current slug
- **WHEN** a patch includes the target game's own slug with optional surrounding whitespace
- **THEN** the patch does not produce a slug conflict

#### Scenario: Conflict with another game
- **WHEN** a patch supplies another game's slug together with a valid name change
- **THEN** the response is HTTP 409 ProblemDetails
- **AND** both games, including their names and timestamps, remain unchanged

#### Scenario: Persistence detects a slug race
- **WHEN** a patch passes the initial uniqueness check but saving encounters the recognized duplicate game slug constraint
- **THEN** the response is HTTP 409 ProblemDetails and no partial changes from the patch are persisted

### Requirement: Enforce the game merge patch HTTP contract
Updates SHALL accept JSON objects as application/merge-patch+json or application/json with identical merge-patch semantics; charset parameters SHALL be allowed. Other media types SHALL return HTTP 415 ProblemDetails. Malformed JSON, incompatible types, invalid GUID or enum representations, non-object bodies, and unknown or read-only fields SHALL return HTTP 400 ProblemDetails. PUT SHALL NOT be exposed.

#### Scenario: Equivalent supported media types
- **WHEN** equivalent patches use application/merge-patch+json or application/json against equivalent game states
- **THEN** both apply the same omission, explicit null, validation, and update rules
- **AND** both return the same response status and game values, apart from independently generated update timestamps

#### Scenario: Reject unsupported media types
- **WHEN** a client submits an update using application/json-patch+json or text/plain
- **THEN** the response is HTTP 415 ProblemDetails and the game remains unchanged

#### Scenario: Accept a charset parameter
- **WHEN** a valid patch uses application/merge-patch+json; charset=utf-8 or application/json; charset=utf-8
- **THEN** the media type is accepted

#### Scenario: Reject unknown and read-only fields
- **WHEN** a patch includes an unknown property or id, createdAt, or updatedAt, even alongside valid changes
- **THEN** the response is HTTP 400 ProblemDetails and the game remains unchanged

#### Scenario: Reject invalid JSON shapes and representations
- **WHEN** the patch is malformed JSON, an array, a scalar, or null, or contains an invalid mapId GUID, a numeric or unknown status, or a non-string name, slug, or image
- **THEN** the response is HTTP 400 ProblemDetails and the game remains unchanged

#### Scenario: No PUT update route
- **WHEN** a client attempts PUT on the game resource
- **THEN** the request cannot update the game

### Requirement: Safe failures and existing endpoint compatibility during updates
Update failures SHALL use ProblemDetails. Unexpected storage or library failures SHALL return a safe HTTP 500 without exception details, SQL, or connection information; unrelated database failures SHALL NOT become slug conflicts. Introducing PATCH validation SHALL preserve existing POST and GET behavior, including POST ignoring additional status input and creating a draft.

#### Scenario: Unexpected update storage failure
- **WHEN** saving a patch fails for a reason other than the recognized duplicate-slug constraint
- **THEN** the response is HTTP 500 ProblemDetails without internal details and no partial patch is persisted

#### Scenario: Preserve creation behavior
- **WHEN** an otherwise valid POST includes extra status, identity, or timestamp fields
- **THEN** creation continues to generate server metadata and create a draft, as before

### Requirement: Document the game update contract
OpenAPI SHALL describe PATCH `/games/{id}` as UpdateGame with a GUID path parameter, application/merge-patch+json and application/json request bodies sharing one schema and semantics, five optional mutable fields, the complete game response, and ProblemDetails for 400, 404, 409, 415, 422, and 500. It SHALL distinguish optional presence from nullability and describe nonempty body validation and immutable fields.

#### Scenario: Discover the PATCH operation
- **WHEN** a client reads the generated OpenAPI document
- **THEN** the request has no individually required mutable fields, allows null only for mapId, and exposes metadata length and slug format constraints
- **AND** status is documented using string values, the response contains all eight game fields, and PUT is absent
- **AND** both supported request media types describe the same merge-patch behavior and request schema
