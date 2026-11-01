# Design

## Context

See proposal.md for motivation. Maps already has generated REST transport and MapsQueryOptions.list/detail/create, but no metadata PATCH and no update/delete option factories. Map stores immutable geometry and server timestamps; its normalized name already has game-scoped uniqueness. Games editor currently owns its child routes, wraps their outlet in the game form, and supplies game-specific outlet data. PageHeader currently stores one portal, so overlapping parent and child portals can lose actions or breadcrumbs.

## Goals / Non-Goals

Goals: preserve domain import boundaries and existing game form behavior while composing Maps within the editor; keep HTTP cache reconciliation explicit; extend existing domain and merge-patch patterns.

Non-goals: a generic integration framework, a new state store, new map geometry or processing behavior. See proposal.md for excluded product features.

## Decisions

### App composes sibling domain features

Create libs/maps/feature-list tagged type:feature, scope:maps, platform:admin. It imports maps/data-access and shared libraries only. Export the Games editor shell, its guard and tab components through the Games public API as needed, and assemble its child routes at apps/admin. The maps child receives the route game ID through app route input binding/adapter and does not consume FeatureEditorContext. Remove the obsolete game-maps placeholder. This follows the requested composition boundary; importing Maps from Games or injecting Games options in Maps would violate it.

Move the game form boundary to the General content (or otherwise outside the Maps outlet), retaining its stable form ID and form ownership. A map dialog must never create a nested form or submit Games. Keep unsaved General state while switching tabs and preserve existing leave protection.

### Scoped header actions with inherited context

Extend the existing shared header portal handling narrowly to support an active nested action contribution with inherited parent breadcrumbs/loading and restoration on destroy. The editor remains owner of game context; General contributes Save changes, Maps contributes Add map, and other tabs omit Save changes. Keep cancellation behavior consistent with existing Games. Avoid a cross-domain action service or callbacks from Games into Maps. A single competing parent/child portal was rejected because detach does not currently restore parent content.

### Name-only metadata patch

Add UpdateMap request, operation, validation and domain rename behavior using Optional<string?>. Register PATCH with the existing merge-patch binder. Load the scoped aggregate with styles for the full response. Follow existing map mutation locking, transaction and recognized uniqueness-conflict handling; exclude the current ID from duplicate checks. Trim before comparison; a trimmed identical name is a no-op, while a case-only display change is persisted. Update only map updatedAt for an actual rename. Do not enqueue tiling or introduce a metadata socket event. Existing schema supports this; no migration is expected.

### Query snapshot and mutation ownership

Configure the list consumer with staleTime Infinity, refetchOnMount false, refetchOnWindowFocus false, refetchOnReconnect false and no interval. Fetch once when cache is absent; offer explicit retry on read failure. Do not invalidate this list to discover processing status.

Extend MapsQueryOptions with update and delete options, preserving transport cancellation and cache ownership. Reuse safe authoritative creation merging. Rename uses optimistic cached-name changes with internal rollback and then the complete response; use built-in per-map mutation scopes and cancel obsolete reads so failures do not overwrite later successes. Preserve creation-order sorting. Deletion cancels obsolete reads and removes detail/list data only after server success. No list reconstruction from a missing cache and no post-mutation duplicate GET.

Admin configures a MutationCache onSuccess callback on the application QueryClient. For map create/update/delete mutation keys it invalidates only the exact game-detail query using the Games-owned public key helper. Active game details refetch once to refresh summaries and counts; unsaved editor models remain intact. The Maps list continues to reconcile mutation responses locally, and Games list caches are neither patched nor invalidated. No shared event contract or adapter is needed.

### Reuse administration UI

Use Spartan dialogs, fields, menus, skeleton and empty states, shared Button, ConfirmationDialog, MediaThumbnail, semantic theme colors and Material Symbols Rounded. Colocate map-row/card and map-editor-dialog components within feature-list. Use Signal Forms and preserve input on failed submissions. Display server createdAt/updatedAt with existing date conventions. Main and unsupported statistics are omitted. Styles show source previews and labelled snapshot states; processing indicators must not imply a live subscription. Open editor remains disabled until its destination exists.

## Risks / Trade-offs

- Snapshot statuses can stay stale for the page lifetime -> label status as a snapshot and avoid promises of live completion; real-time integration is deferred.
- Moving form and route ownership can regress guards or dirty values -> explicitly inspect General-to-Maps navigation, reload and return paths during verification.
- Header restoration and cached game summaries span shared/application code -> keep contracts minimal and verify other header consumers still behave correctly.
- Optimistic rollback can overwrite concurrent writes -> use mutation scopes, internal snapshots and preserve newer authoritative results.

## Migration Plan

Deploy the additive PATCH backend first, regenerate the Maps client over its configured HTTP OpenAPI source, then ship the frontend. Do not apply or rewrite migrations. A frontend rollback restores the previous read-only screen; the additive endpoint can remain. Verify affected backend behavior with focused tests, and frontend with formatting, affected Nx lint/typecheck and admin production build. Frontend unit/E2E work remains deferred.
