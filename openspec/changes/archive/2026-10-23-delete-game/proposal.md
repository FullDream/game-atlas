# Proposal

## Why

The catalog supports creating, listing, and updating games but cannot remove them. Clients need to permanently delete games that should no longer exist in the catalog.

## What Changes

- Add unauthenticated DELETE `/games/{id}` for both draft and published games, without a request body.
- Physically delete the game and return HTTP 204 with no response body after successful persistence.
- Return HTTP 404 ProblemDetails for a missing game, including repeated deletion.
- Allow a deleted game's slug to be reused and omit deleted games from list items and totals.
- Document the operation and cover persistence, error responses, and existing endpoint compatibility.
- Out of scope: soft deletion, restoration, audit history, authorization, bulk deletion, external image or map cleanup, and new concurrency/versioning mechanisms.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-catalog`: Add permanent game deletion, its HTTP contract, and its effects on browsing and slug availability.

## Impact

- API: add DeleteGame to the existing Games endpoints and reuse nongeneric OutcomeResult for HTTP 204.
- Application: add the Games/DeleteGame command, handler, and feature-owned errors using IApplicationDbContext.
- Persistence: delete the existing Games row; no schema changes or migrations are needed.
- Tests and documentation: add deletion coverage and examples. No new packages or domain state are required.
