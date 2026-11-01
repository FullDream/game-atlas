# Design

## Context

See [proposal.md](proposal.md) for motivation and scope. `Map` currently constructs geometry immediately, rejects differing source dimensions twice during attachment, and computes default from ready styles by creation sequence. `MapMappingExtensions` uses that sequence for presentation. `MapProcessor` claims map dimensions and validates generated manifests against them. `VipsMapImageProcessor` tiles originals without resizing. Angular `MapEngine` uses one map grid for every source; the style form rejects dimension mismatches. Existing backend tests encode these contracts and must change alongside them.

The modified requirements are in [maps](specs/maps/spec.md), [admin-maps](specs/admin-maps/spec.md), and [map-viewer](specs/map-viewer/spec.md).

## Goals / Non-Goals

**Goals:** Separate the logical coordinate frame from raster resolution, retain native detail, preserve existing data, and keep presentation mutations atomic using existing feature handlers and persistence coordination.

**Non-Goals:** Semantic registration of different crops, changing stored marker positions, replacing sources, changing upload admission, or introducing live processing discovery in the admin.

## Decisions

### Initialize geometry in the first attachment transaction

Make persisted Width/Height nullable as a pair; derive nullable geometry only when both exist. Keep the existing flat map response fields, making all geometry fields null while uninitialized. This avoids zero-sized placeholder geometry and unnecessary nesting changes. CreateMap accepts only name and rejects obsolete dimension fields under existing unknown-field handling.

Initialize from the first valid attachment in the same transaction that creates the queued style. Use existing SQLite mutation coordination so simultaneous attachments cannot choose different coordinate frames or allocate colliding order positions. Validation or save failures roll back both initialization and attachment. A later processing failure does not undo initialization. Geometry remains after every style is deleted. Content creation before initialization retains the current finite-coordinate marker rules.

### Tile each source in its own pixel space

Keep NetVips native pyramids rather than resizing derived images to logical dimensions: resizing a later 8000-pixel source onto a 1000-unit map would otherwise discard detail. Pass source dimensions in processing claims and validate decoded dimensions/manifests against the source, not the map. Keep tile URL layout, tile size, transparent padding, lifecycle and cleanup rules unchanged.

Add `tileGeometry` to style DTOs, containing source extent, tile origin, tile size, min/max zoom and resolutions, computed from immutable source dimensions. Logical map geometry continues to describe only the coordinate frame and initial camera scale. Existing equal-sized pyramids already satisfy this format without regenerating files.

### Apply independent affine scales in the viewer

For logical size W,H and source size w,h, map centered source coordinates using `xLogical = xSource * W / w` and `yLogical = ySource * H / h`. Use the inverse for tile sampling. The camera and eventual marker overlays always remain in logical coordinates. Clip to the logical extent so padded tile pixels do not extend the footprint.

Use the installed OpenLayers projection/reprojection facilities with a source-pixel projection per style and explicit forward/inverse transforms to the map projection. The style's native grid belongs to the source projection; the view belongs to the logical projection. A shared TileGrid resolution multiplier alone is insufficient for unequal aspect ratios because it cannot express different X/Y scales. Release style sources and projection registrations owned by the viewer on disposal to avoid obsolete transforms during remounting.

Separate camera zoom limits from the selected source's native zoom limits. Keep camera resolution stable across switches; choose source levels through transformed sampling, allowing high-resolution imagery to supply detail even if the first source was small. Do not clamp to each newly selected style's maxZoom. Retain fit, fullscreen, no-wrap, transparency and SSR behavior. Before geometry exists, do not instantiate the map engine; initialize after receiving the first attachment's geometry.

### Persist order and default independently

Add a display-order value to each style without changing its immutable creation sequence used for processing tie-breaking. New styles append. Persist the effective ready default on the map. When no ready default survives, choose the first ready style in display order during the committed lifecycle/deletion transaction; a later style becoming ready must not displace an existing ready default.

Extend map PATCH with optional `defaultStyleId` and `styleOrder` (the full ordered list of current style IDs), alongside name. Preserve Optional presence handling and both supported merge-patch media types. Reject null presentation values with 422, missing/foreign style IDs with 404, non-ready default or incomplete/duplicate order with 422. Validate the full resulting state before writes. Omission preserves values; equal values preserve timestamps. Only effective map metadata changes update the map timestamp; do not change game/style timestamps or queue processing for presentation updates. Geometry fields remain read-only.

Use one map mutation transaction for combined updates. A stale list whose membership changed is rejected rather than silently losing a concurrent addition. For competing reorders with unchanged membership, the last successfully committed complete order wins, consistent with existing PATCH behavior. Default-derived game/map previews must use the same effective default as complete map responses.

### Frontend interactions and cache reconciliation

Remove dimension inputs from map creation; show uninitialized dimensions as awaiting the first style in lists and dialogs. Remove the style form's exact-size validation and matching-size helper/error text. Compare ratios with integer cross-products `source.width * map.height !== source.height * map.width`, avoiding floating rounding. Warn only when geometry exists and the ratios differ. Explain stretching and distortion near the source picker, without invalidating the form or requiring confirmation.

Add a ready-only Make default card action independent of local viewer selection. Use CDK DropList/Drag with a separate labelled button carrying DragHandle; keep card selection/actions outside the handle. Persist a complete ID permutation on a changed drop. Provide keyboard Move up/down actions because pointer drag alone is insufficient. Disable conflicting presentation/deletion submissions while pending; reconcile the authoritative map and affected map-list/game-summary caches through existing query-options patterns. Failed optimistic ordering rolls back; no-op drops send nothing. No polling or sockets are introduced.

## Risks / Trade-offs

- Different crops or geographic content will not line up merely through stretching. The warning describes distortion; semantic alignment remains outside scope.
- Reprojected tiles can reveal seams or interpolation artifacts at odd boundaries. Validate corners, off-center anchors, transparent edges and fractional extents at several zooms, including unequal aspect ratios.
- Native detail increases renderer complexity compared with normalizing all derived rasters. Keep transforms isolated in maps UI, and verify camera continuity independently from source level selection.
- Null geometry is a breaking contract. Regenerate map clients and update all consumers in the same release; generated-client compilation does not replace browser checks.
- Persisting automatic fallback requires worker publication and deletion to follow the same transactional selection rule. Verify readiness races, deletion and restart paths.

## Migration Plan

1. Add a new migration; do not rewrite prior migrations. Preserve current map dimensions, including maps with zero styles. Backfill display order from creation sequence and persisted defaults from the existing earliest-ready rule.
2. Keep existing ready tile directories and URLs unchanged. New processing validates source geometry; existing source and logical dimensions already coincide.
3. Deploy backend contracts and regenerated frontend consumers together. Apply the migration explicitly using the normal development/deployment workflow, never on API startup. Back up the populated database and generated storage before deployment.
4. Verify a populated-store migration preserves map identities, markers, originals and published files, then verify new name-only maps and different-resolution attachments.
5. Rollback to an old binary is not compatible with uninitialized maps or unequal-sized styles. Restore the coordinated database/storage backup if reverting the release; do not down-migrate by dropping new maps or recalculating marker positions.
