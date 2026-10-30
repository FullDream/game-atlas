## ADDED Requirements

### Requirement: Retrieve a game by identity

Unauthenticated GET /games/{id} SHALL return HTTP 200 with the same seven-field Game representation as listing, including canonical ImageAsset, for draft and published games. It SHALL NOT embed maps or change persisted data.

#### Scenario: Read either status

- **WHEN** a client requests an existing draft or published game by its UUID
- **THEN** the response contains id, name, slug, image, createdAt, updatedAt and string status matching persisted data
- **AND** no maps are created or returned and timestamps remain unchanged

#### Scenario: Missing game

- **WHEN** a client requests a valid UUID that does not identify a game, including a deleted game
- **THEN** HTTP 404 ProblemDetails is returned

#### Scenario: Malformed identifier

- **WHEN** a client requests a malformed UUID
- **THEN** HTTP 400 is returned without a database write

### Requirement: Document game retrieval

OpenAPI SHALL expose game retrieval under the Games tag with a UUID path parameter, the canonical Game response and safe documented error responses for invalid identifiers, missing games and unexpected failures.

#### Scenario: Generate the detail operation

- **WHEN** the Games client is regenerated from the running backend document
- **THEN** it includes retrieval by ID using the existing Game and ImageAsset schemas
