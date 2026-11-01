# Maps UI

Shared map presentation for administration and public consumers. Import `MapViewport`, `MapGeometry` and `MapViewStyle` from `@game-atlas/maps/ui`. This non-buildable library has no HTTP, route or data-access dependencies.

Bind authoritative pixel geometry to `geometry`, style presentation data to `styles`, and the selected ready style ID through `[(selectedStyleId)]`. The consumer initializes selection from `defaultStyleId` and reconciles it when styles disappear. Only ready styles with tile URLs appear in the selector. Switching source/background preserves the camera; a new map identity resets it. Geometry is immutable for a map identity.

The viewport loads OpenLayers only after browser rendering, observes container size and disposes engine resources on destruction. It supports pan, keyboard navigation, zoom, fit and browser fullscreen. Empty and processing-only maps retain their coordinate space without requesting unavailable tiles. Unsupported fullscreen is disabled. Tile URLs are used unchanged; the host must serve their origin/path.

Each style includes a presentation-only `source` image (`id`, `name`, `url`, `alt`, `placeholder`). The style selector shows its thumbnail in both the trigger and options. Supply `defaultStyleId` to append the default indicator to its label. Image rendering reuses the shared thumbnail and its unavailable-image fallback.

The selector is displayed only when the map has more than one style. With zero or one style, viewport controls remain aligned to the right.

Verify with `npm exec nx -- run maps-ui:lint` and consuming application production builds. Frontend unit/E2E work remains deferred.
