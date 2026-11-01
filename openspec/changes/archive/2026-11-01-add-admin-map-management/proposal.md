# Proposal

## Why

Administrators can currently see map summaries but cannot manage maps from the game editor. A domain-independent Maps feature should provide the same management experience as Games without coupling their libraries.

## What Changes

- Replace the game editor Maps placeholder with an app-composed Maps list featuring previews, dimensions, style snapshots, creation and modification dates.
- Create maps through a modal; rename them through an edit modal with read-only dimensions; confirm cascading deletion through a dialog.
- Add a name-only map PATCH endpoint with existing merge-patch semantics and complete-map responses.
- Show Add map in the workspace header on Maps and Save changes only on General.
- Keep query results as snapshots without sockets, polling, focus/reconnect refreshes or status-driven refetches; reconcile mutation results in cache.
- Keep existing Games styling and Spartan components. Do not display Main badges.
- Out of scope: map viewer/editor implementation, resizing, style management, live processing, map category/marker counters absent from the current DTO, search, pagination and editable ordering.

## Capabilities

### New Capabilities

- `admin-maps`: Independent administration map listing and modal CRUD inside the game editor.

### Modified Capabilities

- `maps`: Add scoped name-only PATCH and replace the prior explicit exclusion of map metadata PATCH; document the additional operation.
- `admin-games`: Delegate Maps to the independent feature and make header actions depend on the active settings page.

## Impact

Backend Domain Maps, Application Maps, API Maps and focused backend tests; regenerated Maps transport and handwritten query options; new admin Maps feature; Games editor composition and shared header integration; app routes and providers. Existing read and creation contracts stay compatible. No database schema change is expected. Documentation must reflect implemented behavior. Frontend unit/E2E work remains deferred.
