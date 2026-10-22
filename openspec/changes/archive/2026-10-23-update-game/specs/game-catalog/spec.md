# Spec Delta

## ADDED Requirements

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
