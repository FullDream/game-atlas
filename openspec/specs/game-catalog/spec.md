# game-catalog Specification

## Purpose

Maintain the game catalog by creating identifiable draft games with validated metadata and stable, unique client-supplied slugs.

## Requirements

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

### Requirement: Server-generated identity and timestamps

The system SHALL generate each game's ID as UUID v7 and set createdAt and updatedAt to the same UTC instant during creation. Client input SHALL NOT control those fields.

#### Scenario: Generated creation metadata

- **WHEN** a game is created successfully
- **THEN** its ID is a server-generated UUID v7
- **AND** createdAt and updatedAt represent the same UTC instant and are returned in the response

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

- **WHEN** a stored game is named `Elden Ring` and search is `RING`
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

OpenAPI SHALL expose DeleteGame at DELETE /games/{id} with a GUID path parameter, no request body, empty HTTP 204, and ProblemDetails 400/404/500. Documentation SHALL describe cleanup of all owned maps and content and the seven-field game DTO used by other operations.

#### Scenario: Discover deletion and ownership

- **WHEN** a client reads the game deletion contract
- **THEN** it documents physical removal of the whole game's ownership hierarchy and deferred cleanup of map files
- **AND** no response includes a singular map association

#### Scenario: Discover deletion

- **WHEN** a client reads OpenAPI for DeleteGame
- **THEN** the GUID path, no required body, empty 204, and ProblemDetails 400/404/500 are documented
- **AND** other game operations use the seven-field DTO and maps remain a separate collection

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
