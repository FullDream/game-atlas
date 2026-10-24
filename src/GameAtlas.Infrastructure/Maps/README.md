# Map storage and processing

Maps run in one API process with one native image processor. Apply EF migrations explicitly before starting it; startup never migrates. Each MapStyle references one immutable library image and owns one derived pyramid. There is no source-version entity, replacement upload, or active/current image pointer.

## Native runtime

Pinned packages are NetVips 3.2.0 and NetVips.Native 8.18.7. Publish for the deployment RID, for example `dotnet publish src/GameAtlas.Api -c Release -r linux-x64`, retaining native assets. Alpine needs a matching linux-musl RID. Startup checks actual PNG/JPEG/WebP/AVIF codecs. Windows tests exercise native codecs; repeat them on the Linux deployment image.

Media-library admission checks static JPEG/PNG/WebP/AVIF signatures and dimensions, rejecting animation. Deferred pixel-decoding failure becomes failed style state. Original bytes remain unchanged; EXIF autorotation is not applied. Tile pixels are converted to 8-bit sRGB plus alpha before lossless WebP; this does not preserve HDR/16-bit encoding or promise lossless reduction between zooms.

Native Google-layout output uses 512 pixels, zero overlap, one-tile depth, transparent padding, no recentering, and disabled blank skipping. Normalize z/row/column files to z/x/y in private work before validation and publication. Native cross-operation caching is disabled to release Windows filesystem handles between disposed jobs.

## Configuration and layout

Maps:StorageRoot defaults to storage/maps against API content root and is Git-ignored. Work and tiles must share a volume for atomic publication:

    work/{mapId:N}/{styleId:N}/{attemptId:N}/
    tiles/{mapId:N}/{styleId:N}/{z}/{x}/{y}.webp

Originals live only in the independent media volume under generated asset IDs. Existing legacy content/staging directories are not runtime upload targets. Back up SQLite, media originals and derived tiles consistently. No source bytes are copied when attaching an image to a style.

Only the trusted tiles subtree is exposed through /map-tiles with a physical file provider and WebP-only content mapping. Do not expose the root, work paths, JSON manifests, or directory listings. Generated IDs determine paths; storage operations reject links/junctions and path escape. Static GET/HEAD/304 performs no database lookup; deleted files may remain accessible until cleanup and cached bytes may survive longer. Common original GET/HEAD uses `/media/files/{assetId}` through static-file middleware without database access or Mediator; crash-orphaned originals can remain accessible by known URL.

Upload limits and native inspection are configured through Media:Limits; see the media README. Maps:WorkerEnabled=false disables the hosted worker for isolated tests without migrating or discarding queued styles.

## Explicit migrations

Initialize a fresh empty database through the full EF migration chain. No legacy ownership or media preparation commands are provided. Historical migrations remain unchanged; populated legacy upgrades are unsupported. See ../Persistence/README.md for schema and rollback constraints.

```sh
dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Populated rollback requires matching database/filesystem backups and the preceding application. Runtime startup never applies migrations or resets a database.

## Admission and recovery

Blank maps create no upload work and may own content before any style exists. JSON style creation atomically attaches an existing library raster image. Sources must match the immutable map dimensions. Media admission owns inspection, staging cleanup and promotion; a style does not own an original directory or upload session. A lost style response leaves discoverable queued work; GET before retrying creation.

The worker reads the shared source directly from media storage without file leases or an active-reader registry. Removing a style detaches its source; explicit asset deletion can then affect an ongoing attempt. Missing sources fail safely, and stale attempts cannot restore deleted style metadata. Shared metadata changes never requeue jobs or change source bytes. Media has no startup/periodic recovery or cleanup service.

Queued MapStyles are the durable queue. One native job runs outside transactions, independent of viewers. Native callbacks fill a thread-safe buffer; fresh contexts persist progress at most about once per second. Restart resets interrupted processing to queued/0 with a fresh attempt; ready styles remain ready. Manual retry applies only to failed styles and reuses the same original and style ID. Failed styles are not retried endlessly.

Validate manifests, all expected coordinates, decoded 512x512 tiles and geometry before atomic directory publication. Complete files are statically accessible after the move; metadata/SSE advertise them only after the guarded ready commit. Crashed or failed incomplete attempts are rebuilt/cleaned. Output-only cleanup preserves originals and ready tiles. Late attempts cannot resurrect deleted metadata or overwrite concurrent name/color edits.

Map/style/game deletion commits metadata removal and cleanup intents together. Map deletion also cascades to its groups/categories/markers/attachment rows; style deletion, including the final style, retains that content. Game deletion captures durable cleanup for every map before cascading. HTTP 204 does not promise immediate disk erasure or cache revocation. Cleanup covers work/public tiles, survives FK cascades, is serialized with native writers, and retries IO/access failures. Monitor cleanup-intent IDs, volume permissions/free space, queue age, failed styles, native latency, and RSS. Never remove referenced/live originals or use marker-media URLs as cleanup paths. Restore matching database/storage backups with writers stopped.

## Verification observations

On 2026-10-23, Windows win-x64 / .NET 10.0.13 with pinned native packages generated a 16384x16384 solid-black PNG pyramid with all 1365 tiles in 8.62 seconds. The isolated host peak was 300335104 bytes (about 286.4 MiB); original size was 784264 and tile bytes totaled 70980. This highly compressible fixture includes host overhead and excludes worker validation/database work; it is not a RAM/time upper bound or Linux result. Repeat resource tests with representative inputs on the deployment image.

Tests cover shared source attachments, interrupted tile-processing recovery, complete files before ready, stale deletion fencing, original-preserving failure/retry, durable cleanup, every asymmetric zoom and alpha edge/blank cell, native cancellation, private-file exclusion, zero-SQL static GET/HEAD/304, ten-field style DTOs, nine map operations, independent SSE snapshots/heartbeats, and at most two queries per snapshot. A disposable database test confirms the legacy-data guard leaves existing map/source rows and parent data intact.
