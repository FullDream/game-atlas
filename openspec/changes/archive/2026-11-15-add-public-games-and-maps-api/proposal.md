# Proposal

## Why

Existing game and map reads serve content management and include drafts and operational metadata. Public browsing needs independent read contracts and enforced publication visibility, while future administrator preview should reuse detailed map assembly.

## What Changes

- Add `GET /public/games`, `GET /public/games/{gameSlug}`, and `GET /public/games/{gameSlug}/maps/{mapSlug}` with dedicated public DTOs.
- Return compact game cards, game details with map summaries, and complete map details including ready styles, ordered category trees and a flat marker collection.
- Enforce publication visibility across games, maps, groups, categories and markers, including counts and pagination totals.
- Add an internal `PublishedOnly` / `IncludeDrafts` visibility mode to reusable detailed map retrieval for future administrator preview. Public HTTP operations always use `PublishedOnly`.
- Place public routes in Games/PublicEndpoints.cs and Maps/PublicEndpoints.cs with `Public Games` and `Public Maps` OpenAPI tags and unique operation IDs.

### Out of Scope

- All frontend changes, API client regeneration and switching web consumers.
- Changes to `add-web-home-catalog` or existing administrative routes and their visibility.
- Preview HTTP endpoints, administrator authentication/authorization and preview UI; these require a later change together.
- Marker pagination, viewport loading, new publication transitions, storage changes or file-access protection.

## Capabilities

### New Capabilities

- `public-games`: Published game browsing, slug-based game details, compact contracts and visibility-aware content counts.
- `public-maps`: Slug-based complete public map reads and reusable draft-aware assembly for future preview.

### Modified Capabilities

None. Existing game-catalog, maps and map-viewer contracts remain unchanged; these are additive HTTP reads.

## Impact

- Backend Api and Application feature folders for games, maps, categories and markers; existing persistence abstractions and canonical media contracts are reused.
- New OpenAPI operations and schemas, backend integration/contract tests and API documentation.
- No new runtime dependency, database migration or frontend modification is expected.
