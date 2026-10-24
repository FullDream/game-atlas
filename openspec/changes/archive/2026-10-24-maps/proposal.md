# Proposal

## Why

GameAtlas needs several independent coordinate spaces per game. A map must exist before any image is uploaded so its groups, categories, and markers can be edited on a blank viewer; image styles are optional visualizations of that map.

## What Changes

- **BREAKING**: a game owns zero or more maps. Create a map with JSON name, width, and height, without uploading a file. Map names are unique within their game ignoring case; dimensions are immutable.
- **BREAKING**: move metadata routes to /games/{gameId}/maps and /games/{gameId}/maps/{mapId}. List maps as a complete deterministic array and keep individual map reads.
- **BREAKING**: each map owns its category groups. Categories remain group-owned and markers category-owned. Scope content routes and transfers to the specific map, including isolation between maps of the same game.
- **BREAKING**: remove mapId from game responses. Retrieve maps through their collection endpoint rather than embedding a selected map or map collection in GameDto.
- Allow zero styles, including deletion of the final style. Preserve map dimensions, groups, categories, markers, and their coordinates when styles are removed. Deleting a map removes its own complete content hierarchy; deleting a game removes all its maps.
- Add each style separately using typed multipart name/backgroundColor/file fields. Each style owns one immutable original matching the map dimensions, with durable queued processing, failed-only retry, and per-map SSE progress.
- Preserve centered coordinates, original PNG/JPEG bytes, sequential native tiling, static WebP delivery without per-tile database queries, durable recovery/cleanup, and existing marker media unions.
- Migrate existing ownership without inventing map dimensions or deleting content. Preserve IDs, scalar metadata, coordinates, media, and stored-file paths; refuse ambiguous or incomplete legacy assignments until explicitly prepared.

## Capabilities

### New Capabilities

- maps: Multiple named game-owned maps with immutable geometry, optional styles, image admission, recoverable tiling, static rendering, SSE, and complete lifecycle ownership.

### Modified Capabilities

- game-catalog: Remove singular mapId from game representations and delete every owned map and its artifacts with the game.
- category-groups: Own groups by map, scope uniqueness/order and CRUD to that map, and return mapId instead of gameId.
- categories: Scope routes and group transfers to one map; cascade map ownership without changing category DTOs or validation.
- markers: Scope marker operations and category transfers to one map, allow editing without styles, and cascade map deletion while retaining media and coordinate semantics.

## Impact

- Api/Application/Domain: JSON CreateMap, ListMaps, explicit map routes, revised DTOs and parent checks, map-owned content, and removal of singular game-map projections and final-style guards.
- Infrastructure: EF relationships/indexes and a new explicit ownership migration; preserve the existing storage layout, processing, admission leases, and cleanup protections.
- Contracts/tests/docs: Breaking routes and game/group DTOs, cross-map isolation, empty maps, deletion behavior, migration safety, native OpenAPI, HTTP examples, and regression coverage.
- Out of scope: frontend implementation, map metadata editing, map resizing, map ordering or default-map selection, cross-map content transfers/sharing, image replacement/versioning, general media uploads, object storage, distributed workers, authentication, and PostgreSQL.
