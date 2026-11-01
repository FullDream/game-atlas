# Map editor

Administration editor at `/games/:gameId/maps/:mapId/edit`, outside game settings tabs and inside the persistent shell. Admin supplies `gameId`, `mapId` and `gameName`. The feature consumes Maps data-access and Maps UI; it does not import Games or Media libraries.

The left panel shows source thumbnails and dimensions. Below the dimensions, unfinished or failed styles show their last-loaded status; the server-derived ready default shows Default in the same slot. Other ready styles have no badge, and ready is never shown as status text. Ready styles can be selected from either panel or viewport. Narrow screens toggle between styles and map. Search and filters are disabled placeholders.

Add and edit open Spartan modal dialogs inside the editor. Creation selects a committed compatible raster image through the shared media picker; editing changes name/background only. Deletion confirms that library media and map content remain. Forms retain values after errors and prevent duplicate submission.

Status is an HTTP snapshot without sockets, polling, focus/reconnect refreshes, percentages or progress bars. Returning to the editor loads current metadata. Categories, markers, source replacement, processing retry, publication, minimap and geographic scale remain deferred.

Verify with `npm exec nx -- run maps-feature-editor:lint` and `npm exec nx -- build admin --configuration=production`. Frontend unit/E2E work remains deferred.
