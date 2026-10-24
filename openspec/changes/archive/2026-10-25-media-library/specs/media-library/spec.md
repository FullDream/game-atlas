# Media Library Spec Delta

## Purpose

Organize and reuse uploaded images and videos across games through nested folders, stable assets, safe file delivery, and explicit reference-aware deletion.

## ADDED Requirements

### Requirement: Share persistent assets independently of games
The library SHALL hold images and uploaded videos independently of games. Every successful upload SHALL receive a new UUID v7, stable file URL, and UTC timestamps. Repeated names and equal file contents SHALL be allowed without replacement or deduplication. Committed assets SHALL remain available until explicitly deleted, including assets with no attachments.

#### Scenario: Reuse a committed upload
- **WHEN** one image is attached to resources in different games
- **THEN** all attachments identify the same asset without copying its bytes
- **AND** deleting the last attachment does not expire or delete the asset

#### Scenario: Keep distinct uploads with equal names
- **WHEN** the same filename or identical bytes are uploaded twice into one folder
- **THEN** two independently addressable assets are created and neither existing asset is overwritten

### Requirement: Manage manually created nested folders
Folders SHALL have server-generated UUID v7, name, nullable parentId, and UTC timestamps. POST /media/folders SHALL create a folder and return 200. Root SHALL be represented by null parentId rather than a stored folder. Names SHALL contain 1..200 trimmed characters and be unique among sibling folders after invariant case normalization, including Latin and Cyrillic. Automatic game folders SHALL NOT be created.

#### Scenario: Create nested folders with independent names
- **WHEN** users create GTA-5/Icons and Shared/Icons
- **THEN** both Icons folders are permitted and survive restart
- **AND** creating ' icons ' beside an existing Icons folder returns 409 without partial creation

#### Scenario: Validate parent and name
- **WHEN** a valid folder request identifies a missing parent
- **THEN** it returns 404
- **AND** missing/null/blank/overlength names return 422; malformed parent GUIDs return 400

### Requirement: Rename and move folders without changing file identities
PATCH /media/folders/{id} SHALL accept optional name and parentId under the shared JSON Merge Patch contract and return 200 with the complete folder. Null parentId SHALL move to root; null name SHALL return 422. Moves SHALL reject self/descendant destinations with 422 and sibling-name conflicts with 409. Missing folders or parents SHALL return 404. Changes SHALL be atomic and preserve stable asset URLs.

#### Scenario: Move an entire subtree
- **WHEN** a folder containing nested folders and assets is renamed or moved
- **THEN** descendants keep their IDs, attachment relationships, and file URLs without physical file moves
- **AND** trimmed-name and unchanged-parent no-ops preserve timestamps

#### Scenario: Reject cycles and combined failures
- **WHEN** a patch combines a valid rename with a move into that folder or its descendant
- **THEN** 422 preserves the complete hierarchy and every supplied field

### Requirement: Delete only empty folders
DELETE /media/folders/{id} SHALL return empty 204 only for a folder with no immediate assets or subfolders. A nonempty folder SHALL return 409 without removing or detaching anything. Missing or repeated deletion SHALL return 404. Empty-folder checks and deletion SHALL be atomic against concurrent child creation or moves.

#### Scenario: Preserve nonempty contents
- **WHEN** a folder contains a subfolder or an asset, including an asset whose physical file is missing
- **THEN** deletion returns 409 and all records and attachments remain unchanged

#### Scenario: Delete after moving contents
- **WHEN** all direct contents have been moved or explicitly deleted
- **THEN** folder deletion succeeds with empty 204 and its sibling name becomes reusable

### Requirement: Browse one folder through a paginated mixed list
GET /media SHALL accept folderId, offset, limit, and search. Omitted folderId SHALL select root. The 200 response SHALL contain items, limit, offset, and total. Defaults SHALL be offset=0 and limit=20, with offset>=0 and limit=1..100. Items SHALL include only direct child folders and assets, ordered folders first, then all files together, then name and id ascending within each group.

#### Scenario: Page across folders and files
- **WHEN** root contains folders, images, and videos spanning several pages
- **THEN** pagination is applied to the single combined ordered collection and total counts all matching direct children
- **AND** image/video type does not introduce another sort group and equal names are resolved by ID

#### Scenario: Empty missing and invalid queries
- **WHEN** an existing folder has no matching contents or offset exceeds total
- **THEN** 200 returns items=[] with the effective pagination and actual total
- **AND** a missing folder returns 404, invalid ranges 422, and malformed query types 400

### Requirement: Search only immediate folder contents
Search SHALL match a literal name substring among the selected folder's immediate folders and assets. Surrounding search whitespace SHALL be trimmed; omitted/blank search SHALL disable filtering. SQLite ASCII case-insensitive matching SHALL be supported without promising Unicode case folding. Search SHALL precede pagination and total calculation and SHALL NOT recurse into descendants.

#### Scenario: Keep search local and literal
- **WHEN** search contains RING, percent, underscore, or backslash
- **THEN** ASCII case is ignored and pattern characters are treated literally
- **AND** a match inside a grandchild folder or another parent is not returned

### Requirement: Return shared folder image and video representations
Mixed-list items SHALL carry required type values folder, image, or video. Assets SHALL expose id, folderId, name, url, contentType, byteLength, createdAt, and updatedAt. Images SHALL additionally expose width, height, alt defaulting to empty text, and a read-only placeholder. Videos SHALL expose nullable preview as a shared image representation. Physical paths and temporary upload records SHALL NOT be exposed.

#### Scenario: Reuse asset schemas in resources
- **WHEN** an image or video appears in the library or as a marker attachment
- **THEN** it uses the same asset representation and canonical type value
- **AND** folders cannot be attached as media and YouTube is not a library variant

#### Scenario: Represent intrinsic SVG dimensions
- **WHEN** a valid SVG lacks an absolute intrinsic width or height
- **THEN** the corresponding dimension is null rather than an invented pixel dimension
- **AND** raster-image width and height come from verified content and are positive integers

### Requirement: Provide Base64 image placeholders
Every image SHALL expose a read-only nullable placeholder. SVG and raster images whose dimensions are both at most the configured maximum SHALL skip generation and return null. Larger raster images SHALL return a bounded WebP data:image/webp;base64 URI preserving aspect ratio/transparency without changing originals. Shared responses SHALL return the same stored value; clients SHALL NOT supply it.

#### Scenario: Generate and persist a small raster placeholder
- **WHEN** JPEG, PNG, WebP, or AVIF image admission succeeds and at least one original dimension exceeds the configured placeholder maximum
- **THEN** upload and later browse/resource responses include the same persisted placeholder, with a configurable default maximum dimension of 32 pixels and bounded encoded length
- **AND** decoding the Base64 produces a valid WebP; source byteLength and dimensions continue to describe the original

#### Scenario: Skip originals at or below the placeholder dimensions
- **WHEN** both raster original dimensions are at most the configured maximum, including equality
- **THEN** admission succeeds with placeholder=null and performs no placeholder resize, encode, or Base64 generation
- **AND** with a 32-pixel maximum, 16x24 and 32x32 images are skipped while 33x16 requires a placeholder

#### Scenario: Skip placeholders for every SVG
- **WHEN** an SVG passes the static security profile, regardless of its declared size or whether intrinsic dimensions are known
- **THEN** it is admitted with placeholder=null and no SVG rasterization, resize, encoding, or Base64 generation occurs
- **AND** all shared image responses use the same rule without requiring a native SVG renderer

#### Scenario: Share placeholders across every consumer
- **WHEN** an image is a category icon, marker attachment, map-style source, or selected video preview
- **THEN** its shared image representation includes the same placeholder without regeneration on reads
- **AND** moving/renaming/changing alt preserves it, and manually missing originals leave the persisted placeholder intact while full-file delivery returns 404

#### Scenario: Reject client-generated placeholder metadata
- **WHEN** a client supplies placeholder during upload or metadata PATCH
- **THEN** 400 rejects the read-only input without partial state changes
- **AND** failure to safely generate a placeholder prevents image admission rather than committing a partly initialized image

### Requirement: Count direct folder contents
Every returned folder SHALL expose itemsCount equal to its immediate asset and subfolder records. Counts SHALL NOT recurse, depend on the current search, or exclude missing physical files. Counts SHALL reflect committed moves, uploads, and deletions without requiring separately maintained counters.

#### Scenario: Count folders and files independently of search
- **WHEN** a listed folder contains two files and one subfolder, and only one child matches the caller's search
- **THEN** its itemsCount is 3 regardless of the grandchild count or search text

### Requirement: Upload one file with one shared size limit
POST /media/assets SHALL accept multipart/form-data containing exactly one file, optional folderId/name, and image-only alt or video-only previewAssetId metadata. Success SHALL return 200 with the complete committed asset. One configurable maximum SHALL default to 314572800 bytes for every file type. Actual streamed bytes SHALL enforce 413, without trusting Content-Length or silently truncating content.

#### Scenario: Admit at the configured boundary
- **WHEN** a supported file has exactly the configured byte limit
- **THEN** the size check permits it and the committed bytes equal the upload
- **AND** one extra byte returns 413 with no visible asset or permanent unreferenced file

#### Scenario: Validate multipart fields
- **WHEN** input has multiple files, missing file, duplicate/unknown fields, or malformed folder/preview IDs
- **THEN** it returns 400 without committing an asset
- **AND** a missing destination returns 404 and invalid supported metadata returns 422

#### Scenario: Assign a display name
- **WHEN** name is omitted on upload
- **THEN** a bounded filename basename is used as display text only; supplied names must contain 1..200 trimmed characters
- **AND** filenames never select a storage path and MIME/type/read-only metadata cannot be supplied by the client

### Requirement: Verify supported image and video content
The library SHALL accept static JPEG, PNG, WebP, AVIF, safe SVG, and uploaded MP4/WebM containing video. Type and canonical contentType SHALL be derived from inspected content rather than extension or declared MIME. Unsupported, invalid, animated image, or audio-only input SHALL return 422. Accepted bytes SHALL remain unchanged; video transcoding and automatic previews SHALL NOT occur.

#### Scenario: Admit supported formats without conversion
- **WHEN** each supported static image or video container is uploaded, including with a misleading extension
- **THEN** verified type/MIME and byteLength are returned without rewriting the original
- **AND** format-dependent probing remains bounded and makes no remote requests

#### Scenario: Reject unsupported content
- **WHEN** input contains HTML, a YouTube URL, audio-only media, animation, or invalid supported-format headers
- **THEN** 422 rejects the upload without creating an asset or triggering map processing

### Requirement: Reject unsafe SVG without rewriting accepted files
SVG admission SHALL accept only an explicitly supported static element/attribute/namespace allowlist. DTD, external entities, processing instructions, scripts, event handlers, foreignObject, animation, arbitrary CSS, and external or embedded resource references SHALL be rejected with 422. Supported local fragment references SHALL remain internal. Accepted SVG bytes SHALL NOT be sanitized or reserialized.

#### Scenario: Accept a self-contained static icon
- **WHEN** an SVG uses supported shapes, paths, presentation attributes, gradients, and internal fragment references
- **THEN** it is admitted unchanged as image/svg+xml
- **AND** malformed XML, unknown namespaces/elements/attributes, or disallowed values are rejected rather than silently removed

#### Scenario: Reject active and externally resolved SVG
- **WHEN** SVG includes script/onload, foreignObject, style content, external CSS URLs, external href, data resources, DTD/entities, or XML stylesheet instructions
- **THEN** 422 is returned without executing scripts, fetching resources, or exposing the document

### Requirement: Bound media inspection independently of upload size
Inspection SHALL bound XML document size, depth, element/attribute counts, raster dimensions (initially at most 16384 per side), and video cumulative reads/concurrency. Cooperative video deadlines SHALL be observed between native parsing blocks. Exceeding a configured bound SHALL reject admission with 422 without changing accepted bytes.

#### Scenario: Refuse expensive content safely
- **WHEN** a byte-limit-compliant file exceeds an inspection bound or cannot be inspected within the configured budget
- **THEN** it is rejected without partial admission, external resource access, or leaked inspection resources

### Requirement: Retain ownership during cooperative native video inspection
Video inspection SHALL use opened local streams through packaged cross-platform MediaInfo libraries. No network or external-resource input SHALL be supplied. Native execution SHALL retain its stream, concurrency slot and request staging file until it returns; cancellation SHALL NOT imply forcibly interrupting a native call.

#### Scenario: Cancel while a native parsing block is running
- **WHEN** the upload is cancelled or its cooperative deadline expires during a native parsing call
- **THEN** its reader and request staging file remain alive until that call returns
- **AND** no asset is admitted, cancellation is observed between blocks, and resources are released after native work ends

### Requirement: Update shared asset metadata without replacing bytes
PATCH /media/assets/{id} SHALL accept optional name and folderId, image alt, or video previewAssetId and return 200 with the shared asset. Null folderId SHALL move to root; null previewAssetId SHALL clear a preview. Null name/alt SHALL return 422. Other subtype fields and byte replacement SHALL be rejected. Effective metadata changes SHALL affect later resource responses without touching resource timestamps.

#### Scenario: Rename move and change shared alt
- **WHEN** one image is renamed, moved, or given new alt text while attached in several games
- **THEN** its ID, URL, bytes, and attachment order remain unchanged and subsequent responses resolve current shared metadata
- **AND** only effective asset changes update its updatedAt; no-op metadata patches perform no update

#### Scenario: Apply the common PATCH contract
- **WHEN** an update omits fields or explicitly clears nullable metadata
- **THEN** omission preserves state and permitted nulls clear it atomically
- **AND** empty objects/null required values return 422, malformed/unknown/read-only fields 400, unsupported media 415, and missing assets/destinations 404

### Requirement: Select optional video previews from library images
Video previewAssetId SHALL optionally reference an existing library image, including safe SVG. Missing preview assets SHALL return 404 and non-image destinations 422. A preview SHALL count as an asset use that prevents deletion of that image. Clearing/replacing a preview SHALL update the relationship atomically without deleting either image.

#### Scenario: Keep a reused preview protected
- **WHEN** an image is used only as the preview of one video
- **THEN** deleting that image returns 409
- **AND** clearing the preview makes the image eligible for explicit deletion without automatically removing it

### Requirement: Deliver all committed files through stable library URLs
GET and HEAD /media/files/{id} SHALL serve immutable originals through static middleware without database access or Mediator. Delivery SHALL provide correct MIME, nosniff, public caching, filesystem validators, conditional 304 and video byte ranges. URLs SHALL survive metadata changes. Staging and directory listings SHALL remain private.

#### Scenario: Read seek and cache an asset
- **WHEN** a committed asset is requested, including an image used as a map original
- **THEN** its unchanged bytes and verified headers are available without a separate original-download restriction
- **AND** HEAD has no body, valid video ranges return 206, unsatisfiable ranges 416, and matching validators can return 304

#### Scenario: Serve a crash-orphaned original
- **WHEN** original bytes remain in the asset store without a matching database record after a crash
- **THEN** the original remains accessible by its known URL without requiring a database record
- **AND** no automatic cleanup or recovery service is introduced

### Requirement: Protect SVG delivery and metadata presentation
SVG responses SHALL include a restrictive Content-Security-Policy with sandbox and no script permissions, including direct navigation, HEAD, and conditional responses. Names, filenames, folder names, and alt SHALL be ordinary text rather than executable markup. Consumer documentation SHALL require image-context SVG display and contextual text escaping, without raw SVG/metadata injection into HTML.

#### Scenario: Prevent active document behavior
- **WHEN** an accepted SVG is embedded as an image or opened directly
- **THEN** it is delivered with image/svg+xml, nosniff, and the restrictive SVG CSP
- **AND** metadata resembling HTML remains text; the API does not claim that MIME headers alone sanitize content

### Requirement: Preserve records after out-of-band file removal
Deleting or damaging a physical file outside the application SHALL NOT automatically delete its asset record, attachments, or preview relationships. Missing file delivery SHALL return 404. Library/resource metadata SHALL remain queryable without a periodic availability scan or an availability status field. A manually missing file SHALL NOT bypass the in-use deletion guard.

#### Scenario: Keep metadata for an unavailable asset
- **WHEN** an operator manually removes a file used by a marker, icon, or map style
- **THEN** its record and references remain and file requests return 404
- **AND** a map decoding failure is reflected safely in processing state rather than silently detaching its source

### Requirement: Delete only unused assets immediately
DELETE /media/assets/{id} SHALL return 409 while a category icon, marker attachment, style, or video preview references it. Otherwise it SHALL delete the physical file immediately and remove its record before returning empty 204. Missing files SHALL be tolerated; repeated deletion SHALL return 404. Serialized relationship checks and restrictive FKs SHALL prevent dangling references. No active-reader leases, cleanup intents, deferred deletion, startup cleanup, or retry jobs SHALL be introduced.

#### Scenario: Reject persisted uses
- **WHEN** any current resource or preview references an asset
- **THEN** deletion returns 409 without detachment or byte changes
- **AND** uses across all games count even when original bytes are missing

#### Scenario: Delete an unused original
- **WHEN** the last relationship is removed and the client deletes the asset
- **THEN** the original is deleted synchronously and 204 follows record removal
- **AND** an IO failure returns safe 500 while retaining metadata, without scheduling cleanup

#### Scenario: Accept independent storage and metadata failure boundaries
- **WHEN** a DB failure follows physical deletion or a crash interrupts the operation
- **THEN** metadata may reference a missing file or an orphan may remain
- **AND** no automatic reconciliation infrastructure is added

### Requirement: Use request-scoped staging without automatic recovery
Uploads SHALL use generated staging files with try/finally deletion after native readers finish. Committed unreferenced assets SHALL remain. The system SHALL NOT create media sessions, leases, expiry, cleanup-intent tables, startup cleanup, periodic maintenance, or recovery workers.

#### Scenario: Finish or cancel an ordinary upload
- **WHEN** upload handling completes, rejects input, or observes cancellation
- **THEN** finally removes its temporary file after readers return
- **AND** committed assets are not removed by temporary-file cleanup

#### Scenario: Accept files remaining after a crash
- **WHEN** the process crashes before finally or between file promotion and DB commit
- **THEN** temporary or orphaned files may remain
- **AND** startup and background processing do not scan, recover, or remove these files

#### Scenario: Keep native capability checks without a maintenance worker
- **WHEN** the API starts
- **THEN** necessary media processor capability checks still run explicitly
- **AND** no media cleanup IHostedService or BackgroundService is registered

### Requirement: Preserve safety and document the shared contract
Media operations SHALL use ProblemDetails for expected failures and safe 500 responses for unexpected failures. OpenAPI SHALL describe mixed folder/image/video responses, image/video asset responses, UUID attachment inputs, multipart upload, the static file URL contract through DTO documentation rather than a separate operation, limits, PATCH semantics, and deletion conflicts. Resource operation order SHALL remain GET/POST/PATCH/DELETE. No PUT, batch upload, or recursive deletion SHALL exist.

#### Scenario: Discover the library without implementation leakage
- **WHEN** a client reads OpenAPI and resource examples
- **THEN** discriminator mappings, nullable metadata, direct counts, pagination defaults/bounds, supported types, the 300 MiB limit, and status codes are discoverable
- **AND** no filesystem paths, upload internals, arbitrary-URL attachment schema, or YouTube variant is exposed

### Requirement: Initialize without legacy media conversion
The media-library transition SHALL support fresh empty databases without legacy media or map-ownership converters, import manifests or preparation commands. Applied migrations SHALL remain unchanged. Unresolved legacy references SHALL NOT be silently discarded. Populated legacy upgrades SHALL be unsupported; no automatic database reset or file deletion SHALL occur.

#### Scenario: Initialize a fresh store
- **WHEN** the complete migration chain is explicitly applied to an empty disposable database
- **THEN** the library and resource schemas are ready for normal uploads and attachments
- **AND** no legacy import command or conversion infrastructure is required
