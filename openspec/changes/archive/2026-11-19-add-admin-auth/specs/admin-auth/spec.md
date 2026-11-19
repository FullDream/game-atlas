## Purpose

Give the administrator secure access to GameAtlas management tools while preserving anonymous access for map readers.

## ADDED Requirements

### Requirement: Provision the first administrator

The API SHALL create one administrator from deployment secrets when its migrated user store is empty. It SHALL persist only a password hash, never log credentials and fail closed if initial credentials are missing or invalid.

#### Scenario: Restart with a provisioned account

- **WHEN** the API restarts after provisioning with missing or changed bootstrap credentials
- **THEN** the account and password remain usable and no additional account is created

### Requirement: Manage an administrator session

POST /auth/login SHALL accept login and password and issue a nonpersistent Secure, HttpOnly, host-only SameSite=Strict cookie on success with HTTP 204. Sessions SHALL expire after eight hours without sliding renewal. Invalid credentials and locked accounts SHALL yield the same HTTP 401 Problem Details response. Failed password attempts SHALL count toward Identity lockout. Requests beyond the login rate limit SHALL receive HTTP 429 with Problem Details.

#### Scenario: Read and end a session

- **WHEN** a signed-in administrator calls GET /auth/session
- **THEN** the response contains the login
- **AND** after a successful POST /auth/logout returns 204 the browser's next session check returns 401

### Requirement: Protect administrative requests

Management routes, including media library operations, and the editor hub SHALL explicitly require authentication. Anonymous public game and map reads SHALL retain published-only visibility. Direct media and tile URLs SHALL remain public regardless of publication status. State-changing management requests including login and logout SHALL require antiforgery validation.

#### Scenario: Guest requests management data

- **WHEN** a guest requests a management resource or negotiates an editor hub connection
- **THEN** the API returns 401 without redirecting to HTML

#### Scenario: Missing or invalid antiforgery proof

- **WHEN** a client submits a login or an authenticated management mutation without valid antiforgery proof
- **THEN** the server returns HTTP 400 with Problem Details without changing the session or resource

#### Scenario: Connect to the editor hub

- **WHEN** an authenticated browser negotiates or connects to the editor hub
- **THEN** a supplied `Origin` must match the API origin or an explicitly allowed development origin, otherwise the API returns HTTP 403
- **AND** the connection closes when authentication expires

### Requirement: Handle login and expiry in admin

The admin SPA SHALL offer an accessible login form outside the shell. It SHALL restore a validated Games or Media destination after login, falling back to Games and clear private cached data on logout or session expiry. Administrative queries SHALL stop retrying immediately on 401 or 403.

#### Scenario: Session expires during editing

- **WHEN** management requests concurrently return 401
- **THEN** the SPA sends the operator to login once with the original destination

#### Scenario: Invalid credentials or forbidden operation

- **WHEN** credentials are rejected on the login page
- **THEN** the page shows a generic error without a redirect loop or automatic retry
- **AND** a 403 on an authenticated route is handled there without a login redirect
