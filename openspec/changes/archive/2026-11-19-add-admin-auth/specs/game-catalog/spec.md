## MODIFIED Requirements

### Requirement: Create a draft game

POST /games SHALL accept authenticated requests with name, slug, and required coverId input selecting an existing library image and return HTTP 200 with id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt, and string status. Response cover SHALL be the complete canonical ImageAsset object. The server SHALL assign draft status and generate identity/timestamps. Game representations SHALL NOT include mapId or a selected map; read-only map summaries SHALL be included. Additional server-owned creation properties SHALL NOT assign maps.

#### Scenario: Create a game before any maps

- **WHEN** valid name, slug, and coverId are submitted
- **THEN** exactly one draft game is persisted and its expanded fields are returned without mapId
- **AND** no map is created, including when extra mapId or status input is supplied

#### Scenario: Create without a map

- **WHEN** a client submits valid game name, slug, and coverId
- **THEN** HTTP 200 returns the expanded draft game without mapId and with read-only map summaries
- **AND** no map is created

#### Scenario: Create with a map identifier

- **WHEN** valid creation includes an additional mapId value
- **THEN** the value is ignored and cannot create, select, or assign a map
- **AND** the game response has no mapId field

#### Scenario: Explicitly null map

- **WHEN** valid game creation additionally supplies mapId=null
- **THEN** creation succeeds without creating a map or returning mapId

#### Scenario: Client cannot choose publication status

- **WHEN** valid game creation additionally supplies status=published
- **THEN** the server still creates and returns status=draft

### Requirement: Browse games with complete metadata

Authenticated GET /games SHALL include draft and published games using the same expanded fields as creation: id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt, and string status. The response SHALL NOT include singular mapId or embed complete map representations; read-only map summaries SHALL be included. Existing offset/limit/search/sort behavior SHALL remain unchanged.

#### Scenario: Browse games with zero or several maps

- **WHEN** games with zero, one, or several maps are listed
- **THEN** every game uses the same expanded representation and maps are available through GET /games/{gameId}/maps
- **AND** catalog paging, filtering, and sorting do not depend on a selected map

#### Scenario: Return drafts and published games

- **WHEN** draft and published games owning zero or several maps are listed
- **THEN** each item uses the same expanded representation as creation
- **AND** the existing paginated envelope, filters, and sorting remain unchanged

### Requirement: Partially update an existing game

Authenticated PATCH /games/{id} SHALL allow only name, slug, coverId, bannerId, releaseDate, developer, description, and status to change and return HTTP 200 with the complete expanded game representation with cover resolved to the canonical ImageAsset object. A valid request for a missing game SHALL return HTTP 404 without creating a game. Game updates SHALL NOT create, select, reassign, or remove maps.

#### Scenario: Update metadata independently of maps

- **WHEN** a valid patch changes game metadata for a game with several maps
- **THEN** the complete updated game is returned without mapId and all maps/content retain their ownership and values

#### Scenario: Update every mutable field

- **WHEN** valid name, unique slug, coverId, and status are patched together
- **THEN** HTTP 200 returns the complete expanded updated game with unchanged identity/createdAt
- **AND** all maps and content remain unchanged after storage is reopened

#### Scenario: Update a missing game

- **WHEN** a valid nonempty patch identifies a missing game
- **THEN** HTTP 404 returns ProblemDetails without creating a game

### Requirement: Permanently delete an existing game

Authenticated DELETE /games/{id} SHALL physically delete an existing draft or published game and return empty HTTP 204 after persistence. All owned maps, styles, groups, categories, markers, and marker-media records SHALL be removed, with durable artifact cleanup for every map. Other games SHALL remain unchanged. Referenced marker-media files and the selected game image asset SHALL NOT be deleted.

#### Scenario: Delete several maps with mixed processing states

- **WHEN** a game with blank, ready, queued, or processing maps is deleted
- **THEN** every owned content hierarchy disappears and cleanup survives restart for every map ID
- **AND** late work cannot recreate deleted resources and another game's content remains unchanged

#### Scenario: Delete a game without maps

- **WHEN** an existing game owns no maps
- **THEN** deletion still returns empty HTTP 204 without requiring an image or map prerequisite

#### Scenario: Delete a draft

- **WHEN** an existing draft game is deleted by an authenticated administrator without a body
- **THEN** empty HTTP 204 follows durable deletion, including every owned map hierarchy
- **AND** other games and their metadata remain unchanged

#### Scenario: Delete a published game

- **WHEN** an existing published game is deleted
- **THEN** empty HTTP 204 physically removes it and its maps/content without first unpublishing it

#### Scenario: Preserve a game image after game deletion

- **WHEN** a game using a shared library image is deleted
- **THEN** the image remains in the library with the same bytes and metadata
- **AND** other games using that cover remain unchanged

### Requirement: Retrieve a game by identity

Authenticated GET /games/{id} SHALL return HTTP 200 with the same expanded Game representation as listing, including canonical ImageAsset, for draft and published games. It SHALL include read-only map summaries and SHALL NOT change persisted data.

#### Scenario: Read either status

- **WHEN** a client requests an existing draft or published game by its UUID
- **THEN** the response contains id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt and string status matching persisted data
- **AND** no maps are created and existing maps are returned only as summaries and timestamps remain unchanged

#### Scenario: Missing game

- **WHEN** a client requests a valid UUID that does not identify a game, including a deleted game
- **THEN** HTTP 404 ProblemDetails is returned

#### Scenario: Malformed identifier

- **WHEN** a client requests a malformed UUID
- **THEN** HTTP 400 is returned without a database write
