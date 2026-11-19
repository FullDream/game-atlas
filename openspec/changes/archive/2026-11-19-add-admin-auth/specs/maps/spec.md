## MODIFIED Requirements

### Requirement: Scope multiple maps and their styles to a game

A game SHALL own zero or more independent maps. Map operations SHALL use /games/{gameId}/maps and /games/{gameId}/maps/{mapId}, with authentication for draft or published games. Styles SHALL belong to exactly one map. Valid requests for missing parents or resources outside the route game/map SHALL return HTTP 404 without changes. The former singular map routes SHALL NOT remain active aliases.

#### Scenario: Independent maps in one game

- **WHEN** a game already has a map and a valid request creates another with a different normalized name
- **THEN** both maps exist with independent identities, dimensions, styles, and content

#### Scenario: Reject foreign resources even within one game

- **WHEN** a route identifies a map of another game or a style of another map, including a sibling map of the same game
- **THEN** HTTP 404 preserves all resources and files

### Requirement: Publish a map and all owned draft content atomically

Authenticated POST /games/{gameId}/maps/{mapId}/publish SHALL atomically transition the scoped map and all of its owned draft category groups, categories, and markers to `published`, and return HTTP 200 with the complete map representation. Only records whose status changes from `draft` to `published` SHALL update `updatedAt`. Missing or foreign game/map routes SHALL return HTTP 404 without changes.

#### Scenario: Publish a draft map with mixed draft and published content

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a draft map owning draft and published groups, categories, and markers
- **THEN** the map and every owned group, category, and marker persist with status `published`
- **AND** `updatedAt` advances only on the map and owned entities that were previously `draft`, while already-published entities retain their prior `updatedAt`

#### Scenario: Publish changes on an already published map

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a published map that contains at least one draft group, category, or marker
- **THEN** the draft child entities transition to `published` with a new `updatedAt` while the map itself retains its existing `updatedAt`

#### Scenario: Idempotent publish when all content is already published

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish is called for a published map whose groups, categories, and markers are all already `published`
- **THEN** HTTP 200 returns the complete map and no `updatedAt` timestamp changes on the map or any child entity

#### Scenario: Missing or foreign map on publish

- **WHEN** POST /games/{gameId}/maps/{mapId}/publish targets a nonexistent game, nonexistent map, or a map belonging to another game
- **THEN** HTTP 404 ProblemDetails is returned without modifying any map or content
