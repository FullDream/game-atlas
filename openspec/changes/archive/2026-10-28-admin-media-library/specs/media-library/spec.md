# Spec Delta

## MODIFIED Requirements

### Requirement: Browse one folder through a paginated mixed list

GET /media SHALL accept folderId, offset, limit, and search. Omitted folderId SHALL select root. The 200 response SHALL use PaginatedResult<MediaItemDto> containing only items, limit, offset, and total. Defaults SHALL be offset=0 and limit=24, with offset>=0 and limit=1..120. Items SHALL include only direct child folders and assets, ordered folders first, then all files together, then name and id ascending within each group.

#### Scenario: Page across folders and files

- **WHEN** root contains folders, images, and videos spanning several pages
- **THEN** pagination is applied to the single combined ordered collection and total counts all matching direct children
- **AND** image/video type does not introduce another sort group and equal names are resolved by ID

#### Scenario: Empty missing and invalid queries

- **WHEN** an existing folder has no matching contents or offset exceeds total
- **THEN** 200 returns items=[] with the effective pagination and actual total
- **AND** a missing folder returns 404, invalid ranges 422, and malformed query types 400

#### Scenario: Accept the increased maximum

- **WHEN** limit=120 is supplied
- **THEN** the list returns at most 120 items
- **AND** limit=121 returns 422

## ADDED Requirements

### Requirement: Retrieve folder context independently

GET /media/folders/{id} SHALL return 200 with folder and ancestors. Folder SHALL contain the complete selected folder representation. Ancestors SHALL contain id and name, ordered from the topmost parent to the nearest parent, excluding the selected folder and synthetic root. The context SHALL be independent of list search and pagination.

#### Scenario: Open a nested folder directly

- **WHEN** a consumer requests GET /media/folders/{id} for a nested folder
- **THEN** folder contains its metadata and direct itemsCount
- **AND** ancestors contains the complete ordered parent chain

#### Scenario: Browse root

- **WHEN** the consumer browses the synthetic root
- **THEN** GET /media omits folderId and returns ordinary pagination
- **AND** the frontend uses an empty breadcrumb chain without requesting a synthetic folder

#### Scenario: Reject a missing folder

- **WHEN** the folder detail or listing request identifies a nonexistent folder
- **THEN** the endpoint returns 404

#### Scenario: Separate OpenAPI contracts

- **WHEN** consumers inspect OpenAPI
- **THEN** GET /media exposes the shared generic pagination model without folder or ancestors
- **AND** GET /media/folders/{id} exposes MediaFolderDetailsDto with folder and ancestors
