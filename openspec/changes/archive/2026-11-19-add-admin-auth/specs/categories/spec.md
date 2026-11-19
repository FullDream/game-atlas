## MODIFIED Requirements

### Requirement: Scope category operations to a game map and current group

Category operations SHALL use /games/{gameId}/maps/{mapId}/category-groups/{groupId}/categories with authentication for draft or published games. Each category SHALL belong to one group of that map. Missing or foreign game/map/group/category scope SHALL return HTTP 404 without changes. Maps with zero styles SHALL support all category operations. Legacy game-wide category routes SHALL NOT remain active.

#### Scenario: Validate every ownership boundary

- **WHEN** an otherwise valid operation targets a group outside the route map or a category outside the route group
- **THEN** HTTP 404 is returned even if all resources belong to the same game
- **AND** a valid operation on a map without styles succeeds normally

#### Scenario: Existing parent in either game status

- **WHEN** categories are managed in an existing group of the route map, whose game is draft or published
- **THEN** the operation is permitted for the administrator without requiring styles

#### Scenario: Missing or foreign resource

- **WHEN** an otherwise valid request targets a missing game, missing group, group of another map, missing category, or category outside the route group
- **THEN** it returns HTTP 404 and changes no storage
