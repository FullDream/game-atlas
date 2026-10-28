# Game catalog delta

## MODIFIED Requirements

### Requirement: Create a draft game

POST /games SHALL accept unauthenticated name, slug, and required imageAssetId input selecting an existing library image and return HTTP 200 with id, name, slug, image, createdAt, updatedAt, and string status. Response image SHALL be the complete canonical ImageAsset object. The server SHALL assign draft status and generate identity/timestamps. Game representations SHALL NOT include mapId, a selected map, or an embedded maps collection. Additional server-owned creation properties SHALL NOT assign maps.

#### Scenario: Create a game before any maps

- **WHEN** valid name, slug, and imageAssetId are submitted
- **THEN** exactly one draft game is persisted and its seven fields are returned without mapId
- **AND** no map is created, including when extra mapId or status input is supplied

#### Scenario: Create without a map

- **WHEN** a client submits valid game name, slug, and imageAssetId
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

### Requirement: Normalize and validate game metadata

The system SHALL trim name and slug and require nonempty strings of at most 200 characters each. Creation imageAssetId SHALL be a nonempty UUID selecting an existing library image. Missing/null/empty UUID SHALL return 422, malformed UUID input 400, missing assets 404, and video/folder references 422. Invalid input SHALL NOT create a game. Arbitrary image strings SHALL NOT satisfy image selection.

#### Scenario: Trim accepted input

- **WHEN** valid strings include surrounding whitespace
- **THEN** their trimmed values are stored and returned

#### Scenario: Accept an image string without URL syntax

- **WHEN** only legacy image=`cover-image` is supplied without imageAssetId and all other fields are valid
- **THEN** creation returns 422 for missing imageAssetId and creates no game

#### Scenario: Reject absent or empty required strings

- **WHEN** name or slug is missing, null, empty, or whitespace-only, or imageAssetId is missing, null, or the empty UUID
- **THEN** the response is HTTP 422 validation ProblemDetails identifying invalid fields
- **AND** no game is created

#### Scenario: Enforce length limits after trimming

- **WHEN** trimmed name or slug exceeds 200 characters
- **THEN** the response is HTTP 422 validation ProblemDetails and no game is created

#### Scenario: Accept exact length limits

- **WHEN** name has 200 characters, a syntactically valid unique slug has 200 characters, and imageAssetId selects a valid library image
- **THEN** creation succeeds

### Requirement: Partially update an existing game

Unauthenticated PATCH /games/{id} SHALL allow only name, slug, imageAssetId, and status to change and return HTTP 200 with the complete seven-field game representation with image resolved to the canonical ImageAsset object. A valid request for a missing game SHALL return HTTP 404 without creating a game. Game updates SHALL NOT create, select, reassign, or remove maps.

#### Scenario: Update metadata independently of maps

- **WHEN** a valid patch changes game metadata for a game with several maps
- **THEN** the complete updated game is returned without mapId and all maps/content retain their ownership and values

#### Scenario: Update every mutable field

- **WHEN** valid name, unique slug, imageAssetId, and status are patched together
- **THEN** HTTP 200 returns the complete seven-field updated game with unchanged identity/createdAt
- **AND** all maps and content remain unchanged after storage is reopened

#### Scenario: Update a missing game

- **WHEN** a valid nonempty patch identifies a missing game
- **THEN** HTTP 404 returns ProblemDetails without creating a game

### Requirement: Preserve omission and explicit null during game updates

Every mutable game field SHALL be optional. Omitted values SHALL remain unchanged; omitting imageAssetId SHALL preserve the selected image. Null name, slug, imageAssetId, or status SHALL return HTTP 422. mapId SHALL NOT be a mutable or response property; any supplied mapId during PATCH SHALL return HTTP 400 without changes.

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

- **WHEN** game PATCH explicitly supplies null name, slug, imageAssetId, or status
- **THEN** HTTP 422 identifies invalid fields and preserves every stored value and timestamp

### Requirement: Validate supplied game update metadata

Supplied name and slug SHALL follow creation string and slug rules. Supplied imageAssetId SHALL be a nonempty UUID selecting an existing library image; null/empty UUID SHALL return 422, malformed UUID input 400, missing assets 404, and video/folder references 422. The legacy image input SHALL return 400 as an unknown field. Failures SHALL apply no changes.

#### Scenario: Trim update values and accept length boundaries

- **WHEN** supplied strings have surrounding whitespace and their trimmed values satisfy syntax and length limits, including exact maximum lengths
- **THEN** the trimmed values are persisted and returned

#### Scenario: Reject invalid metadata atomically

- **WHEN** a patch combines a valid name change with a null or empty-UUID imageAssetId, an overlength string, or an invalid slug
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid fields
- **AND** neither the valid change nor any other game field is persisted

#### Scenario: Preserve plain image string semantics

- **WHEN** a patch supplies image as `cover-image`
- **THEN** HTTP 400 rejects the unknown image property and preserves all game data

### Requirement: Document the game update contract

OpenAPI SHALL document UpdateGame with four optional mutable fields (name, slug, imageAssetId, status), both supported PATCH media types, the seven-field response with required canonical ImageAsset at image, and ProblemDetails for 400/404/409/415/422/500. It SHALL omit mapId from response and request schemas and distinguish optional presence from required-field null rejection.

#### Scenario: Discover game metadata separately from maps

- **WHEN** a client reads OpenAPI
- **THEN** game creation, listing, and update share seven response fields and no mapId
- **AND** maps are managed through explicit map operations, not game PATCH

#### Scenario: Discover the PATCH operation

- **WHEN** a client reads OpenAPI
- **THEN** UpdateGame exposes four optional mutable fields, required-value null rejection, both PATCH media types, and the seven-field response
- **AND** mapId and maps are absent from mutable and response properties and PUT is absent

#### Scenario: Discover the image input and output contracts

- **WHEN** a client inspects game creation, listing, and PATCH schemas
- **THEN** creation requires UUID imageAssetId, PATCH permits optional UUID imageAssetId and documents null rejection, and every Game.image references canonical ImageAsset
- **AND** no request exposes the old image string property and no duplicate image component is introduced

### Requirement: Permanently delete an existing game

Unauthenticated DELETE /games/{id} SHALL physically delete an existing draft or published game and return empty HTTP 204 after persistence. All owned maps, styles, groups, categories, markers, and marker-media records SHALL be removed, with durable artifact cleanup for every map. Other games SHALL remain unchanged. Referenced marker-media files and the selected game image asset SHALL NOT be deleted.

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

#### Scenario: Preserve a game image after game deletion

- **WHEN** a game using a shared library image is deleted
- **THEN** the image remains in the library with the same bytes and metadata
- **AND** other games using that image remain unchanged

## ADDED Requirements

### Requirement: Resolve shared game images consistently

All game success responses SHALL return the complete canonical ImageAsset at image, including type, URL, dimensions, alt, placeholder, and shared metadata. They SHALL resolve current library metadata without changing game timestamps for asset-only edits. Paginated listing SHALL avoid one asset read per game and SHALL preserve existing pagination, totals, filtering, and sorting.

#### Scenario: Resolve creation listing and update images

- **WHEN** a game is created, listed, or updated with a library image
- **THEN** image contains the same complete representation as that asset in the library, including nullable placeholder and type=image
- **AND** the game keeps seven root fields and does not expose imageAssetId as an extra response property

#### Scenario: Share metadata across games

- **WHEN** a shared image is renamed, moved, or given new alt text
- **THEN** subsequent game responses reflect the updated shared metadata without changing game updatedAt

#### Scenario: Load a page with several covers

- **WHEN** a page contains several games using distinct or reused images
- **THEN** images are resolved without a separate asset read for each game and existing list totals and page boundaries remain correct

#### Scenario: Read metadata after physical file removal

- **WHEN** an image's physical original is missing outside the application
- **THEN** game metadata still resolves its persisted image representation and file delivery returns 404

### Requirement: Replace game image references atomically

Selecting another image SHALL update the game reference atomically with other supplied fields. Selecting the same image without other effective changes SHALL preserve updatedAt. Replacing the reference SHALL NOT delete the old asset. Multiple games SHALL be able to select the same image independently.

#### Scenario: Replace a cover

- **WHEN** PATCH supplies another valid imageAssetId and valid metadata
- **THEN** the reference and metadata change together and the response resolves the new image
- **AND** the former image remains in the library

#### Scenario: Keep the same cover

- **WHEN** PATCH supplies the current imageAssetId without any other effective change
- **THEN** it returns 200 with the current image and unchanged updatedAt

#### Scenario: Refuse an invalid reference atomically

- **WHEN** creation or PATCH selects a missing asset, video, or folder, including alongside otherwise valid metadata
- **THEN** it returns the specified 404 or 422 without partial writes or timestamp changes

### Requirement: Initialize game image references without legacy conversion

The migration chain SHALL support a fresh empty database. Conversion of existing arbitrary game image strings SHALL NOT be required. A legacy database containing games SHALL be rejected before changing those records. Applied migrations SHALL remain unchanged; migration SHALL NOT automatically reset the database, delete games/files, or invent asset references.

#### Scenario: Initialize an empty database

- **WHEN** all migrations are explicitly applied to a fresh database
- **THEN** games can select required existing library images and dangling asset references are prevented

#### Scenario: Refuse populated legacy games safely

- **WHEN** the new migration is attempted against a legacy database containing games with image strings
- **THEN** it fails clearly without deleting games, changing their strings, or deleting files
