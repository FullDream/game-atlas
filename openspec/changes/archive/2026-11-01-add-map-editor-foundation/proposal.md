# Proposal

## Why

Administrators can create maps but cannot open their tile imagery or manage styles in an editor. A shared map renderer is needed now so administration and the future public viewer use the same geometry and engine integration.

## What Changes

- Introduce a reusable OpenLayers map viewport with pixel geometry, ready-style selection, pan, zoom, fit and fullscreen controls.
- Add a dedicated administration editor with a left style panel and a central map, following the supplied sketch and existing Spartan UI conventions.
- Connect style creation from library media, metadata editing and confirmed deletion to existing REST endpoints.
- Show status snapshots without percentages, progress bars, sockets, polling or automatic focus/reconnect refreshes.
- Render search and filter controls as explicitly unavailable placeholders.
- Exclude category/marker editing, the right details panel, publication workflows, minimap, geographic scale, style reordering/default editing, source replacement and public-page implementation.

## Capabilities

### New Capabilities

- `map-viewer`: Shared rendering of aligned game-map tile styles and viewport controls.

### Modified Capabilities

- `admin-maps`: Enable editor navigation and add style management within a responsive editor while retaining snapshot status behavior.

## Impact

- Frontend: new non-buildable maps UI and administration editor libraries; existing maps data-access, maps list, admin routing and tile proxy configuration.
- Dependency: direct `ol` integration behind our Angular component; no additional Angular mapping wrapper.
- Reuse shared media picker through its existing adapter/port and Spartan Helm entrypoints.
- Existing map/style REST contracts and backend tile generation remain unchanged.
- Update affected frontend documentation. Verification uses formatting, lint and production builds; unit/E2E work remains deferred.
