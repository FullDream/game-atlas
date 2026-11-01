# Map editor

Administration editor at `/games/:gameId/maps/:mapId/edit`, outside game settings tabs and inside the persistent shell. Admin supplies `gameId`, `mapId` and `gameName`. The feature consumes Maps data-access and Maps UI; it does not import Games or Media libraries.

The left panel shows source thumbnails and dimensions. Below the dimensions, unfinished or failed styles show their current server status; the server-derived ready default shows Default in the same slot. Other ready styles have no badge, and ready is never shown as status text. Ready styles can be selected from either panel or viewport. Narrow screens toggle between styles and map. Search and filters are disabled placeholders.

Add and edit open Spartan modal dialogs inside the editor. Creation selects a committed compatible raster image through the shared media picker; editing changes name/background only. Deletion confirms that library media and map content remain. Forms retain values after errors and prevent duplicate submission.

The editor owns a SignalR map-group subscription and displays server progress with accessible processing bars. Queued styles show waiting state; failed cards expose the safe error and Retry processing action with pending and error feedback. Ready styles remain selectable without resetting the camera. The editor shows feedback only when reconnecting or stale, with explicit recovery; a successful connection adds no status message. Reconnect rejoins and reconciles HTTP state; there is no polling or window-focus refresh. Categories, markers, source replacement, publication, minimap and geographic scale remain deferred.

Verify with `npm exec nx -- run maps-feature-editor:lint` and `npm exec nx -- build admin --configuration=production`. Frontend unit/E2E work remains deferred.

Styles accept any supported library resolution. Different aspect ratios show a non-blocking warning that the image stretches over the full logical map. The first style fixes logical dimensions; source thumbnails retain their native dimensions. Ready cards offer Make default without changing the current local selection. A dedicated CDK drag handle reorders cards, with Move up/down menu actions for keyboard use. Order/default changes persist through map PATCH, preserve the camera and roll back on failure; pending actions prevent conflicting submissions.

Cards group the preview and metadata on the left, with the menu and muted drag handle in a right action column. Dragging is vertical and bounded by the style list. The default CDK placeholder retains the card's exact dimensions and shows a dashed slot. A dropped order updates synchronously before asynchronous cache reconciliation, avoiding a return to the old order while saving; failed saves restore the cached order. Drag transitions respect reduced-motion preferences.

Drag handles retain accessible labels without hover popups. Name tooltip directives are removed for the entire list during dragging and restored afterward, closing existing overlays and cancelling pending hover delays so cards underneath the preview cannot open tooltips.

The editor owns the left aside, responsive panel toggle, viewport, live subscription and local style selection. Its MapStyles child owns the style-list header, cards, dialogs, mutations, retry and CDK ordering, with selectedStyleId as the selection model. Drag styles are scoped to the child.
