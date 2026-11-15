# public-games Specification

## Purpose

Provide independent published game browsing and slug-based game details for public consumers without exposing content management representations.

## Requirements

### Requirement: Browse published game cards

Unauthenticated GET /public/games SHALL return items, offset, limit and total. Items SHALL contain id, name, slug, cover, nullable banner, mapsCount, markersCount and categoriesCount, without status, descriptions, timestamps or embedded maps. Cover and banner SHALL use canonical ImageAsset representations. Publication and search filters SHALL precede total calculation and pagination.

#### Scenario: Mixed publication states

- **WHEN** draft and published games match a request
- **THEN** only published games appear and contribute to total
- **AND** offset beyond total returns an empty items array with the correct total

### Requirement: Search sort and validate the public catalog

The public list SHALL use offset 0 and limit 20 by default, with offset at least 0 and limit 1..100. It SHALL support trimmed literal name substring search with ASCII case-insensitivity and name/createdAt/updatedAt sorting in asc/desc directions. Default order SHALL be createdAt ascending with id ascending breaking all ties. Direction without sortBy and invalid ranges SHALL return 422; binding failures SHALL return 400 ProblemDetails.

#### Scenario: Search and sort before paging

- **WHEN** search contains literal percent or underscore and sortBy=name with no direction
- **THEN** matching uses those characters literally and ordering is name ascending then id ascending before paging
- **AND** blank search applies no filter, enum names are case-insensitive, and undefined numeric enum values return 422

### Requirement: Resolve published game details by slug

Unauthenticated GET /public/games/{gameSlug} SHALL return id, name, slug, cover, nullable banner, releaseDate, developer, description, mapsCount, markersCount, categoriesCount and maps. Maps SHALL be summaries with id, name, slug and nullable canonical ImageAsset preview, ordered by createdAt then id ascending. Missing, deleted or draft games SHALL return 404 ProblemDetails. Reads SHALL NOT change stored data or timestamps.

#### Scenario: Published game with mixed maps

- **WHEN** a published game has draft and published maps
- **THEN** only published maps appear and mapsCount equals the returned summary count
- **AND** previews use the selected ready default style source, or null when none exists

#### Scenario: Empty published game

- **WHEN** the game exists and is published but has no published maps
- **THEN** details return 200 with maps=[] and zero content counts

### Requirement: Count only publicly visible content

Public counts SHALL include maps only under published games, categories only under published maps and groups with published category status, and markers only under that complete published hierarchy with published marker status. Hidden descendants SHALL NOT contribute to counts.

#### Scenario: Hidden parent suppresses descendants

- **WHEN** a published marker belongs to a draft category, draft group or draft map
- **THEN** it contributes to no public marker count
- **AND** published categories under hidden parents contribute to no public category count

### Requirement: Publish independent public game operations

OpenAPI SHALL expose ListPublicGames and GetPublicGame under the exact tag Public Games with dedicated public schemas. DTO suffix normalization and canonical media schema reuse SHALL apply. Existing game read and mutation contracts SHALL remain unchanged.

#### Scenario: Discover additive contracts

- **WHEN** a client inspects OpenAPI
- **THEN** public operations have unique operation IDs and public response schemas
- **AND** existing GET /games and GET /games/{id} still include drafts using their existing representations
