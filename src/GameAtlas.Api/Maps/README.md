# Maps

A game owns zero or more independently named maps, including draft games. Each map owns its coordinate space, groups, categories, markers, and optional styles. Metadata and SSE routes start at `/games/{gameId}/maps`; static tiles use `/map-tiles`. Game responses have seven fields and no mapId or embedded maps.

| Method | Suffix | Operation | Success |
| --- | --- | --- | --- |
| GET | empty | ListMaps | 200 complete array |
| GET | /{mapId} | GetMap | 200 complete map |
| GET | /{mapId}/events | StreamMapProgress | 200 text/event-stream |
| POST | empty | CreateMap | 200 complete map |
| POST | /{mapId}/styles | AddMapStyle | 200 complete map |
| POST | /{mapId}/styles/{styleId}/retry | RetryMapStyle | 200 complete style |
| PATCH | /{mapId}/styles/{styleId} | UpdateMapStyle | 200 complete style |
| DELETE | /{mapId}/styles/{styleId} | DeleteMapStyle | empty 204 |
| DELETE | /{mapId} | DeleteMap | empty 204 |

There is no pagination, separate style GET, public source history, manual order/default selection, or PUT operation.

## Creation and listing

POST accepts application/json with required name, width, and height, for example `{"name":"World","width":1025,"height":513}`. Names are trimmed, 1..200 characters and unique per game after invariant case normalization. Dimensions are immutable integers from 1 to 16384. Success returns 200 with geometry, styles=[], and defaultStyleId=null. No files, leases, or processing work are created. Groups/categories/markers can be added immediately.

GET returns every complete map ordered by createdAt then id ascending, including [] for an existing game without maps. There are no pagination/filter/order parameters. Missing games return 404. Every item route requires both the owning game ID and map ID; a sibling or foreign map/style returns 404. Legacy singular-map and game-wide content routes are absent.

## Shared style sources

Upload the original once through `POST /media/assets`, then add a style with JSON: `{"name":"Atlas","backgroundColor":"#fff","sourceAssetId":"<image UUID>"}`. Multipart style requests return 415. Sources must be existing raster JPEG, PNG, WebP, or AVIF images; SVG, videos, and folders return 422, unknown IDs return 404. The first and every subsequent source must exactly match the map's declared dimensions.

Names are trimmed, 1..200 characters, and unique within the map ignoring case. Colors use #RGB/#RRGGBB and return uppercase six-digit notation; they only affect the viewer background. Success returns the complete map with the new style queued. The relationship is committed atomically without copying the original. Multiple styles and maps can share one asset.

Sources are immutable attachments: for a different image, add a new style and delete the old style. Retry only accepts failed styles, retains style/source IDs, resets error/progress, and creates a fresh internal attempt. Queued, processing, and ready styles return 409 for retry. Original bytes are delivered through the common `/media/files/{assetId}` route; the media library owns the 300 MiB upload limit and content inspection.

Shared name/alt/folder changes appear in style and SSE responses without changing style timestamps or triggering processing. Each source includes the same persisted nullable Base64 placeholder as other image responses. Explicit asset deletion returns 409 while persisted references remain. After its final style is deleted, a running native reader does not independently prevent deletion; no file-use leases or media maintenance jobs are created.

## Responses and updates

Map fields: `id`, `gameId`, `name`, `width`, `height`, `extent`, `tileOrigin`, `tileSize`, `minZoom`, `maxZoom`, `resolutions`, `defaultStyleId`, `styles`, `createdAt`, `updatedAt`. Styles are returned in creation order. The default is the earliest surviving ready style; it is null until any style is ready. Queued, processing, and failed styles are skipped.

Style fields: `id`, `name`, `backgroundColor`, `source`, `status`, `progress`, `error`, `tilesUrlTemplate`, `createdAt`, `updatedAt`. Status is `queued`, `processing`, `ready`, or `failed`. Progress is approximate, nondecreasing within an attempt, 0..99 until full publication, and 100 only when ready. Error is null or a safe `{code,message}`; no paths or native diagnostics are exposed. The tile template is null until ready. IDs use UUID v7; dates use UTC.

PATCH accepts `application/json` and `application/merge-patch+json`, including charset, with identical RFC 7396 semantics. Only `name` and `backgroundColor` are optional mutable members. Omission preserves a field; null is invalid. Empty objects return 422. Changes are atomic; normalized no-ops preserve timestamps, and metadata may change while processing without retiling. Unknown/read-only fields and incompatible JSON return 400, other update media 415.

Errors use ProblemDetails: malformed JSON 400; missing/foreign owners 404; duplicate map/style names and invalid retry state 409; streamed byte excess 413; unsupported request media 415; invalid metadata/content/dimensions 422; unexpected pre-response storage errors safe 500. A failed patch or attachment does not persist partial state. Previously admitted unreferenced assets remain in the library.

## OpenLayers geometry and tiles

The image center is (0,0), X right, Y up, one map unit per original pixel. Extent is `[-width/2,-height/2,width/2,height/2]` and tileOrigin is its top-left `[-width/2,height/2]`. Odd dimensions preserve fractional halves. Existing markers are unchanged; finite coordinates outside the image remain valid.

Configure an OpenLayers pixel projection and TileGrid with the returned extent, origin, tileSize=512, resolutions, minZoom=0 and maxZoom. Configure the XYZ source with `tilesUrlTemplate` and `wrapX:false`. Rows increase downward from the origin and columns rightward. Adjacent zooms differ by two; the final resolution is 1. Small images are not upscaled. Partial/blank tiles are transparent 512x512 lossless WebP with no overlap.

Use the returned `tilesUrlTemplate`: `/map-tiles/{mapId:N}/{styleId:N}/{z}/{x}/{y}.webp`. Static-file middleware serves GET/HEAD directly from the dedicated public tile root without Mediator or database queries. Each style's files are immutable, use `Cache-Control: public,max-age=31536000,immutable`, and carry native ETag/Last-Modified validators; conditional requests can return 304. Missing files, malformed paths, and nonexistent coordinates return 404.

Only complete validated pyramids move into the public root atomically. A complete pyramid may be accessible by its generated URL before its ready database commit; metadata/SSE advertise it only after that commit succeeds. Deleted style files may remain accessible until physical cleanup, and browser/proxy caches may retain their bytes longer. There is no retention guarantee or immediate access revocation. Staging, work directories, manifests, and directory listings are not public. Shared originals are public through /media/files/{assetId} without database lookups. Static files have no separate OpenAPI operation; the DTO property documents their contract.

## Server-sent events

Subscribe to `/{mapId}/events`. Empty maps send styles=[] and defaultStyleId=null, keep emitting idle heartbeats, and report styles added later. The first `snapshot` contains `{mapId,defaultStyleId,styles}`; `progress` has the same shape when current state changes, including metadata and default changes. Each subscriber polls independently in fresh read scopes at one-second intervals. A short snapshot transaction keeps the two split queries consistent with concurrent deletion; it is released before any event is yielded. Reconnect receives a fresh snapshot; Last-Event-ID replay is not supported.

An idle `heartbeat` about every 15 seconds contains `{mapId}` and does not fabricate progress. The stream stays open when all styles are terminal. `mapDeleted` contains `{mapId}` and closes the subscription, even if a same-name replacement or a sibling map exists. `error` contains `{mapId,code,message}` and closes after a safe stream failure. Before headers, errors remain normal ProblemDetails.

Viewer disconnects never cancel processing. Disable proxy buffering/compression buffering for this route (`X-Accel-Buffering: no` is emitted), allow long-lived streaming connections, and use proxy idle/read timeouts greater than heartbeat intervals. Avoid caching SSE responses.

## Deletion

Delete any style, including the last in any state, with empty 204. Deleting the default selects the next ready style or null. Removing every style retains the map's geometry, groups, categories, markers, positions, and referenced media.

Deleting the whole map returns 204 and physically cascades to its styles, groups, categories, markers, and attachment rows. Sibling maps and the owning game's fields/timestamps remain unchanged. Repeated deletion returns 404. A new map starts with empty content; it does not relocate or recover deleted markers.

Deletion commits durable cleanup scheduling before 204; disk deletion may follow later and is retried after IO failure/restart. Game deletion schedules cleanup for every owned map while keeping existing relational cascades. Cleanup only removes derived tiles/work. Shared media originals and records survive style, map, and game deletion. See Infrastructure Maps README for storage, migration, recovery and operational constraints.
