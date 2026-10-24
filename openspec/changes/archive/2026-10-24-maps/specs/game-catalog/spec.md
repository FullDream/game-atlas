# Game Catalog Spec Delta

## MODIFIED Requirements

### Requirement: Create a draft game
POST /games SHALL accept unauthenticated name, slug, and image input and return HTTP 200 with id, name, slug, image, createdAt, updatedAt, and string status. The server SHALL assign draft status and generate identity/timestamps. Game representations SHALL NOT include mapId, a selected map, or an embedded maps collection. Additional server-owned creation properties SHALL NOT assign maps.

#### Scenario: Create a game before any maps
- **WHEN** valid name, slug, and image are submitted
- **THEN** exactly one draft game is persisted and its seven fields are returned without mapId
- **AND** no map is created, including when extra mapId or status input is supplied

#### Scenario: Create without a map
- **WHEN** a client submits valid game name, slug, and image
- **THEN** HTTP 200 returns the seven-field draft game without mapId or an embedded maps list
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

### Requirement: Safe API error responses
Request failures SHALL use ProblemDetails. Malformed JSON SHALL return HTTP 400. Extra mapId input during game creation SHALL be ignored without assigning a map. Unexpected library/database failures SHALL return safe HTTP 500 without internal details. Unrelated failures SHALL NOT be classified as slug conflicts.

#### Scenario: Invalid JSON and storage failure
- **WHEN** creation receives malformed JSON or encounters an unexpected storage failure
- **THEN** it returns HTTP 400 or safe HTTP 500 respectively without partial creation or internal diagnostics

#### Scenario: Malformed request
- **WHEN** game creation receives malformed JSON
- **THEN** HTTP 400 ProblemDetails is returned without creating a game

#### Scenario: Unexpected persistence failure
- **WHEN** game persistence fails unexpectedly without a recognized slug conflict
- **THEN** safe HTTP 500 is returned without SQL, connection, or exception details

### Requirement: Durable game metadata
Created games SHALL survive application restarts, preserving their seven returned fields. Status SHALL be stored as a string. Map ownership SHALL be persisted separately and SHALL NOT require restoring a singular mapId property on the game representation.

#### Scenario: Reload persisted game
- **WHEN** a successfully created game is loaded after reopening the database
- **THEN** its ID, normalized metadata, timestamps, and Draft status match the seven-field creation response without mapId
- **AND** the stored status is a string and separately created maps retain their own identities and game ownership

### Requirement: Browse games with complete metadata
Unauthenticated GET /games SHALL include draft and published games using the same seven fields as creation: id, name, slug, image, createdAt, updatedAt, and string status. The response SHALL NOT include singular mapId or embed map collections. Existing offset/limit/search/sort behavior SHALL remain unchanged.

#### Scenario: Browse games with zero or several maps
- **WHEN** games with zero, one, or several maps are listed
- **THEN** every game uses the same seven-field representation and maps are available through GET /games/{gameId}/maps
- **AND** catalog paging, filtering, and sorting do not depend on a selected map

#### Scenario: Return drafts and published games
- **WHEN** draft and published games owning zero or several maps are listed
- **THEN** each item uses the same seven-field representation as creation
- **AND** the existing paginated envelope, filters, and sorting remain unchanged

### Requirement: Partially update an existing game
Unauthenticated PATCH /games/{id} SHALL allow only name, slug, image, and status to change and return HTTP 200 with the complete seven-field game representation. A valid request for a missing game SHALL return HTTP 404 without creating a game. Game updates SHALL NOT create, select, reassign, or remove maps.

#### Scenario: Update metadata independently of maps
- **WHEN** a valid patch changes game metadata for a game with several maps
- **THEN** the complete updated game is returned without mapId and all maps/content retain their ownership and values

#### Scenario: Update every mutable field
- **WHEN** valid name, unique slug, image, and status are patched together
- **THEN** HTTP 200 returns the complete seven-field updated game with unchanged identity/createdAt
- **AND** all maps and content remain unchanged after storage is reopened

#### Scenario: Update a missing game
- **WHEN** a valid nonempty patch identifies a missing game
- **THEN** HTTP 404 returns ProblemDetails without creating a game

### Requirement: Preserve omission and explicit null during game updates
Every mutable game field SHALL be optional. Omitted values SHALL remain unchanged. Null name, slug, image, or status SHALL return HTTP 422. mapId SHALL NOT be a mutable or response property; any supplied mapId during PATCH SHALL return HTTP 400 without changes.

#### Scenario: Preserve omitted fields and reject map assignment
- **WHEN** only name is patched
- **THEN** other game metadata and all maps remain unchanged
- **AND** any accompanying mapId, including null, rejects the entire patch with HTTP 400

#### Scenario: Omitted fields remain unchanged
- **WHEN** a published game with several maps is patched only with a name
- **THEN** slug, image, status, and every map/content association remain unchanged

#### Scenario: Clear or replace a map identifier
- **WHEN** game PATCH supplies mapId as null or a valid GUID
- **THEN** HTTP 400 rejects the complete patch, including accompanying valid metadata changes
- **AND** map management remains available only through map operations

#### Scenario: Reject null required values
- **WHEN** game PATCH explicitly supplies null name, slug, image, or status
- **THEN** HTTP 422 identifies invalid fields and preserves every stored value and timestamp

### Requirement: Enforce the game merge patch HTTP contract
PATCH SHALL accept application/merge-patch+json and application/json with identical RFC 7396 semantics, including charset parameters. Other media SHALL return 415. Malformed JSON, incompatible types, invalid enum representations, non-object bodies, and unknown/read-only properties SHALL return 400. PUT SHALL NOT be exposed.

#### Scenario: Equivalent media and invalid fields
- **WHEN** valid equivalent patches use either supported media type
- **THEN** they apply the same omission/null/update rules
- **AND** mapId, embedded maps, IDs, timestamps, malformed shapes, or unknown status values reject the complete patch with 400; unsupported media return 415

#### Scenario: Equivalent supported media types
- **WHEN** equivalent valid patches use either supported JSON Merge Patch media type
- **THEN** both apply the same omission/null/validation rules and return the seven-field game response

#### Scenario: Reject unsupported media types
- **WHEN** game PATCH uses application/json-patch+json or text/plain
- **THEN** HTTP 415 preserves game and map data

#### Scenario: Accept a charset parameter
- **WHEN** a valid patch uses either supported media type with charset=utf-8
- **THEN** the request is accepted under the same merge-patch rules

#### Scenario: Reject unknown and read-only fields
- **WHEN** a patch supplies id, mapId, maps, timestamps, or unknown fields with otherwise valid changes
- **THEN** HTTP 400 preserves all game and map state

#### Scenario: Reject invalid JSON shapes and representations
- **WHEN** PATCH is malformed, non-object, or has incompatible metadata/enum types, or any mapId value
- **THEN** HTTP 400 returns ProblemDetails without state changes

#### Scenario: No PUT update route
- **WHEN** a client attempts PUT on a game resource
- **THEN** no game update is performed

### Requirement: Safe failures and existing endpoint compatibility during updates
Update failures SHALL use safe ProblemDetails and atomic persistence. Unexpected failures SHALL return 500 without being mislabeled as slug conflicts. POST SHALL continue to ignore server-owned creation input and create drafts. GET and mutation responses SHALL consistently use seven fields, excluding mapId, while preserving unrelated catalog behavior.

#### Scenario: Failure and creation compatibility
- **WHEN** saving an update fails unexpectedly
- **THEN** no partial changes persist and the response contains no internal details
- **AND** creation still generates identity/timestamps and draft status without assigning maps

#### Scenario: Unexpected update storage failure
- **WHEN** game update persistence fails unexpectedly before commit
- **THEN** safe HTTP 500 is returned and every supplied change rolls back

#### Scenario: Preserve creation behavior
- **WHEN** valid POST includes extra identity, mapId, status, or timestamp properties
- **THEN** server-generated metadata and draft status are preserved and no map is assigned

### Requirement: Document the game update contract
OpenAPI SHALL document UpdateGame with four optional mutable fields, both supported PATCH media types, the seven-field response, and ProblemDetails for 400/404/409/415/422/500. It SHALL omit mapId from response and request schemas and distinguish optional presence from required-field null rejection.

#### Scenario: Discover game metadata separately from maps
- **WHEN** a client reads OpenAPI
- **THEN** game creation, listing, and update share seven response fields and no mapId
- **AND** maps are managed through explicit map operations, not game PATCH

#### Scenario: Discover the PATCH operation
- **WHEN** a client reads OpenAPI
- **THEN** UpdateGame exposes four optional mutable fields, required-value null rejection, both PATCH media types, and the seven-field response
- **AND** mapId and maps are absent from mutable and response properties and PUT is absent

### Requirement: Permanently delete an existing game
Unauthenticated DELETE /games/{id} SHALL physically delete an existing draft or published game and return empty HTTP 204 after persistence. All owned maps, styles, groups, categories, markers, and marker-media records SHALL be removed, with durable artifact cleanup for every map. Other games SHALL remain unchanged. Referenced marker-media files SHALL NOT be deleted.

#### Scenario: Delete several maps with mixed processing states
- **WHEN** a game with blank, ready, queued, or processing maps is deleted
- **THEN** every owned content hierarchy disappears and cleanup survives restart for every map ID
- **AND** late work cannot recreate deleted resources and another game's content remains unchanged

#### Scenario: Delete a game without maps
- **WHEN** an existing game owns no maps
- **THEN** deletion still returns empty HTTP 204 without requiring an image or map prerequisite

#### Scenario: Delete a draft
- **WHEN** an existing draft game is deleted without a body or authentication
- **THEN** empty HTTP 204 follows durable deletion, including every owned map hierarchy
- **AND** other games and their metadata remain unchanged

#### Scenario: Delete a published game
- **WHEN** an existing published game is deleted
- **THEN** empty HTTP 204 physically removes it and its maps/content without first unpublishing it

### Requirement: Document the game deletion contract
OpenAPI SHALL expose DeleteGame at DELETE /games/{id} with a GUID path parameter, no request body, empty HTTP 204, and ProblemDetails 400/404/500. Documentation SHALL describe cleanup of all owned maps and content and the seven-field game DTO used by other operations.

#### Scenario: Discover deletion and ownership
- **WHEN** a client reads the game deletion contract
- **THEN** it documents physical removal of the whole game's ownership hierarchy and deferred cleanup of map files
- **AND** no response includes a singular map association

#### Scenario: Discover deletion
- **WHEN** a client reads OpenAPI for DeleteGame
- **THEN** the GUID path, no required body, empty 204, and ProblemDetails 400/404/500 are documented
- **AND** other game operations use the seven-field DTO and maps remain a separate collection
