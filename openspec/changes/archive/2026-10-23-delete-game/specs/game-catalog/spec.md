# Spec Delta

## ADDED Requirements

### Requirement: Permanently delete an existing game
The system SHALL accept unauthenticated DELETE `/games/{id}` without requiring a request body. It SHALL physically remove the identified game regardless of draft or published status and return HTTP 204 with an empty response body only after successful persistence. Other games SHALL remain unchanged.

#### Scenario: Delete a draft
- **WHEN** a client deletes an existing draft game without authentication or a request body
- **THEN** the response is HTTP 204 with no body
- **AND** the game is physically absent from storage, including after reopening the database
- **AND** other games and their metadata remain unchanged

#### Scenario: Delete a published game
- **WHEN** a client deletes an existing published game
- **THEN** the response is HTTP 204 with no body and the game is physically removed
- **AND** no change to draft status is required before deletion

### Requirement: Report missing games during deletion
DELETE targeting a valid GUID for which no game exists SHALL return HTTP 404 ProblemDetails and SHALL NOT change any stored games. Repeating a successful deletion SHALL follow the same missing-game behavior.

#### Scenario: Delete a missing game
- **WHEN** a client deletes a valid game identifier that is not stored
- **THEN** the response is HTTP 404 with application/problem+json
- **AND** stored games remain unchanged

#### Scenario: Repeat a deletion
- **WHEN** a client repeats DELETE after that game was successfully deleted
- **THEN** the response is HTTP 404 ProblemDetails rather than another successful deletion

### Requirement: Reflect permanent deletion in catalog operations
Successfully deleted games SHALL be excluded from subsequent list items and totals. Their slugs SHALL become available for creation or updating another game, subject to the existing slug syntax and uniqueness rules. Subsequent valid updates of deleted identifiers SHALL return HTTP 404 ProblemDetails.

#### Scenario: List after deletion
- **WHEN** a client lists games after a successful deletion
- **THEN** the deleted game is absent from items and is not counted in total

#### Scenario: Reuse a deleted slug during creation
- **WHEN** a valid creation request uses the slug of a successfully deleted game and no existing game owns it
- **THEN** creation succeeds with HTTP 200 and a new server-generated identifier

#### Scenario: Reuse a deleted slug during update
- **WHEN** a valid patch changes another game's slug to the deleted game's slug and no existing game owns it
- **THEN** the patch succeeds under the existing update rules

#### Scenario: Update a deleted game
- **WHEN** a nonempty valid patch targets a successfully deleted identifier
- **THEN** the response is HTTP 404 ProblemDetails and the deleted game is not recreated

### Requirement: Return safe deletion failures
A DELETE path identifier that cannot bind to a GUID SHALL return HTTP 400 ProblemDetails without deleting a game. Unexpected storage or library failures SHALL return HTTP 500 ProblemDetails without exposing exception details, SQL, or connection information. Failed persistence SHALL NOT be reported as successful deletion or as a slug conflict.

#### Scenario: Invalid identifier
- **WHEN** a client sends DELETE `/games/not-a-guid`
- **THEN** the response is HTTP 400 with application/problem+json and storage is unchanged

#### Scenario: Unexpected storage failure
- **WHEN** the deletion storage operation fails before committing
- **THEN** the response is HTTP 500 ProblemDetails without internal details
- **AND** the game remains stored

### Requirement: Document the game deletion contract
OpenAPI SHALL expose DELETE `/games/{id}` with operationId DeleteGame, a required GUID path parameter, no required request body, an HTTP 204 response without a content schema, and ProblemDetails responses for 400, 404, and 500. Existing creation, listing, and update contracts SHALL remain compatible.

#### Scenario: Discover deletion
- **WHEN** a client reads the generated OpenAPI document
- **THEN** the DELETE operation identifies DeleteGame and documents the required id as a uuid-formatted path parameter
- **AND** no request body is required and response 204 has no content schema
- **AND** responses 400, 404, and 500 document application/problem+json
- **AND** the existing POST, GET, and PATCH operations retain their contracts
