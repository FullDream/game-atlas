# Tasks

Completed items retain verified infrastructure from the earlier map implementation. Unchecked items describe the revised multi-map/content-ownership delta; the previous passing test run does not verify that delta.

## 1. Domain model

- [x] 1.1 Keep centered geometry, power-of-two tiles, finite negative/fractional/out-of-bounds positions, and single-original style metadata/state/attempt fencing.
- [x] 1.2 Change Map.Create to accept gameId/name/width/height and create zero styles with sequence 0, available immutable geometry, and defaultStyleId=null. Validate 1..200 trimmed names and 1..16384 dimensions; test invalid and boundary inputs.
- [x] 1.3 Remove the final-style deletion guard/error. Test deleting any processing state, zero-style maps, preserved geometry/content, default recalculation, and subsequent style addition.
- [x] 1.4 Replace CategoryGroup.GameId with immutable MapId. Preserve category-group/marker-category links and existing metadata/order rules; test names/orders scoped independently per map.

## 2. Persistence and migration safety

- [x] 2.1 Retain applied Maps/SimplifyMapStyles migration history, the guarded legacy-version upgrade, directly persisted styles, durable admissions, and FK-independent cleanup intents.
- [x] 2.2 Preserve the earlier explicitly authorized developer test-map removal and backup/upgrade record; it does not authorize deleting any newly affected legacy content.
- [x] 2.3 Implement migration preflight/preparation for explicit legacy map names and group-to-map destinations. Refuse missing maps/dimensions/names, ambiguous assignments, and destination conflicts without partial changes. Document operator-supplied mapping and safe backup/restore; do not apply to persistent data until prepared.
- [x] 2.4 Generate a new EF ownership migration/snapshot: multiple Maps.GameId rows, unique game/name, required map metadata, CategoryGroups.MapId FK and map/name/order indexes, and map-to-content cascades. Do not rewrite applied migrations or change file identities.
- [x] 2.5 Test fresh upgrades, prepared nonempty ownership transfer, unresolved/ambiguous refusal, duplicate map names, same-name groups in sibling maps, cascades, model consistency, and preservation of IDs/timestamps/positions/media/files.

## 3. Map use cases and routes

- [x] 3.1 Introduce separate JSON CreateMap request/mapping/validation and a no-file handler. Return 200 MapDto with 15 fields and no styles without creating upload sessions, files, or background work. Test 400/404/409/415/422 and recognized name conflicts.
- [x] 3.2 Implement ListMaps at GET /games/{gameId}/maps as full MapDto[] ordered by createdAt/id, no query controls. Test existing empty game [], missing game 404, parent isolation, equal timestamps, and bounded queries.
- [x] 3.3 Move GetMap, style upload/update/retry/delete, map deletion, and SSE to explicit /games/{gameId}/maps/{mapId} routes. Validate both game/map and style/map boundaries, including sibling maps. Remove legacy singular routes and one-map-conflict behavior.
- [x] 3.4 Make MapUploadAdmission style-addition-only for one existing map, removing creation mode/branches. Preserve staging, durable owner registration, fresh transactional scope/name/dimension rechecks, promotion/save/completion, rollback, and original-byte guarantees.
- [x] 3.5 Remove singular mapId from GameDto and game handler/mapping/projection queries. Keep seven-field create/list/update responses and existing catalog controls. Extra mapId creation input cannot assign maps; PATCH rejects it. Test zero/several maps without catalog N+1.

## 4. Map-owned content use cases

- [x] 4.1 Move group endpoints/commands/queries under game/map scope; return six-field DTOs with mapId instead of gameId. Update uniqueness checks, database conflict classification, and serialized default-order allocation to map scope.
- [x] 4.2 Move category routes under game/map/current-group scope. Permit transfers only within the same map; preserve group-scoped uniqueness/order, nine-field DTOs, derived marker counts, atomic updates, and no-op/parent timestamp rules.
- [x] 4.3 Move marker operations under game/map scope with same-map category checks and transfers. Preserve eight-field DTOs, unfiltered deterministic lists, nested position merge, media replacement/unions, and counts.
- [x] 4.4 Test missing/foreign game/map/group/category/marker paths and same-game sibling-map access; reject cross-map transfers atomically. Verify full CRUD works before any style is uploaded and legacy game-wide routes are absent.

## 5. Native storage and processing

- [x] 5.1 Retain pinned Windows-verified native codecs/generation, single-writer processing, immutable PNG/JPEG originals, transparent WebP tiles, every-zoom geometry, private paths, and cancellation. Linux deployment-image verification remains an operational requirement.
- [x] 5.2 Retain generated staging/content/work/tile paths, actual-byte/configuration limits, link/path checks, Git ignores, atomic validation/publication, durable leases, and cleanup retry/protection.
- [x] 5.3 Retain queued/processing/ready/failed transitions, buffered progress, attempt fencing, failed-only retry, restart recovery, and viewer-independent work.
- [x] 5.4 Adapt processing/fault/recovery tests to multiple explicit maps and maps without styles. Verify a failed map/style does not block other maps and stale callbacks cannot resurrect deleted content.

## 6. Deletion and lifecycle

- [x] 6.1 Delete every scoped style, including the last, with empty 204 and durable artifact cleanup. Test concurrent distinct deletions to zero styles, repeated 404, unchanged geometry/content, and default null/reselection.
- [x] 6.2 Delete a map with its current styles/groups/categories/markers/media records in one transaction with cleanup scheduling. Test empty/styled/processing maps, repeated 404, media-reference preservation, rollback, and sibling-map/game timestamp isolation.
- [x] 6.3 Capture cleanup for every map ID before game cascades in one transaction. Test mapless games, multiple blank/processing/ready maps, failure rollback, eventual files cleanup/restart, unrelated games, and stale worker fencing.

## 7. Rendering and SSE

- [x] 7.1 Retain static style-addressed GET/HEAD/304 tiles without Mediator/SQL, immutable caches, private originals/work/manifests, eventual removal, and unchanged map/style storage IDs.
- [x] 7.2 Adapt SSE to the explicit map route and allow styles=[] snapshots/defaultStyleId=null with idle heartbeats and later style changes. Test independent maps/subscribers/reconnect, fresh consistent bounded queries, deletion closure, and no rebinding to a same-name replacement map.
- [x] 7.3 Test centered geometry available before uploads, mismatching first/later style rejection, and preservation of coordinates/content after deleting all styles or changing visible style.

## 8. OpenAPI and documentation

- [x] 8.1 Document nine map operations, JSON CreateMap, ListMaps arrays, explicit map IDs, typed single-file AddMapStyle, 15-field maps, 7-field games, and 6-field map-owned groups. Preserve category/marker DTOs and marker anyOf unions, PATCH media, errors, static tile docs, and operation ordering.
- [x] 8.2 Update resource README/HTTP examples and operational migration instructions for creating blank maps/content, uploading styles, deleting the final style, multi-map cleanup, new ownership, and the revised destructive map deletion contract. Remove obsolete singular-map/final-style-preservation assumptions.

## 9. Acceptance and verification

- [x] 9.1 Exercise one game with multiple maps: create without files, add groups/categories/markers, upload/process/retry styles, observe SSE, delete all styles while retaining content, and delete one whole map while preserving siblings.
- [x] 9.2 Run regression tests for catalogs, ordering, transfers, counts, media, progress, static tiles, publication faults, admission rollback/expiry, cleanup recovery, transactional deletion, and safe migrations under revised ownership.
- [x] 9.3 Run full solution tests, scoped formatting, UTF-8/LF, git diff checks, EF model consistency, rename staging verification, and strict OpenSpec validation. Record actual results only after implementation; do not silently migrate or reset persistent databases.

## Workflow follow-up

- The multi-map/content-ownership revision is implemented. Review the change before the explicit sync/archive step.
- Sync specifications, archive, and commit only when explicitly requested.

## Verification record

- 2026-10-24, Windows / .NET 10: full Release solution run passed 126 Domain, 100 Application, and 491 Integration tests.
- After the deletion rollback test was extended to map and game deletion with owned content and a sibling map,
  the focused map-fault/category-group regression run passed all 45 tests.
- EF CLI reports no pending model changes. Generated ownership migration preserves existing map/style identities
  and original files in disposable database tests; unresolved preparation and unsafe rollback are refused.
- Resource README/HTTP examples and the explicit ownership-preparation command are documented in English.
- Persistent developer data was not migrated, reset, or reassigned during this implementation.
