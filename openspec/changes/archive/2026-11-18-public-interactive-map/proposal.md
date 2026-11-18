## Why

Players need to explore published maps, filter their markers and keep track of discoveries across visits without signing in.

## What Changes

- Load published maps by game and map slug through the generated Public Maps API and stable TanStack Angular Query.
- Display the interactive map below the public layout header. Provide category filters, map navigation and a default ready tile style.
- Show marker details in an anchored desktop popover, an expanded right drawer or a half-height mobile bottom drawer.
- Store completion and visibility preferences locally with Dexie. Dim completed markers or hide them according to the player's preference.

## Capabilities

### New Capabilities

- `web-map` provides public interactive map navigation, marker inspection, visibility filtering and local completion tracking.

### Modified Capabilities

- `map-viewer` accepts explicit marker opacity while preserving admin draft indicators and pending marker behavior.

## Impact

The web route consumes `libs/maps/feature-public-map`, the shared viewport in `libs/maps/ui` and public query options in `libs/maps/data-access`. Dexie manages client data in IndexedDB.
