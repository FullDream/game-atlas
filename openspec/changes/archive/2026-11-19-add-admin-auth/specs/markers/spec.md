## MODIFIED Requirements

### Requirement: Scope marker operations to the route map

Authenticated GET/POST SHALL use /games/{gameId}/maps/{mapId}/markers and PATCH/DELETE its /{id} resource for draft or published games. Every marker SHALL belong to a category whose group belongs to that map. Missing/foreign game/map/marker scope SHALL return HTTP 404 without changes, including sibling maps of the same game. Legacy game-wide marker routes SHALL NOT remain active.

#### Scenario: No image prerequisite and strict map scope

- **WHEN** a valid marker operation targets a map without styles
- **THEN** it is permitted normally
- **AND** supplying another map's marker under this route returns 404 even within the same game

#### Scenario: Game status and missing scope

- **WHEN** an existing map belongs to a draft or published game
- **THEN** marker operations require authentication and support maps without styles
- **AND** missing or foreign game/map/marker scope returns 404 without changes
