# Proposal

## Why

Management endpoints currently allow anonymous changes to the catalog. The first deployment needs one administrator who can sign in to the admin SPA while readers continue to browse published maps.

## What Changes

- **BREAKING** Require an authenticated Identity session for management APIs and the map editor hub. Preserve anonymous public reads, media file and tile delivery, including direct URLs used by drafts.
- Bootstrap one administrator from deployment secrets and expose login, session and logout through a small same-origin cookie API.
- Add an admin-only Nx auth scope, Spartan login page, protected navigation and session-aware HTTP error handling.

## Capabilities

### New Capabilities

- `admin-auth` covers provisioning, session lifecycle, request protection and SPA login.

### Modified Capabilities

- `admin-shell` gates management navigation and exposes logout.
- `game-catalog`, `maps`, `category-groups`, `categories` and `markers` require an administrator for operations previously specified as anonymous.

## Impact

Affects ASP.NET Core Identity EF stores, the existing SQLite migration path, API startup, OpenAPI/Orval generation, administrative integration fixtures and Angular admin composition. Deployment supplies bootstrap secrets, HTTPS and persistent Data Protection keys. Registration, account management, roles, token APIs, draft previews and changes to static file visibility are outside this change.

Production mounts admin at /admin/, public web at / and the API at /api/ on one HTTPS origin.
