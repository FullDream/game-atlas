# Design

## Context

See proposal.md for the revised capability. The current unarchived implementation creates one styled map per game, derives GameDto.mapId, and owns content through CategoryGroup.GameId. The agreed model instead gives each game several independent coordinate spaces, each editable without an uploaded image. The existing processing/storage pipeline remains useful and must retain its recovery guarantees.

## Goals / Non-Goals

Goals are explicit map identity, immutable geometry before image upload, independent content ownership, and safe migration. Keep the four existing layers, feature folders, Result/FV/OutcomeResult integration, direct IApplicationDbContext access, SQLite, explicit EF CLI migrations, and native OpenAPI metadata.

Do not introduce shared categories, cross-map transfers, a giant map aggregate containing every marker, repositories, new infrastructure projects, generic file libraries, or a selected/default map. Map metadata editing and resizing are outside this revision. Frontend rendering is not implemented here.

## Decisions

### 1. Map is an independent coordinate space

Game owns 0..N Maps. Each Map stores UUID v7 identity, GameId, Name/NormalizedName, immutable Width/Height, UTC timestamps, and its existing style creation sequence. Use 1..200 trimmed characters and invariant case normalization for map names, with a unique index on GameId/NormalizedName. Map.Create validates name and dimensions directly and creates zero styles; LastCreationSequence starts at zero. Geometry is available immediately, including after EF materialization.

Keep MapStyle inside the existing map/style aggregate. Remove the last-style deletion guard and its obsolete error. The default remains the earliest surviving ready style, or null when none is ready. Deleting all styles keeps the same map geometry and content; the first subsequent image must still match it. Multiple maps represent different coordinate spaces, whereas styles remain prealigned images of a single space.

Explicit creation dimensions avoid a partially defined coordinate system and allow a blank OpenLayers viewer. A frontend can read image dimensions before issuing JSON creation followed by style upload. These are separate requests: if upload fails, the empty map remains and the client can retry or delete it.

### 2. Own content through the map

Replace CategoryGroup.GameId with immutable MapId. Keep Category.CategoryGroupId and Marker.CategoryId; do not add redundant MapId fields to either. Map/game membership checks traverse this ownership chain. Map groups and markers remain independently queried and mutated aggregates; map reads do not load their entire hierarchy.

Group name uniqueness and default-order allocation are scoped to MapId. Category names/order stay group-scoped, and marker count remains derived from the category's actual marker records. Categories can move only between groups of one map; markers can move only between categories of one map. A sibling map in the same game is foreign scope and returns 404. Group membership cannot be changed through PATCH. Content mutations preserve parent game/map/group/category timestamps under the existing rules.

### 3. Explicit HTTP identity and compact representations

Base collection: /games/{gameId}/maps. Specific map: /games/{gameId}/maps/{mapId}.

| Method | Path relative to collection | Operation | Success |
| --- | --- | --- | --- |
| GET | empty | ListMaps | 200 MapDto[] |
| GET | /{mapId} | GetMap | 200 MapDto |
| GET | /{mapId}/events | StreamMapProgress | text/event-stream |
| POST | empty | CreateMap | 200 MapDto |
| POST | /{mapId}/styles | AddMapStyle | 200 MapDto |
| POST | /{mapId}/styles/{styleId}/retry | RetryMapStyle | 200 MapStyleDto |
| PATCH | /{mapId}/styles/{styleId} | UpdateMapStyle | 200 MapStyleDto |
| DELETE | /{mapId}/styles/{styleId} | DeleteMapStyle | empty 204 |
| DELETE | /{mapId} | DeleteMap | empty 204 |

CreateMap uses application/json with required name, width, and height and no upload session/file/processing. Invalid required values return 422; malformed JSON and incompatible types return 400; unsupported media return 415. Dimensions are integers in 1..16384. Duplicate normalized names within one game return 409, including specifically recognized database conflicts.

ListMaps returns full MapDto objects ordered by CreatedAt then Id, without pagination/search/sorting controls. Missing game is 404 and an existing game without maps returns []. GetMap and style operations require both route parents. Use existing consistent read transactions/split queries for map/styles snapshots, with no N+1 per listed map.

MapDto adds name to its existing fields (15 total). GameDto removes mapId (7 total), with no inline maps or map count. CategoryGroupDto replaces gameId with mapId (6 total). CategoryDto and MarkerDto remain unchanged (9 and 8 fields). Remove the old singular-map projection helpers and reads from game handlers; catalog queries must not need map loading.

Content paths become:

- /games/{gameId}/maps/{mapId}/category-groups and /{id}
- /games/{gameId}/maps/{mapId}/category-groups/{groupId}/categories and /{id}
- /games/{gameId}/maps/{mapId}/markers and /{id}

Retain existing operation IDs and GET/POST/PATCH/DELETE resource ordering. Remove former singular-map and game-wide content routes rather than selecting an arbitrary map. Every path boundary is validated, including sibling-map access. Existing PATCH Optional/RFC 7396 semantics, error classes, ProblemDetails, and read-only input rules remain.

### 4. Style admission and processing stay independent

Only AddMapStyle uses the typed flat multipart form name/backgroundColor/file, streaming exactly one immutable PNG/JPEG original. MapUploadRequest and shared FV schema mapping remain useful for this endpoint; JSON CreateMap gets a separate request/mapping/validator. Remove creation mode and its branches from MapUploadAdmission: it now operates only on an existing route map and one staged original.

Check dimensions/name/scope, register the planned style owner durably outside the admission write transaction, then reload the specific map and recheck before promotion/save/lease completion. Deleted/recreated maps must not be confused merely because their names match. Keep original bytes, 200 MiB/file limits, actual-byte counting, metadata/envelope bounds, admission leases, and durable cleanup.

Preserve queued/processing/ready/failed state, fresh attempt fencing, failed-only retry, restart recovery, one native writer, buffered 0..99 progress, complete output validation/cancellation, atomic publication, and ready-only 100. Workers operate by immutable map/style IDs and must not assume one map per game or nonempty style collections.

### 5. Rendering and SSE

Geometry remains centered original-pixel coordinates, X right/Y up, floating half extents, a top-left tile origin, and powers-of-two resolutions ending at 1. Finite negative/fractional/out-of-bounds marker coordinates remain allowed even on a blank map. Styles must exactly match map dimensions; there is no automatic alignment or resizing.

Preserve pinned native generation, 512-pixel lossless transparent WebP tiles, and the same storage layout: staging/session/file, content/map/style/original, work/map/style/attempt, tiles/map/style/z/x/y.webp. No image replacement or version history is introduced.

Static /map-tiles/{mapId:N}/{styleId:N}/{z}/{x}/{y}.webp still performs no Mediator/SQL lookup and uses immutable caching plus validators. Only generated WebP is public. Eventual removal and surviving browser caches remain accepted.

A per-map SSE snapshot can contain styles=[] and defaultStyleId=null. Empty maps still emit idle heartbeats and later style changes. Pin the explicitly requested map ID; deletion emits mapDeleted and closes even if another map of the same game or name exists. Reconnect reads only that map's current state. Viewer connections do not own background work.

### 6. Deletion follows current ownership

Style deletion removes only its style and schedules owned artifacts; it succeeds for the final queued/processing/ready/failed style. Map identity, dimensions, groups, categories, markers, media, and coordinates survive style deletion. Concurrent removal of all styles is valid.

Map deletion now removes its groups/categories/markers/media records as well as styles. Game deletion removes all its maps and their hierarchies. Capture a durable cleanup intent for every affected map ID in the same transaction as metadata deletion, before cascading rows disappear. Cleanup intents remain independent of FKs, serial with native writes, and protected against referenced/live owners.

Cascade only records owned by the deleted parent. Marker image/video URLs are external references under the current feature and are not filesystem cleanup targets. Unrelated maps, games, timestamps, counts, media, and paths must remain unchanged. A failed database commit rolls back both cascades and cleanup scheduling; late workers cannot resurrect deleted state.

## Migration Plan

Do not rewrite applied Maps or SimplifyMapStyles migrations. Generate a new ownership migration through EF CLI. Replace the unique Maps.GameId index with nonunique game lookup and unique GameId/NormalizedName. Add required map name metadata. Replace CategoryGroups.GameId/FK with MapId/FK and move the normalized-name/order indexes to map scope. Preserve category/marker relationships and use Map -> Groups -> Categories -> Markers -> Media cascades.

Preflight legacy data before dropping columns/indexes. Existing single-map games can assign their groups to that preserved map ID after a valid map name is explicitly supplied. Existing groups without a map need an explicitly prepared destination with chosen name/width/height; do not guess dimensions or names, drop groups, or infer coordinates from marker bounds. Ambiguous destinations or conflicting names must stop the transactional migration unchanged. The exact developer-data mapping is an operator input to resolve before applying the migration, not an implementation default.

Keep existing map/style IDs and geometry so original/tile/work paths remain valid. Preserve all group/category/marker/media IDs, scalar values, order, positions, counts, and timestamps while changing ownership. Fresh/disposable databases must upgrade through the complete migration chain. Retain the historical guarded legacy-version transition without bypassing it.

Persistent database changes are not part of this planning update. Before an explicit deployment, stop writers and back up SQLite and storage together, prepare/verify the mapping, apply EF CLI migration, and verify all ownership/foreign keys and preserved rows. Rollback restores the matching backup; do not flatten multiple maps or merge formerly independent group names to force the old schema. Database migration must never silently delete files.

## Risks / Trade-offs

- Breaking paths/DTOs -> update OpenAPI, HTTP examples, callers, all affected tests, and the five capability deltas together; do not maintain an ambiguous legacy alias.
- Incorrect same-game/same-map assumptions -> validate every parent boundary and test sibling-map isolation, transfers, counts, and deletion.
- Empty map and failed first upload -> preserve the valid coordinate space, provide GET/list, and allow retrying upload or explicit map deletion.
- Destructive content cascades differ from the old image-only map -> document deletion of the complete map hierarchy and test transaction rollback and unrelated-map preservation.
- Missing legacy geometry/ownership -> refuse the migration until explicit destination metadata is prepared; keep data and files intact.

## Validation

Run domain/application tests and real SQLite/filesystem/native/API tests for multiple maps, blank maps, map-scoped content and transfers, immutable dimensions, first/later styles, final-style deletion, name conflicts, 7/6/9/8/15-field DTOs, listing without N+1, independent SSE, every-map game cleanup, migration refusal/preservation, and existing rendering/media behavior. Finish with full solution tests, scoped formatting, UTF-8/LF, git diff checks, EF model consistency, and strict OpenSpec validation. Archive/sync/commit only when explicitly requested.
