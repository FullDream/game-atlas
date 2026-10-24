# Design

## Context

See proposal.md for motivation and scope. The repository uses four backend projects, SQLite, IApplicationDbContext, shared validation/Result integration, Optional merge-patch handling, and PaginatedResult. Before this change, maps admitted multipart uploads with map-owned originals and durable upload sessions. The old map-specific probe accepted only PNG/JPEG. These paths are removed; shared media admission validates originals and the map processor only generates derived tiles.

The previous category icon string and marker-specific image/video/YouTube hierarchy duplicated URL/dimensions/alt/preview data. These are replaced by shared asset references. Map deletion detaches sources and removes only derived files.

This design is necessary because the change spans persistence, shared ownership, security, native probing, and migration. No code or persistent data is changed by creating these planning artifacts.

## Goals / Non-Goals

**Goals:** One owner for immutable uploaded bytes; explicit foreign-key-backed uses; shared response models; predictable nested-folder browsing; race-safe deletion/admission; retention of current map rendering and resource rules.

**Non-Goals:** See proposal.md for product exclusions. Do not turn logical folders into physical paths, implement a general storage-provider framework, unify folder and asset domain entities solely for the API union, or build an unrestricted SVG sanitizer. Game.Image remains its existing plain-string contract in this change.

## Decisions

### 1. Shared feature within the existing layers

Use Media feature folders in Domain/Application/Api and media persistence/storage/inspection implementations in Infrastructure. Domain owns metadata invariants; Application orchestrates existing context transactions, attachment checks, and use cases. Api owns multipart parsing, file responses, polymorphism, and endpoint conventions. Infrastructure owns disk and native/XML probes; Media has no lease or automatic cleanup subsystem. Keep DTOs and extension mappings in separate files.

Do not add projects, repositories, microservices, or a general attachment registry. Explicit consumers are known and can use proper FKs. This avoids an untyped OwnerType/OwnerId table whose integrity cannot be enforced by SQLite.

### 2. Separate folders, assets, and usages

- MediaFolder: Id, ParentId?, Name, NormalizedName, CreatedAt, UpdatedAt. It is an independent entity, not an asset subtype.
- MediaAsset: Id, FolderId?, Name, ContentType, ByteLength, immutable storage identity, CreatedAt, UpdatedAt. Persist ImageAsset and VideoAsset directly using string TPH discrimination; image metadata includes Alt, intrinsic Width/Height, and a nullable generated Placeholder, video metadata includes PreviewAssetId?.
- Category gains required IconAssetId instead of the persisted Icon string.
- MapStyle gains required SourceAssetId. Source type/dimensions may be read through the asset for validation; do not retain an independently mutable second original record. Processing status, progress, error, sequence, attempts, and style timestamps remain on MapStyle.
- MarkerMedia becomes a lightweight ordered MarkerAsset attachment with MarkerId/AssetId/Order, without duplicated subtype metadata. A unique (MarkerId, AssetId) constraint enforces no duplicate assets. Deleting a marker cascades only to attachment rows.
- Video preview, category icon, style source, and marker attachment asset FKs use restrictive deletion. Folder FKs also restrict deletion rather than recursively removing children.

Logical moves update metadata only. Shared alt is intentionally global: later reads reflect the current asset, but this does not change the timestamps of categories/markers/styles using it. No file-byte replacement operation exists.

### 3. Proposed HTTP contract

| Method | Route | Input | Success |
| --- | --- | --- | --- |
| GET | /media | folderId?, offset=0, limit=20, search? | 200 PaginatedResult<MediaItemDto> |
| GET / HEAD | /media/files/{id} | validators or video Range | bytes / 206 / 304 |
| POST | /media/folders | name, parentId? JSON | 200 folder |
| POST | /media/assets | file, folderId?, name?, image alt? or video previewAssetId? multipart | 200 image/video asset |
| PATCH | /media/folders/{id} | optional name, parentId | 200 folder |
| PATCH | /media/assets/{id} | optional name, folderId, image alt or video previewAssetId | 200 image/video asset |
| DELETE | /media/folders/{id} | none | empty 204, or 409 if nonempty |
| DELETE | /media/assets/{id} | none | empty 204, or 409 if referenced/in active use |

Use the existing shared PATCH media/shape rules. All mutable fields are optional. Null parentId/folderId moves to root; null previewAssetId clears preview. Names/alt cannot be null. Metadata patches reject fields belonging to a different subtype, read-only fields, and bytes/type changes. Missing destination/entity IDs return 404; folder cycles, invalid names, duplicate marker asset IDs, and incompatible types return 422. Existing resources retain their map-scoped 404 checks independently of globally reusable assets.

Endpoint/field spelling, response expansion, and inspection defaults below are proposed implementation defaults for review; they do not add extra product capabilities. Preserve current successful mutation status 200 and deletion 204.

Category input uses iconAssetId; the existing nine-field CategoryDto keeps icon but changes it to ImageAssetDto. Style creation input becomes name/backgroundColor/sourceAssetId JSON; its existing response gains source: ImageAssetDto, making ten style fields while MapDto stays fifteen fields. Marker creation/PATCH keep media as the property name but accept distinct ordered GUIDs; MarkerDto keeps eight root fields and resolves media to MediaAssetDto images/videos. No URL input fallback or YouTube variant remains.

MediaFolderDto exposes type=folder, id, parentId, name, itemsCount, createdAt, updatedAt. Common image/video fields are defined in the library spec; VideoAssetDto.preview resolves an ImageAssetDto or null. A single ImageAssetDto/VideoAssetDto implementation is reused across list and consumer responses. System.Text.Json declares concrete discriminator mappings for the mixed item base and image/video asset base. Reuse the project's anyOf OpenAPI convention, with unique required type values and no folder variant in asset-only responses. Do not introduce duplicate discriminator properties.

SVG dimensions are nullable doubles when absolute intrinsic dimensions can be read safely; relative/unspecified dimensions remain null. Raster dimensions are verified positive integers, representable in those response fields. Map attachment accepts only raster assets with exact integer dimensions. Original filename is display text only; absent name uses a basename within the same name bounds. Alt defaults to empty, is stored as text without HTML encoding, and is not trimmed implicitly.

ImageAssetDto additionally exposes read-only placeholder: a complete data:image/webp;base64,... URI, or null for SVG and raster images whose dimensions are both at most the configured placeholder maximum. This shared field also appears in CategoryDto.icon, MarkerDto.media images, MapStyleDto.source, and VideoAssetDto.preview. Video assets themselves do not generate image previews or contain a separate placeholder; their explicitly selected preview image supplies one.

### 4. Browse as one combined query shape

Build a server-side union of immediate child folders and assets, then filter, count, sort, skip, and take. The sort key is folder-first, Name, Id; all image/video files share the same rank. Use SQLite's established name ordering rather than promising locale-specific ordering. Search reuses the game-catalog escaped literal substring behavior: trimmed, ASCII case insensitive, no recursive expansion.

Derive folder itemsCount through grouped/correlated counts of direct folder/asset records. Fetch nested preview data in a bounded query set, not one request/query per row. Use a short consistent read transaction for the page/count/preview projection and release it before returning or streaming bytes. Query count must remain bounded as page size increases. No full-file scans, remote calls, periodic existence checks, or in-memory materialization of an entire library tree for listing.

Folder uniqueness needs both a filtered unique index on NormalizedName where ParentId is null and a (ParentId, NormalizedName) unique index for nonroot siblings; nullable SQLite uniqueness alone is insufficient for root names. Apply the project's invariant uppercase normalization. Serialize hierarchy writes with the existing SQLite transaction approach, including descendant/cycle checks, empty-folder delete, and root-name conflicts. A move into the same parent or a trimmed-equal display name is a no-op; an effective case-only display change is still a rename.

### 5. Immutable storage with request-scoped staging

Use one configurable Media storage root, default storage/media, with generated staging/{uploadId} and assets/{assetId} files, ignored by Git. Keep map tiles/work paths stable. Reject path/link traversal; expose the assets directory through static library URLs; never expose staging.

Stream one file into staging, inspect its unchanged bytes, validate destination/preview metadata in the admission transaction, and promote it on the same volume. A request-level try/finally always attempts deletion of its temporary staging file, including rejection and cancellation. Keep the staging file and native stream alive until inspection returns. Do not create durable admission sessions, leases, expiry records, startup cleanup, periodic scans, or a media hosted/background service. Retain native capability checks explicitly during API startup without a cleanup service.

One Media MaximumBytes setting defaults to 314572800. Count actual bytes and bound multipart metadata/envelope overhead. Committed unreferenced assets remain indefinitely. After a process crash, temporary or orphaned promoted files may remain; this is an accepted limitation with no automatic reconciliation.

### 6. Content inspection without transforming files

Reuse pinned NetVips 3.2.0 / NetVips.Native 8.18.7 for raster metadata/decoding checks, extending exact format recognition and static-image checks to WebP/AVIF. Explicit capability/fixture checks must verify required loaders on the deployment runtime, including AVIF, rather than treating the package version as proof. Preserve EXIF/orientation behavior and original bytes; generation remains 8-bit sRGB transparent lossless WebP with existing geometry. No silent auto-rotation, recompression, or resizing is introduced.

Use MediaInfo.Wrapper.Core and MediaInfo.Core.Native 26.10.0 for MP4/WebM container/stream inspection, requiring an actual video stream with positive dimensions. NuGet supplies platform-specific native assets for Windows and Linux; no executable installation or PATH configuration is needed. Feed only an already-opened local seekable stream through StreamMediaSource, never paths/URLs into the native Open API; do not discover external subtitles, discs, remote resources, or perform conversion. Limit cumulative reads to 8 MiB, use 64 KiB blocks, permit two simultaneous video inspections, and apply a cooperative 30-second deadline by default. Native calls cannot be forcibly interrupted in process: retain the stream, staging file and concurrency slot until each call returns, then observe cancellation before admission. Do not describe cooperative cancellation as process isolation or a hard execution-time bound. Validate native readiness, pin the native payload through NuGet, test real MP4/WebM/audio-only/malformed fixtures, and verify deployment on the selected Linux image. License distribution notices are deferred until before distribution as agreed. Container acceptance does not promise every client can decode every contained codec.

Do not add a new image or HTML-sanitization library. SVG uses System.Xml.XmlReader with explicit DtdProcessing.Prohibit, XmlResolver=null, bounded characters/depth/counts, and cancellation checks. Derive the XML character budget from the common file-byte cap rather than adding a lower SVG upload-byte setting. Initial structural budgets are depth 64, 100000 elements, and 64 attributes per element; initial raster bound remains 16384 per side. Probe timeout defaults to 30 seconds. These are parser/resource budgets, not competing maximum file-size settings.

### 7. A deliberately restricted static SVG profile

Validate parsed namespaces, element names, attribute names, and values using positive allowlists. Support svg/g/defs, path/basic shapes, title/desc text, linear/radial gradients/stops, clipPath/mask, and the minimal associated presentation/geometry attributes. Only the SVG namespace and needed declaration namespaces are accepted; xlink is recognized only for supported internal references. Do not enable use/image/style/filter/animation/foreignObject/script or arbitrary metadata namespaces in the initial profile. Unsupported well-intentioned SVG also returns 422; document that users may need to export a simpler SVG.

Validate internal references only for allowed reference attributes/paint values with a strict local fragment grammar, ensure targets are in the same document, and reject unknown URL-bearing attributes, external links, data URIs, processing instructions, event attributes, CSS declarations, and external XML resolution. Reject rather than stripping or reserializing. Regex may check individual bounded value grammars after XML parsing; it must not parse the SVG document. Include an adversarial corpus for namespace aliases, encoded values, DTD/entities, CSS/external URLs, malformed XML, deep documents, and unsafe SVG disguised by extension/MIME.

File responses use server-derived Content-Type and nosniff. SVG responses additionally use Content-Security-Policy: sandbox; default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'. Preserve headers on GET/HEAD/304 and test supported SVG gradients/clip/mask still render through image context. Names/alt are plain text; consumer docs require escaping and img rendering, never raw innerHTML or inline insertion of uploaded SVG. Separate media origin is a future deployment option, not a required new service.

### 7a. Persist small image placeholders at admission

After content validation, return null for SVG without invoking a renderer. For raster images, compare both original dimensions with the configured placeholder maximum. When both are less than or equal to it, persist null and skip placeholder resize/encoding/Base64 generation; the client can use the original URL directly. Otherwise generate one tiny WebP with the existing NetVips runtime before committing admission. The proposed default is a maximum 32-pixel long side, aspect-preserving resize with alpha retained, and no raster-image upscaling. Use a low-quality WebP encoding for this derivative only, with an explicit encoded-URI budget initially 8 KiB. Defaults are configuration, not client-supplied input. The original remains byte-identical and its MIME/byteLength/dimensions remain independent of this derivative. A safely unprocessable image returns the normal inspection failure; an unexpected storage/native failure uses safe 500. Neither case commits a partial image asset.

Store the resulting data URI with image metadata rather than regenerating on reads or adding a separate HTTP thumbnail request. It is immutable derived content because original bytes cannot change. Name/alt/folder edits do not recompute it or touch resource timestamps. The payload is bounded and included in existing image projections, without extra queries; pagination bounds how many are returned. It remains available from metadata if the original file is removed outside the application.

SVG always has placeholder=null, independently of intrinsic dimensions, viewBox, or file size. XML security validation remains mandatory, but no SVG decoding/rasterization is required and no native SVG-loader dependency is introduced for placeholders.

Document the field as ordinary raster image data. Consumer CSP must allow data: for image sources where these placeholders are used; this does not permit inline SVG or weaken the CSP applied when serving SVG documents. Automatic extraction of a preview from video remains out of scope.

### 8. Reference-safe immediate deletion

Use restrictive asset FKs and serialized delete-vs-attach checks. Check category icons, marker attachments, style sources, and video previews. A referenced asset returns 409 without detachment. No file-use lease or active-reader registry is introduced. After a style is deleted, its running reader no longer independently blocks asset deletion; platform file locks can produce a normal safe storage error, or the attempt may fail if its source disappears.

Delete an unused original immediately inside the request, then remove its metadata and commit. An already missing file is harmless. A filesystem deletion failure returns safe 500 and retains metadata. Filesystem operations and database commits are not atomic: a later DB failure can leave metadata pointing at an absent file. Do not create cleanup intents, retry jobs, startup cleanup, periodic checks, or automatic recovery. A crash may leave temporary/orphaned files. These are accepted limitations, not reasons to add coordination infrastructure.

Resource deletion still only detaches assets. Existing map-owned derived-output cleanup remains separate and cannot remove shared originals.

### 9. Common file delivery and existing map processing

The user explicitly chose common delivery of map originals. GET/HEAD /media/files/{id} serves immutable originals through ASP.NET Core static-file middleware without database access or Mediator. The route accepts generated GUIDs only, maps to the existing extensionless asset filenames, and determines MIME from the admitted file header. Static middleware supplies filesystem validators, HEAD, conditional responses and byte ranges; SVG CSP, nosniff and immutable public caching apply. Staging remains private. There is no separate OpenAPI operation. As with tiles, an orphan original may remain accessible by its known URL after a crash; no cleanup or availability infrastructure is added. Metadata reads do not stat every file. Missing physical files return 404, records stay visible, and decoder failures become safe processing failures.

Style attachment is now a short database operation, not upload admission: check scope, source/type/dimensions/name, create source relationship and queued style atomically, return complete map. Style attachment does not create upload work. One native job still processes styles sequentially with attempt fencing, safe progress, restart recovery, and failed-only retry. Source metadata changes may appear in subsequent SSE snapshots but do not requeue/rewrite style processing metadata. Reads join sources/previews in a bounded consistent query shape.

### 10. Fresh database schema

Legacy media does not need preservation. Remove media and map-ownership preparation/import commands, manifests and conversion helpers. Remove the obsolete map-upload Probe API and its duplicate image inspection; library admission owns file validation. Unapplied media migrations must not create legacy mapping/backfill tables; refuse populated source tables instead. Apply the migration chain explicitly to a new empty database and upload assets through the regular API. Keep historical migrations immutable and existing guards against unresolved legacy references; populated legacy upgrades are unsupported. This decision does not execute a database reset or delete existing files.

## Risks / Trade-offs

- Shared metadata changes affect every consumer -> make this explicit in API/docs and test no resource timestamp rewrites.
- Public map-original URLs broaden access -> this is the user's explicit choice; keep temporary/work files private and introduce no implicit ownership restrictions based on uses.
- Restricted SVG rejects some design-tool exports -> document the supported profile; do not weaken validation into a denylist or silently sanitize.
- 300 MiB does not bound decoded/native/XML cost -> stream bytes, retain inspection and placeholder-output budgets, skip SVG placeholder rendering entirely, feed bounded local streams to MediaInfo with limited concurrency and cooperative cancellation, and test deployment codecs.
- Filesystem and SQLite cannot commit together -> request-scoped staging/finally, immediate deletion, and explicitly accepted crash/orphan and DB/filesystem consistency limitations.
- Shared file deletion can race with attachments or worker shutdown -> restrictive FKs and serialized checks for persisted relationships; no active-reader leases.
- Populated legacy databases are unsupported upgrade sources -> use a fresh database; do not silently clear persistent data.
- Global library has no access isolation -> authentication/authorization and workspace ownership remain separate future work; no security boundary is inferred from folders.

## Migration Plan

1. Verify fresh upgrades, foreign keys, model consistency and empty rollback using disposable databases and filesystems.
2. Apply EF migrations explicitly to a new empty database. No legacy media conversion or automatic database reset is provided.
3. Deploy the application with RID-selected image/MediaInfo native libraries; configure storage and processing limits, then verify file headers/ranges, tiles, SSE and workers.
4. Restore matching database/filesystem backups for populated rollback when required. Existing persistent stores are not modified without a separate request.

## References

- [Microsoft XmlReaderSettings](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlreadersettings?view=net-10.0)
- [OWASP file upload guidance](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html)
- [MDN SVG image contexts](https://developer.mozilla.org/en-US/docs/Web/SVG/Guides/SVG_as_an_image)
- [MediaInfo managed/native distribution](https://www.nuget.org/packages/MediaInfo.Wrapper.Core)
- [NetVips native changelog](https://kleisauke.github.io/net-vips/CHANGELOG.native.html)
