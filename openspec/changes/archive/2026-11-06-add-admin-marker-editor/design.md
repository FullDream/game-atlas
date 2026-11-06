# Design

## Context

See proposal.md for motivation. The existing feature-editor composes MapStyles, MapCategories and a transport-independent OpenLayers MapViewport/MapEngine. MarkersService already provides map-scoped list/create/update/delete and full marker DTOs; handwritten marker query options do not yet exist. The marker contract supports finite logical X/Y, nullable plain text and ordered library image/video IDs. The shared media picker currently selects images only. Existing admin-maps explicitly defers marker editing; its workspace requirement is modified by this change.

## Goals / Non-Goals

Goals: reuse existing libraries and API contracts, keep persistence outside the viewport, share one draft across adaptive shells, and make each immediate write independent and recoverable.

Non-goals: new projects, parallel resource stores, backend changes, server batch/idempotency endpoints, generalized editor frameworks, video picker expansion or marker SignalR events.

## Decisions

### Ownership and state

Extend maps/ui presentation types and MapEngine with an OpenLayers vector layer and mode-specific interaction callbacks. MapViewport accepts rendered marker data and interaction mode and emits identity/coordinates; it never imports data-access. Keep marker feature components colocated in maps/feature-editor, including form, adaptive shell and toolbar. Add MarkersQueryOptions and scoped query keys to maps/data-access using generated transport and the existing toPromise bridge. Prefer this over a new feature library or global editor service because only this editor consumes the workflow.

TanStack owns persisted marker state. Component signals own selected ID, active mode, draft/template and panel state. Pending/error placements derive from keyed mutation state and variables rather than a separate resource store. Scope all keys and callbacks to game/map; switching map clears local selection/template and rejects late UI effects while successful requests may reconcile only their original cache.

### Adaptive form shells

Use a bottom Spartan Drawer below the existing md breakpoint and right Spartan Sheet above it. One form component and draft live outside shell instances so breakpoint changes cannot recreate or reset the draft. Use normal dialog semantics, focus management and return focus to the invoking control; the named map toolbar is the fallback when a canvas point cannot receive focus. While the modal form is open, map gestures are inactive. Close it before placement or movement instead of weakening modal accessibility to allow background gestures. Reuse owned Helm components, semantic theme tokens, Material Symbols Rounded and shared ui-button. Inspect installed components and generate missing Drawer/Sheet/Carousel entrypoints in shared UI during apply.

A single dirty-draft guard handles close, swipe, backdrop, Escape, tool changes, selection and route departure. Breakpoint adaptation is not dismissal. Keep the form body scrollable with visible actions and safe-area spacing. Repeated-template form omits position because each click supplies it. X/Y in the marker form are editable finite numbers without geographic or map-bound constraints. Before geometry exists map placement is disabled with first-style guidance; initialized maps with no ready tiles remain editable.

Use the native Spartan Drawer drag handle and swipe-to-dismiss behavior. Do not add snap points, upward swipe expansion, fullscreen mode or expand/collapse controls in this stage. Evaluate those only after the initial implementation is reviewed. Native swipe dismissal remains subject to the dirty-draft guard.

### Selected marker visibility on mobile

Measure the actual intersection of the mobile Drawer and map viewport, reserving an uncovered map strip large enough for the complete selected marker and padding. After opening/layout settlement and on drawer-height or orientation changes, the feature passes the unobscured viewport rectangle and selected marker to a transport-independent reveal operation in maps/ui. Account for the marker icon bounds, not only its anchor. Pan by the minimum screen-space displacement required, preserving resolution and coordinates. If it already fits, do nothing. This explicit reveal is the exception to ordinary camera preservation; it never emits a marker-move event or PATCH. Do not restore the prior camera on drawer close, avoiding a second unexpected jump. Respect reduced-motion preferences and avoid resize/animation feedback loops.

### Toolbar and gestures

Order Pan, Move marker, separator, Add marker and Add several markers. Use separate creation buttons; do not include a marker selector or placement dropdown in the toolbar. Use a mutually exclusive accessible control group with active-state labels/tooltips; default Pan. Fixed positioning is relative to the map area, not the page. Controls stop map gesture propagation. Existing search/filter placeholders remain unchanged.

Pan selects a marker and opens its form. Single Add clicks first, opens the form and creates only on explicit submit; success returns to Pan and selects the persisted point. Repeated Add edits a template first, then closes the form and displays template/category, success count and Finish above the toolbar. Each intentional click sends one create using a snapshot of the template and position. Esc/Finish changes future input only; already submitted requests settle and reconcile normally. Drag gestures never count as placements.

Move uses hit-tested marker translation, with empty-space pan preserved; release emits one position change. Keep the pending marker at its candidate location and block repeat movement and conflicting edits/deletion for that marker until settlement. Success uses the returned DTO; failure restores prior coordinates with an error. Cancelled or equal-position gestures send nothing. Provide keyboard-reachable rows in a separate left Markers accordion tied to the same editor and X/Y fields as the alternative to canvas selection and dragging.

### Mutations and cache reconciliation

Read the full map marker array once through the scoped query. Upsert authoritative create/update responses in createdAt/id order; remove successful deletions. Cancel stale scoped reads before cache edits so old responses cannot resurrect removed items or overwrite accepted updates. Serialize conflicting operations per marker using built-in mutation scopes and feature pending guards, allowing independent repeated creates. Avoid whole-list optimistic rollback for concurrent work; movement preview is scoped to one marker.

Invalidate the exact category-tree query after successful create/delete/category transfer to obtain authoritative category/group counts. Reconcile marker-list membership after category/group cascades through existing category mutation options; close a selected marker only when its ownership is removed. Do not invalidate Games or Media domains. Category icon/color changes flow from the current tree into presentation without changing marker DTOs.

Disable automatic mutation retries. Definitive failed placements expose explicit retry with original variables. A transport failure can follow a committed POST: refresh the scoped list and present an uncertain outcome for inspection; matching duplicate-allowed fields cannot prove identity, so never claim exact deduplication or silently retry. Keep uncertain positions visible until resolved/dismissed by the operator. The same no-automatic-POST-retry policy applies to single-create submissions.

### Category and media fields

Use Spartan Select grouped by category groups, rendering image or colored fallback icon plus name both in options and selected value. Show accessible empty/loading/error guidance; no creation without a valid current-map category.

Use the existing shared image picker port and a manual Spartan Carousel with no autoplay. Repeated picker selections append unique IDs. Provide detach and move earlier/later controls, including keyboard operation. Preserve a complete ordered attachment draft: existing videos remain visible as labelled preview/items and keep their relative order, image reordering swaps image slots without dropping videos, new images append. Send the whole ordered ID array only when media changes; unrelated saves omit media. This avoids expanding the image-only picker or silently destroying valid backend data.

## Risks / Trade-offs

- Modal sheets temporarily block the map -> finish field editing before entering map gestures; retain shared draft across shells.
- Concurrent operations and stale reads -> scoped cancellation, authoritative upserts and per-marker guards; no whole-array rollback.
- POST outcome uncertainty without idempotency keys -> no automatic retries, scoped reconciliation and explicit unresolved-state guidance.
- Canvas accessibility -> keyboard-reachable marker selection and numeric coordinate editing; labelled toolbar and carousel controls.
- Large marker lists -> reuse vector features by ID and avoid rebuilding the map; clustering remains separate future work.

## Migration Plan

No storage or API migration. Deploy the frontend with existing backend contracts. Reverting the frontend restores the previous editor while persisted markers and shared assets remain intact. Update affected frontend documentation when implementation lands. Verify with formatting, affected Nx lint targets, admin production build and manual interaction scenarios; frontend unit/E2E suites remain deferred.

The Markers accordion starts open, shares remaining height with styles/categories and owns an independent scroller. It reuses the scoped marker query and current category presentation for icons and secondary category labels. Its loading/error/empty states are local. Row selection closes the mobile content panel before showing the editor; modal closure returns focus to the stable map toolbar.

Configure OpenLayers' default interactions explicitly with onFocusOnly: false; the Map constructor otherwise overrides the interaction factory default with focus-only wheel/pan behavior. Preserve the focusable canvas and normal keyboard focus conditions; do not autofocus on initialization or hover.
