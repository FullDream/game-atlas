## MODIFIED Requirements

### Requirement: Run administration independently

The frontend SHALL provide a separately runnable and buildable `admin` application. It SHALL run as a browser SPA without a production rendering server. Starting or building administration SHALL NOT require building or serving the public website or starting the backend merely to render its shell.

#### Scenario: Open administration without the backend

- **WHEN** an operator starts administration while the backend is unavailable
- **THEN** the sign-in page shows a connection error and lets the operator try again
- **AND** the public website is not a runtime dependency

#### Scenario: Serve a built application

- **WHEN** the production output is served with the documented SPA fallback
- **THEN** direct navigation and refresh on supported administration routes render the application
- **AND** no production Node.js rendering process is required

### Requirement: Navigate foundation destinations

Administration SHALL expose Games and Media destinations within one persistent shell. The root route SHALL lead to Games. Navigation SHALL indicate the active destination, update the page heading and breadcrumbs, and support browser back/forward. Games SHALL provide list, creation and game editing pages; Media SHALL provide the implemented media manager.

#### Scenario: Navigate between resource areas

- **WHEN** an operator selects Media from Games and then uses browser Back
- **THEN** the matching destination, heading and active navigation item change together
- **AND** the shell remains available throughout navigation, subject to confirmed unsaved-change protection

#### Scenario: Reach a foundation page directly

- **WHEN** a signed-in operator opens /games or /media directly
- **THEN** the Games list or Media manager renders inside the same shell
- **AND** a visit to / resolves to Games

#### Scenario: Open a game editor

- **WHEN** a signed-in operator opens /games/new or /games/{id}
- **THEN** the matching editor renders within the persistent shell with Games active and appropriate breadcrumbs

### Requirement: Keep the initial shell limited to available capabilities

The shell SHALL provide shared styling and navigational structure for authenticated administrators with an accessible logout action. Resource mutations SHALL be exposed only through implemented features such as the Media manager. It SHALL NOT display invented user identity, business statistics or functional links to absent features.

#### Scenario: Inspect initial navigation

- **WHEN** a signed-in operator opens administration
- **THEN** Games and Media destinations are present, with functional media management
- **AND** logout is available and no fake profile, dashboard statistics or nonfunctional user-management link is presented

#### Scenario: Confirm sign-out

- **WHEN** the administrator selects the logout action
- **THEN** the shell asks for confirmation before submitting the logout request
- **AND** successful logout or an expired session takes the administrator to the sign-in page
- **AND** a failed logout displays an error and allows another attempt

## ADDED Requirements

### Requirement: Host administration at a dedicated production path

One HTTPS origin SHALL serve public web at /, admin at /admin/, and API at /api/. Admin deep links SHALL resolve within the SPA.

#### Scenario: Restore an editor in production

- **WHEN** a guest opens /admin/games/new and signs in
- **THEN** the application returns to /admin/games/new while /games/new continues to resolve through public web
