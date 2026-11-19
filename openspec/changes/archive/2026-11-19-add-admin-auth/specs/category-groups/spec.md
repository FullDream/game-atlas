## MODIFIED Requirements

### Requirement: Scope category groups to an existing map

Group operations SHALL use /games/{gameId}/maps/{mapId}/category-groups with authentication for draft or published games. Each group SHALL belong to exactly one map through immutable mapId. Valid requests for a missing game/map/group, a map outside the game, or a group outside the map SHALL return HTTP 404. A map SHALL NOT need any style before group operations.

#### Scenario: Empty map and isolated ownership

- **WHEN** a client creates or lists groups on an existing map with no styles
- **THEN** normal group behavior applies
- **AND** a group from another map returns 404, including a sibling map of the same game

#### Scenario: Manage either game status

- **WHEN** an existing map belongs to a draft or published game
- **THEN** the group operation is permitted for the administrator without a style prerequisite

#### Scenario: Missing parent

- **WHEN** an otherwise valid create, list, update, or delete request targets a nonexistent map
- **THEN** the response is HTTP 404 ProblemDetails rather than a successful empty list or an orphaned group

#### Scenario: Missing or foreign group

- **WHEN** an otherwise valid rename or delete request identifies a missing group or a group belonging to another map
- **THEN** the response is HTTP 404 ProblemDetails and no group's metadata changes
