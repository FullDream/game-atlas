# Tasks

## 1. Shared media model and persistence foundation

- [x] 1.1 Add MediaFolder and immutable image/video asset domain models with separate errors, metadata update methods, shared alt, optional image preview, and UUID/UTC conventions; verify domain tests for names, nulls, dimensions, subtype rules, byte immutability, and no-op timestamps.
- [x] 1.2 Configure direct EF persistence, string discriminators, restrictive folder/preview FKs, root/sibling normalized-name indexes, and browse indexes; verify real SQLite tests for root and nested Latin/Cyrillic conflicts, duplicate file names, and forbidden cascades.
- [x] 1.3 Introduce media context access, feature registration, shared folder/image/video DTOs, and separate mappings in the existing projects; verify solution build and schema tests proving folder/image/video discrimination without duplicate fields or a folder variant in asset-only responses.

## 2. Nested folder operations

- [x] 2.1 Implement thin grouped folder POST/PATCH endpoints, Application validation, and atomic hierarchy operations; verify root/nested creation, rename, root move, missing parents, self/descendant rejection, duplicate sibling names, concurrent moves, shared PATCH media, and no-op behavior.
- [x] 2.2 Implement empty-only folder DELETE under serialized hierarchy checks; verify 204/404/409, missing-physical-file children still count, and concurrent child creation/move cannot orphan contents or bypass the guard.
- [x] 2.3 Document folder fields, normalization, manual hierarchy, moves, empty deletion, and errors in the media README/HTTP examples; verify OpenAPI lists the implemented operations in project order with nullable parent handling.

## 3. Bounded immutable storage and content inspection

- [x] 3.1 Add generated media storage/staging paths, configuration, and Git ignores while preserving map tile/work locations; verify path traversal, hostile filenames, symlink/junction rejection, byte preservation, and configuration validation through filesystem tests.
- [x] 3.2 Extend shared raster probing to static JPEG/PNG/WebP/AVIF using the pinned native runtime; verify actual loader capabilities, dimension bounds, animation rejection, misleading extensions/MIME, malformed content, and unchanged original bytes with real fixtures.
- [x] 3.3 Add bounded local MP4/WebM inspection with MediaInfo.Wrapper.Core and RID-selected MediaInfo.Core.Native 26.10.0; verify native readiness/distribution, real fixtures, cumulative read limits, cooperative cancellation and bounded concurrency. Keep readers and staging alive until native work returns, without leases; defer distribution notices until release.
- [x] 3.4 Implement XmlReader-based restricted static SVG validation with explicit namespace/element/attribute/value allowlists and safe internal references; verify unchanged accepted bytes plus adversarial script/event/foreignObject/CSS/DTD/entity/namespace/URI/processing-instruction cases all fail safely with 422.
- [x] 3.5 Enforce configurable XML size/depth/count, raster dimension, placeholder-output and video inspection budgets; retain explicit startup capability checks without a media hosted/background service.
- [x] 3.6 Generate and persist nullable read-only Base64 WebP placeholders for raster images during admission using the existing native runtime; skip SVG unconditionally and raster originals whose dimensions are both at or below the configured maximum. Verify JPEG/PNG/WebP/AVIF, threshold equality, smaller/one-dimension-larger images, SVG nulls with no renderer dependency, encoded bounds, aspect/alpha, unchanged originals, and generation-failure rollback; document the proposed 32-pixel default, shared nullable field, and image data-URI CSP needs.

## 4. Single-file admission and stable delivery

- [x] 4.1 Use generated staging files with request-level try/finally deletion; remove media admission sessions, leases, expiry, startup/periodic cleanup and recovery. Verify cancellation, validation and IO failures clean temporary files and committed assets survive response loss; document accepted crash orphans.
- [x] 4.2 Implement POST /media/assets with one streamed file, optional destination/name, and applicable alt/preview metadata; verify all supported types, root/folder placement, basename naming, duplicate names, shared 314572800-byte boundary, dishonest Content-Length, invalid fields, parent races, and 400/404/413/415/422/500 behavior.
- [x] 4.3 Implement static GET/HEAD /media/files/{id} without database access or Mediator, including map originals, with MIME, nosniff, SVG CSP, public caching, filesystem validators and byte ranges; verify GET/HEAD/206/304/404/416, zero SQL, private staging and rejected non-asset paths. Document that crash-orphaned originals can remain accessible by known URL.
- [x] 4.4 Add restrictive SVG CSP to GET/HEAD/304 delivery and safe metadata/image-context guidance; verify direct navigation restrictions and accepted gradient/clip/mask rendering in browser-level checks, and document common original access, upload limits, multipart overhead, and file examples.

## 5. Paginated mixed browsing

- [x] 5.1 Implement GET /media with immediate-root/folder scope, escaped literal name search, mixed folder-first/name/ID ordering, and the existing PaginatedResult envelope; verify pagination boundaries, equal names, mixed image/video ordering, missing folders, search locality, and current count consistency.
- [x] 5.2 Derive direct itemsCount and resolve shared image/video/preview DTOs using bounded server-side queries; verify nonrecursive search-independent counts, missing-file records, concurrent moves, and constant query count across larger pages without filesystem scans.
- [x] 5.3 Document browsing fields, default/bounded offset/limit, ASCII search semantics, type mappings, folder counts, and root selection; verify generated OpenAPI and runnable examples match the mixed collection contract.
- [x] 5.4 Include the same persisted placeholder or explicit null in every shared image response, including category icons, marker images, style sources, and selected video previews; verify upload/browse/resource/OpenAPI shapes, no generation on reads, no changes after name/alt/folder edits, read-only input rejection, and retained placeholders for manually missing originals.

## 6. Shared metadata, references, and deletion

- [x] 6.1 Implement atomic asset metadata PATCH for rename/move, shared image alt, and optional video image preview; verify subtype validation, missing/wrong-type references, root moves, null clearing, required-field rejection, shared PATCH behavior, immutable bytes/URLs, and no-op timestamps.
- [x] 6.2 Keep serialized persisted-reference checks and restrictive FKs for category icons, marker attachments, style sources and video previews. Remove durable file-use leases and active-reader tracking; verify attach/delete races remain safe and detached readers do not independently cause 409.
- [x] 6.3 Delete unused originals immediately within the request without cleanup intents/jobs. Verify referenced 409, unused 204, repeated 404, missing files, safe IO errors retaining metadata, and unrelated asset preservation; document non-atomic filesystem/DB failures.
- [x] 6.4 Document shared metadata, retained unreferenced assets, immediate deletion and missing-file behavior. Explicitly document no media startup/periodic maintenance and accepted crash temporaries/orphans and detached-reader limitations.

## 7. Category icon attachments

- [x] 7.1 Replace category icon mutation strings with iconAssetId and reference an existing image asset in create/update; verify allowed raster/SVG icons, null/malformed/missing/non-image IDs, same-map transfer atomicity, and shared-asset deletion protection using real persistence tests.
- [x] 7.2 Resolve the shared image under CategoryDto.icon without changing its nine root fields, counts, ordering, or timestamps; verify reuse across games, shared alt/name updates, no category N+1, and that category/group/map/game deletion detaches but preserves assets.
- [x] 7.3 Update category OpenAPI, README, HTTP examples, and existing tests for the breaking icon contract; verify legacy icon input is not accepted/advertised and unrelated color/name/order/parent rules still pass.

## 8. Marker asset attachments

- [x] 8.1 Replace marker-specific media subtype persistence with ordered MarkerAsset relationships and remove YouTube input/response support; verify unique marker/asset constraints, reusable assets across markers, and no duplicated URL/alt/dimension/preview data in attachments.
- [x] 8.2 Change creation/PATCH media to ordered distinct GUID arrays and reuse library image/video response DTOs; verify omitted/empty/null arrays, duplicates, malformed/legacy object inputs, missing/wrong-type IDs, ordering, shared metadata, effective no-ops, and atomic scalar/position/transfer failures.
- [x] 8.3 Preserve marker/category counts, cascade detachment, and map-scoped boundaries without deleting library records/files; verify mixed-media lifecycle, cross-map transfer rejection, timestamps, and constant query behavior with previews.
- [x] 8.4 Update marker OpenAPI unions, README, HTTP examples, validators, and existing tests; verify eight root response fields, UUID input arrays, shared image/video response mappings, and complete absence of YouTube and legacy object/URL input schemas.

## 9. Map style source integration

- [x] 9.1 Replace multipart AddMapStyle with JSON name/backgroundColor/sourceAssetId and remove map-owned upload creation/admission branches; verify scope/name conflicts, first/later dimension mismatches, supported JPEG/PNG/WebP/AVIF, SVG/video rejection, 415 for multipart, atomic queued references, and no copied originals.
- [x] 9.2 Read shared immutable sources directly from media storage during native processing/retry, retaining attempt fencing, progress and static tiles without file-use leases. Verify shared sources, expanded formats, missing-source failure, deletion and metadata edits without retiling.
- [x] 9.3 Restrict map/style/game cleanup to derived output and remove obsolete original-deletion assumptions; verify final-style/map/game deletion preserves every shared asset, correctly detaches all references, keeps sibling content, and recovers cleanup after faults/restart.
- [x] 9.4 Add shared source DTOs to style/map/SSE responses with bounded queries; update OpenAPI, README, HTTP examples, and storage documentation, then verify ten style fields, fifteen map fields, nine map operations, common original delivery, and unchanged zero-SQL static GET/HEAD/304 tiles.

## 10. Fresh database migrations

- [x] 10.1 Generate/review media/folder and consumer-reference migrations/snapshot without admission/lease/cleanup tables; remove unused new migrations for removed infrastructure, preserve applied historical migrations, and verify fresh upgrade, FKs, model consistency and disposable empty rollback.
- [x] 10.2 Remove legacy media preparation/import implementation, CLI commands (media and map ownership), manifest models/errors, obsolete map-image probing and conversion tests. Target fresh empty databases; remove unused media mapping/backfill tables from unapplied migrations and retain empty-source guards without silently discarding legacy references.
- [x] 10.3 Document fresh initialization, unsupported populated legacy upgrades, native deployment requirements and populated rollback through matching backups. Never reset or modify a persistent database without a separate request.

## 11. End-to-end integration and final verification

- [x] 11.1 Exercise one shared image across category/marker/style and a video preview across games/folders: upload, browse/search/page, move/rename/change alt, process/retry/SSE, reject in-use deletion, detach resources, and explicitly delete; verify unrelated bytes/content and missing-file metadata retention throughout.
- [x] 11.2 Run full Release tests and relevant security/browser/native checks for resource regressions, query bounds, MIME/CSP/ranges, ordinary staging/deletion errors and capability observations. Verify no media recovery/maintenance service or coordination tables remain; do not claim unrun deployment checks.
- [x] 11.3 Run scoped formatting, UTF-8/LF, git diff/rename checks, EF pending-model validation, and strict OpenSpec validation; verify only authorized implementation files changed and record actual results before completing tasks.

## Workflow follow-up

- Review this proposal and its breaking contracts before starting implementation through openspec-apply-change media-library.
- Sync main specifications, archive, and commit only when explicitly requested.
