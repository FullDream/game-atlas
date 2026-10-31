# Spec Delta

## ADDED Requirements

### Requirement: Store optional descriptive metadata

Creation and PATCH SHALL accept nullable releaseDate, developer and description. releaseDate SHALL be a calendar date in YYYY-MM-DD format without time or timezone. Developer SHALL be trimmed, with blanks normalized to null. Description SHALL be plain text preserving line breaks, with empty text normalized to null. Omitted creation values SHALL default to null. All game responses SHALL return these persisted values.

#### Scenario: Save clear and reload metadata

- **WHEN** a game is created with metadata and reloaded after restart
- **THEN** the saved date, developer and multiline description are returned
- **AND** PATCH null clears each value, omission preserves it, and invalid calendar dates return 400 atomically

### Requirement: Select an optional library banner

Creation and PATCH SHALL accept nullable bannerId selecting an existing library ImageAsset. Creation omission SHALL mean no banner; PATCH null SHALL clear it. Missing assets SHALL return 404, and video/folder references 422. Responses SHALL return canonical ImageAsset or null at banner. Replacement and game deletion SHALL preserve library assets. Cover SHALL remain required.

#### Scenario: Attach replace and clear

- **WHEN** a banner is attached, replaced and cleared
- **THEN** responses resolve each selected asset and finally banner=null without deleting assets
- **AND** invalid references reject the whole mutation and coverId=null returns 422

### Requirement: Return game-wide content counts

Every game response SHALL include markersCount and categoriesCount as nonnegative counts across all maps owned by that game. Categories without markers SHALL count. Category groups and other games SHALL not contribute. Empty games SHALL return zeros. Subsequent reads SHALL reflect content changes without modifying game timestamps.

#### Scenario: Count isolated content without duplication

- **WHEN** several maps contain categories and markers and another game owns additional content
- **THEN** counts include only owned content once per entity
- **AND** deletion reduces subsequent counts

### Requirement: Return lightweight map summaries

Every game response SHALL include read-only maps ordered by map createdAt then id ascending. Entries SHALL contain only id, name and preview. preview SHALL be canonical ImageAsset from the current default style source, or null when no ready style exists. Empty games SHALL return maps=[]. Complete map endpoints and default-style rules SHALL remain unchanged. PATCH maps or counts SHALL return 400.

#### Scenario: Resolve the current default source

- **WHEN** a map has unfinished and ready styles
- **THEN** preview uses the earliest-created surviving ready style source
- **AND** subsequent reads reflect default changes and return null when no ready style remains

#### Scenario: Preserve blank maps and isolation

- **WHEN** a game has blank maps and another game has styled maps
- **THEN** only owned maps are returned, including null previews, without geometry or styles

### Requirement: Upgrade the expanded game contract safely

The contract SHALL replace response image with cover and request imageAssetId with coverId. Existing persisted cover references, identity, timestamps and status SHALL survive migration. New nullable values SHALL initialize to null. OpenAPI SHALL expose all added fields and reuse canonical ImageAsset for cover, banner and map preview. Legacy imageAssetId SHALL be rejected in PATCH with 400.

#### Scenario: Upgrade an existing game

- **WHEN** an existing game with content is migrated
- **THEN** cover resolves the original asset, optional metadata and banner are null, and counts and map summaries reflect existing content
- **AND** timestamps and shared assets remain unchanged

## MODIFIED Requirements

### Requirement: Create a draft game

POST /games SHALL accept unauthenticated name, slug, and required coverId input selecting an existing library image and return HTTP 200 with id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt, and string status. Response cover SHALL be the complete canonical ImageAsset object. The server SHALL assign draft status and generate identity/timestamps. Game representations SHALL NOT include mapId, a selected map, or a selected map; read-only map summaries SHALL be included. Additional server-owned creation properties SHALL NOT assign maps.

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

### Requirement: Normalize and validate game metadata

The system SHALL trim name and slug and require nonempty strings of at most 200 characters each. Creation coverId SHALL be a nonempty UUID selecting an existing library image. Missing/null/empty UUID SHALL return 422, malformed UUID input 400, missing assets 404, and video/folder references 422. Invalid input SHALL NOT create a game. Arbitrary image strings SHALL NOT satisfy image selection.

#### Scenario: Trim accepted input

- **WHEN** valid strings include surrounding whitespace
- **THEN** their trimmed values are stored and returned

#### Scenario: Accept an image string without URL syntax

- **WHEN** only legacy image=`cover-image` is supplied without coverId and all other fields are valid
- **THEN** creation returns 422 for missing coverId and creates no game

#### Scenario: Reject absent or empty required strings

- **WHEN** name or slug is missing, null, empty, or whitespace-only, or coverId is missing, null, or the empty UUID
- **THEN** the response is HTTP 422 validation ProblemDetails identifying invalid fields
- **AND** no game is created

#### Scenario: Enforce length limits after trimming

- **WHEN** trimmed name or slug exceeds 200 characters
- **THEN** the response is HTTP 422 validation ProblemDetails and no game is created

#### Scenario: Accept exact length limits

- **WHEN** name has 200 characters, a syntactically valid unique slug has 200 characters, and coverId selects a valid library image
- **THEN** creation succeeds

### Requirement: Durable game metadata

Created games SHALL survive application restarts, preserving persisted metadata and image references. Status SHALL be stored as a string. Map ownership SHALL be persisted separately and SHALL NOT require restoring a singular mapId property on the game representation.

#### Scenario: Reload persisted game

- **WHEN** a successfully created game is loaded after reopening the database
- **THEN** its ID, normalized metadata, timestamps, and Draft status match persisted creation metadata without mapId
- **AND** the stored status is a string and separately created maps retain their own identities and game ownership

### Requirement: Browse games with complete metadata

Unauthenticated GET /games SHALL include draft and published games using the same expanded fields as creation: id, name, slug, cover, banner, releaseDate, developer, description, markersCount, categoriesCount, maps, createdAt, updatedAt, and string status. The response SHALL NOT include singular mapId or embed complete map representations; read-only map summaries SHALL be included. Existing offset/limit/search/sort behavior SHALL remain unchanged.

#### Scenario: Browse games with zero or several maps

- **WHEN** games with zero, one, or several maps are listed
- **THEN** every game uses the same expanded representation and maps are available through GET /games/{gameId}/maps
- **AND** catalog paging, filtering, and sorting do not depend on a selected map

#### Scenario: Return drafts and published games

- **WHEN** draft and published games owning zero or several maps are listed
- **THEN** each item uses the same expanded representation as creation
- **AND** the existing paginated envelope, filters, and sorting remain unchanged

### Requirement: Partially update an existing game

Unauthenticated PATCH /games/{id} SHALL allow only name, slug, coverId, bannerId, releaseDate, developer, description, and status to change and return HTTP 200 with the complete expanded game representation with cover resolved to the canonical ImageAsset object. A valid request for a missing game SHALL return HTTP 404 without creating a game. Game updates SHALL NOT create, select, reassign, or remove maps.

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

### Requirement: Preserve omission and explicit null during game updates

Every mutable game field SHALL be optional. Omitted values SHALL remain unchanged; omitting coverId SHALL preserve the selected image. Null name, slug, coverId, or status SHALL return HTTP 422. mapId SHALL NOT be a mutable or response property; any supplied mapId during PATCH SHALL return HTTP 400 without changes.

#### Scenario: Preserve omitted fields and reject map assignment

- **WHEN** only name is patched
- **THEN** other game metadata and all maps remain unchanged
- **AND** any accompanying mapId, including null, rejects the entire patch with HTTP 400

#### Scenario: Omitted fields remain unchanged

- **WHEN** a published game with several maps is patched only with a name
- **THEN** slug, cover, status, and every map/content association remain unchanged

#### Scenario: Clear or replace a map identifier

- **WHEN** game PATCH supplies mapId as null or a valid GUID
- **THEN** HTTP 400 rejects the complete patch, including accompanying valid metadata changes
- **AND** map management remains available only through map operations

#### Scenario: Reject null required values

- **WHEN** game PATCH explicitly supplies null name, slug, coverId, or status
- **THEN** HTTP 422 identifies invalid fields and preserves every stored value and timestamp

### Requirement: Validate supplied game update metadata

Supplied name and slug SHALL follow creation string and slug rules. Supplied coverId SHALL be a nonempty UUID selecting an existing library image; null/empty UUID SHALL return 422, malformed UUID input 400, missing assets 404, and video/folder references 422. The legacy image input SHALL return 400 as an unknown field. Failures SHALL apply no changes.

#### Scenario: Trim update values and accept length boundaries

- **WHEN** supplied strings have surrounding whitespace and their trimmed values satisfy syntax and length limits, including exact maximum lengths
- **THEN** the trimmed values are persisted and returned

#### Scenario: Reject invalid metadata atomically

- **WHEN** a patch combines a valid name change with a null or empty-UUID coverId, an overlength string, or an invalid slug
- **THEN** the response is HTTP 422 validation ProblemDetails identifying the invalid fields
- **AND** neither the valid change nor any other game field is persisted

#### Scenario: Preserve plain image string semantics

- **WHEN** a patch supplies image as `cover-image`
- **THEN** HTTP 400 rejects the unknown image property and preserves all game data

### Requirement: Enforce the game merge patch HTTP contract

PATCH SHALL accept application/merge-patch+json and application/json with identical RFC 7396 semantics, including charset parameters. Other media SHALL return 415. Malformed JSON, incompatible types, invalid enum representations, non-object bodies, and unknown/read-only properties SHALL return 400. PUT SHALL NOT be exposed.

#### Scenario: Equivalent media and invalid fields

- **WHEN** valid equivalent patches use either supported media type
- **THEN** they apply the same omission/null/update rules
- **AND** mapId, embedded maps, IDs, timestamps, malformed shapes, or unknown status values reject the complete patch with 400; unsupported media return 415

#### Scenario: Equivalent supported media types

- **WHEN** equivalent valid patches use either supported JSON Merge Patch media type
- **THEN** both apply the same omission/null/validation rules and return the expanded game response

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

Update failures SHALL use safe ProblemDetails and atomic persistence. Unexpected failures SHALL return 500 without being mislabeled as slug conflicts. POST SHALL continue to ignore server-owned creation input and create drafts. GET and mutation responses SHALL consistently use expanded fields, excluding mapId, while preserving unrelated catalog behavior.

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

OpenAPI SHALL document UpdateGame with eight optional mutable fields (name, slug, coverId, bannerId, releaseDate, developer, description, status), both supported PATCH media types, the expanded response with required canonical ImageAsset at cover, and ProblemDetails for 400/404/409/415/422/500. It SHALL omit mapId from response and request schemas and distinguish optional presence from required-field null rejection.

#### Scenario: Discover game metadata separately from maps

- **WHEN** a client reads OpenAPI
- **THEN** game creation, listing, and update share the expanded response fields and no mapId
- **AND** maps are managed through explicit map operations, not game PATCH

#### Scenario: Discover the PATCH operation

- **WHEN** a client reads OpenAPI
- **THEN** UpdateGame exposes eight optional mutable fields, required-value null rejection, both PATCH media types, and the expanded response
- **AND** mapId is absent from requests and responses, maps is read-only in responses and PUT is absent

#### Scenario: Discover the image input and output contracts

- **WHEN** a client inspects game creation, listing, and PATCH schemas
- **THEN** creation requires UUID coverId, PATCH permits optional UUID coverId and documents null rejection, and every Game.cover references canonical ImageAsset
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
- **AND** other games using that cover remain unchanged

### Requirement: Document the game deletion contract

OpenAPI SHALL expose DeleteGame at DELETE /games/{id} with a GUID path parameter, no request body, empty HTTP 204, and ProblemDetails 400/404/500. Documentation SHALL describe cleanup of all owned maps and content and the expanded game DTO used by other operations.

#### Scenario: Discover deletion and ownership

- **WHEN** a client reads the game deletion contract
- **THEN** it documents physical removal of the whole game's ownership hierarchy and deferred cleanup of map files
- **AND** no response includes a singular map association

#### Scenario: Discover deletion

- **WHEN** a client reads OpenAPI for DeleteGame
- **THEN** the GUID path, no required body, empty 204, and ProblemDetails 400/404/500 are documented
- **AND** other game operations use the expanded DTO and maps remain a separate collection

### Requirement: Resolve shared game images consistently

All game success responses SHALL return the complete canonical ImageAsset at cover, including type, URL, dimensions, alt, placeholder, and shared metadata. They SHALL resolve current library metadata without changing game timestamps for asset-only edits. Paginated listing SHALL avoid one asset read per game and SHALL preserve existing pagination, totals, filtering, and sorting.

#### Scenario: Resolve creation listing and update images

- **WHEN** a game is created, listed, or updated with a library image
- **THEN** cover contains the same complete representation as that asset in the library, including nullable placeholder and type=image
- **AND** the game keeps expanded root fields and does not expose coverId as an extra response property

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

- **WHEN** PATCH supplies another valid coverId and valid metadata
- **THEN** the reference and metadata change together and the response resolves the new image
- **AND** the former image remains in the library

#### Scenario: Keep the same cover

- **WHEN** PATCH supplies the current coverId without any other effective change
- **THEN** it returns 200 with the current image and unchanged updatedAt

#### Scenario: Refuse an invalid reference atomically

- **WHEN** creation or PATCH selects a missing asset, video, or folder, including alongside otherwise valid metadata
- **THEN** it returns the specified 404 or 422 without partial writes or timestamp changes

### Requirement: Retrieve a game by identity

Unauthenticated GET /games/{id} SHALL return HTTP 200 with the same expanded Game representation as listing, including canonical ImageAsset, for draft and published games. It SHALL include read-only map summaries or change persisted data.

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

### Requirement: Optional game list sorting

The list SHALL accept sortBy names name, createdAt, or updatedAt and sortDirection names asc or desc, case-insensitively. Other standard enum forms SHALL be accepted when their resulting values are defined. With sortBy and no direction, direction SHALL default to asc. Explicit sorting SHALL precede pagination. With sortBy omitted, games SHALL be ordered by createdAt ascending. All sorting SHALL use id ascending to break ties before pagination.

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
- **THEN** pagination is applied after ordering by createdAt ascending then id ascending
- **AND** repeated reads of unchanged data return the same page boundaries
