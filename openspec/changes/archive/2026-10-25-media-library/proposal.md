# Proposal

## Why

Map originals currently belong to individual styles, while category icons and marker media are unmanaged references with duplicated metadata. A shared media library will let users organize and reuse uploaded files, attach them consistently, and prevent deletion while they are in use.

## What Changes

- Introduce a game-independent library of immutable image and video assets with manually managed nested folders, stable identities/URLs, shared image alt text, and optional image previews for uploaded videos.
- Generate a small read-only Base64 WebP placeholder for raster images exceeding its configured dimensions, return null for SVG and raster originals at or below that size, and reuse it in library and resource responses without changing original bytes.
- Browse immediate folder contents through one paginated, searchable endpoint returning folder/image/video variants. Sort folders first, then assets, by name and ID; show direct child counts on folders.
- Upload one file per request. Accept JPEG, PNG, WebP, AVIF, static SVG, MP4, and WebM under one configurable 300 MiB limit. Retain successful unreferenced uploads; clean request staging through try/finally without automatic expiry or recovery.
- Validate supported content and reject unsafe SVG rather than rewriting it. Apply safe file headers, constrained SVG CSP, and text-only metadata presentation guidance.
- Deliver originals through the same static file route without database access or Mediator, including map originals. Crash-orphaned originals may remain accessible by known URL. Missing physical files leave library records and relationships intact.
- Support folder/asset rename and move operations, asset alt/preview updates, empty-folder deletion, and deletion of unused assets. Referenced assets return 409; resource deletion only removes attachments.
- **BREAKING**: Marker creation/PATCH accept ordered asset IDs instead of media objects. Responses reuse library image/video DTOs; remove YouTube and marker-specific image/video metadata models.
- **BREAKING**: Categories attach an image by iconAssetId instead of an arbitrary icon string. Styles attach an existing raster image by sourceAssetId through JSON instead of uploading multipart files.
- Extend map originals to static JPEG/PNG/WebP/AVIF with exact map dimensions. Keep map geometry, style processing, retry, SSE, static WebP tiles, and derived-output cleanup.
- Target fresh empty databases; do not implement legacy media import or conversion. Preserve historical migrations and refuse unresolved legacy references rather than silently discarding data.

Temporary or orphaned files may remain after a process crash. Automatic startup scans, recovery, expiry, and deferred media deletion are explicitly out of scope.

## Capabilities

### New Capabilities

- `media-library`: Shared assets, nested folders, single-file admission, paginated browsing, immutable file delivery, SVG safety, reference-aware deletion, and request-scoped staging cleanup.

### Modified Capabilities

- `categories`: Attach library image assets as icons and resolve their shared metadata while retaining map/group scope, counts, ordering, color, and atomic updates.
- `markers`: Replace independent media objects and YouTube references with ordered library-asset attachments and shared response representations.
- `maps`: Attach shared image originals, expand raster formats, deliver originals through the library, and clean only style-owned derived artifacts when maps/styles/games are removed.

## Impact

- Backend changes span the existing Domain, Application, Api, and Infrastructure projects; no new module project, repository layer, service, or storage provider.
- Affected persistence includes new media/folder records, category icon references, marker attachment rows, and style source references. Applied migrations remain immutable; migration application is explicit.
- Reuse the existing Result/OutcomeResult, validation behavior, Optional merge-patch handling, PaginatedResult, System.Text.Json polymorphism, EF Core, and native image processor. Use request-scoped staging with try/finally and immediate physical asset deletion, without media sessions, leases, cleanup records, or background maintenance.
- Video inspection uses MediaInfo.Wrapper.Core and its RID-selected MediaInfo.Core.Native dependency, pinned to 26.10.0 for Windows/Linux distribution through NuGet. No ffprobe installation, subprocess, transcoding, or automatic preview generation is required.
- New and changed OpenAPI schemas, resource examples, migration instructions, and regression tests are required. No frontend implementation is included.
- Out of scope: game-cover conversion, automatic game folders, tags, game-specific asset ownership, deduplication, byte replacement/versioning, YouTube import, remote URL fetching, video transcoding, automatically extracted video previews, recursive search/counts/deletion, usage-browser UI, authentication/authorization, S3/CDN deployment, malware-scanning service, and editing main specs or project conventions during this proposal.
