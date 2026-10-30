## MODIFIED Requirements

### Requirement: Navigate foundation destinations

Administration SHALL expose Games and Media destinations within one persistent shell. The root route SHALL lead to Games. Navigation SHALL indicate the active destination, update the page heading and breadcrumbs, and support browser back/forward. Games SHALL provide list, creation and game editing pages; Media SHALL provide the implemented media manager.

#### Scenario: Navigate between resource areas

- **WHEN** an operator selects Media from Games and then uses browser Back
- **THEN** the matching destination, heading and active navigation item change together
- **AND** the shell remains available throughout navigation, subject to confirmed unsaved-change protection

#### Scenario: Reach a foundation page directly

- **WHEN** an operator opens /games or /media directly
- **THEN** the Games list or Media manager renders inside the same shell
- **AND** a visit to / resolves to Games

#### Scenario: Open a game editor

- **WHEN** an operator opens /games/new or /games/{id}
- **THEN** the matching editor renders within the persistent shell with Games active and appropriate breadcrumbs
