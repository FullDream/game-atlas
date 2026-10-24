# Map storage and processing

Maps run in one API process with one native image processor. Apply EF migrations explicitly before starting it; startup never migrates. Each MapStyle owns one immutable original and one pyramid. There is no source-version entity, replacement upload, or active/current image pointer.

## Native runtime

Pinned packages are NetVips 3.2.0 and NetVips.Native 8.18.7. Publish for the deployment RID, for example `dotnet publish src/GameAtlas.Api -c Release -r linux-x64`, retaining native assets. Alpine needs a matching linux-musl RID. Startup checks actual PNG/JPEG/WebP codecs. Windows tests exercise native codecs; repeat them on the Linux deployment image.

Admission checks still PNG/JPEG signatures, readable metadata and encoded dimensions, rejecting animation. Deferred pixel-decoding failure becomes failed style state. Original bytes remain unchanged; EXIF autorotation is not applied. Tile pixels are converted to 8-bit sRGB plus alpha before lossless WebP; this does not preserve HDR/16-bit encoding or promise lossless reduction between zooms.

Native Google-layout output uses 512 pixels, zero overlap, one-tile depth, transparent padding, no recentering, and disabled blank skipping. Normalize z/row/column files to z/x/y in private work before validation and publication. Native cross-operation caching is disabled to release Windows filesystem handles between disposed jobs.

## Configuration and layout

Maps:StorageRoot defaults to storage/maps against API content root and is Git-ignored. Use an absolute durable writable volume in deployment; back up SQLite and storage together. Staging/work/content/tiles must share a volume for atomic directory moves:

    staging/{sessionId:N}/{fileId:N}
    content/{mapId:N}/{styleId:N}/original
    work/{mapId:N}/{styleId:N}/{attemptId:N}/
    tiles/{mapId:N}/{styleId:N}/{z}/{x}/{y}.webp

Only the trusted tiles subtree is exposed through /map-tiles with a physical file provider and WebP-only content mapping. Do not expose the root, original/staging/work paths, JSON manifests, or directory listings. Generated IDs determine paths; storage operations reject links/junctions and path escape. Do not add external files or links to the public subtree. Static GET/HEAD/304 performs no database lookup; deleted files may remain accessible until cleanup and cached bytes may survive longer.

Maps:Limits defaults are FileBytes=209715200, MaximumDimension=16384, MetadataBytes=65536, and EnvelopeBytes=1048576. Count actual streamed bytes without trusting Content-Length. Proxy body limits must allow FileBytes + EnvelopeBytes; configure upload timeouts for slow clients. JSON endpoint limits remain unchanged. Maps:WorkerEnabled=false disables the hosted worker for isolated tests without migrating or discarding queued styles.

## Explicit migration and legacy-data safety

The original 20261023181923_Maps migration follows 20261023145510_Markers. It removes unchecked Games.MapId and preserves actual game/group/category/marker/media data. Such old unchecked identifiers do not create maps. The current seven-field GameDto exposes no mapId; maps are listed through their collection.

SimplifyMapStyles introduces MapStyles with original metadata and processing fields directly on the style, MapAdmissionStyles, and style-level cleanup. The applied Maps migration is not rewritten. The generated schema upgrade drops obsolete version tables only when all legacy map, admission, and cleanup tables are empty. A named CHECK guard runs before schema changes and refuses nonempty legacy data. Refusal preserves the existing database; do not bypass the guard or delete data to force the upgrade.

The developer database was upgraded after the user explicitly authorized removal of its single test map and owned files. A consistent SQLite backup was created before the explicit EF CLI upgrade. Parent-table contents and timestamps were verified unchanged after deletion and migration. Other populated legacy installations still require an agreed data-and-file transfer or authorized reset; changing tables alone cannot preserve old versioned file paths. Back up both stores and stop writers before a transition. Do not deploy the new worker against the old schema.

For a fresh database (or after completing the ownership preparation documented in ../Persistence/README.md), apply migrations manually:

```sh
dotnet ef database update --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Only empty-map databases may roll back the ownership migration. For populated maps restore matching SQLite/storage backups instead. For an empty-map database, after stopping the API and backing up both stores:

```sh
dotnet ef database update 20261023145510_Markers --project src/GameAtlas.Infrastructure --startup-project src/GameAtlas.Api
```

Rollback removes map metadata, restores nullable Games.MapId with null values, and preserves other feature data. It cannot reconstruct discarded source history or automatically translate filesystem layouts. Files are not silently removed by database migrations; retain backups for any agreed recovery/cleanup.

## Admission and recovery

Blank maps create no upload work and may own content before any style exists. Style uploads always target an existing scoped map. Internal upload leases commit before staging; planned map/style owners commit before original promotion. The short admission transaction saves queued styles and removes the lease together. Files alone never imply acceptance. A lost response after commit leaves discoverable accepted work; GET before uploading again. Rollback removes only its unreferenced owners; IO failures retain durable cleanup intents. Live leases renew at bounded frequency, expired leases cannot renew/complete, and reconciliation protects live or committed styles.

Queued MapStyles are the durable queue. One native job runs outside transactions, independent of viewers. Native callbacks fill a thread-safe buffer; fresh contexts persist progress at most about once per second. Restart resets interrupted processing to queued/0 with a fresh attempt; ready styles remain ready. Manual retry applies only to failed styles and reuses the same original and style ID. Failed styles are not retried endlessly.

Validate manifests, all expected coordinates, decoded 512x512 tiles and geometry before atomic directory publication. Complete files are statically accessible after the move; metadata/SSE advertise them only after the guarded ready commit. Crashed or failed incomplete attempts are rebuilt/cleaned. Output-only cleanup preserves originals and ready tiles. Late attempts cannot resurrect deleted metadata or overwrite concurrent name/color edits.

Map/style/game deletion commits metadata removal and cleanup intents together. Map deletion also cascades to its groups/categories/markers/media rows; style deletion, including the final style, retains that content. Game deletion captures durable cleanup for every map before cascading. HTTP 204 does not promise immediate disk erasure or cache revocation. Cleanup covers content/work/public tiles, survives FK cascades, is serialized with native writers, and retries IO/access failures. Monitor cleanup-intent IDs, volume permissions/free space, queue age, failed styles, native latency, and RSS. Never remove referenced/live originals or use marker-media URLs as cleanup paths. Restore matching database/storage backups with writers stopped.

## Verification observations

On 2026-10-23, Windows win-x64 / .NET 10.0.13 with pinned native packages generated a 16384x16384 solid-black PNG pyramid with all 1365 tiles in 8.62 seconds. The isolated host peak was 300335104 bytes (about 286.4 MiB); original size was 784264 and tile bytes totaled 70980. This highly compressible fixture includes host overhead and excludes worker validation/database work; it is not a RAM/time upper bound or Linux result. Repeat resource tests with representative inputs on the deployment image.

Tests cover streamed upload rollback, post-commit response loss, interrupted recovery, complete files before ready, stale deletion fencing, original-preserving failure/retry, durable cleanup, every asymmetric zoom and alpha edge/blank cell, native cancellation, private-file exclusion, zero-SQL static GET/HEAD/304, nine-field style DTOs, nine map operations, independent SSE snapshots/heartbeats, and at most two queries per snapshot. A disposable database test confirms the legacy-data guard leaves existing map/source rows and parent data intact.
