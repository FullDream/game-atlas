# Maps UI

Shared map presentation for administration and public consumers. Import `MapViewport`, `MapGeometry` and `MapViewStyle` from `@game-atlas/maps/ui`. This non-buildable library has no HTTP, route or data-access dependencies.

Bind authoritative pixel geometry to `geometry`, style presentation data to `styles`, and the selected ready style ID through `[(selectedStyleId)]`. The consumer initializes selection from `defaultStyleId` and reconciles it when styles disappear. Only ready styles with tile URLs appear in the selector. Switching source/background preserves the camera; a new map identity resets it. Geometry is absent before first attachment and immutable after initialization. The engine initializes only when geometry becomes available.

The viewport loads OpenLayers only after browser rendering, observes container size and disposes engine resources on destruction. It supports pan, keyboard navigation, zoom, fit and browser fullscreen. Uninitialized maps show Awaiting first style. Initialized empty and processing-only maps retain their coordinate space without requesting unavailable tiles. Unsupported fullscreen is disabled. Tile URLs are used unchanged; the host must serve their origin/path.

Pointer dragging and wheel zoom work immediately without a preliminary activation click (`onFocusOnly: false`). The map does not take focus automatically; keyboard controls remain available when the map receives focus through normal keyboard or pointer navigation.

Each style includes a presentation-only `source` image (`id`, `name`, `url`, `alt`, `placeholder`). The style selector shows its thumbnail in both the trigger and options. Supply `defaultStyleId` to append the default indicator to its label. Image rendering reuses the shared thumbnail and its unavailable-image fallback.

The selector is displayed only when the map has more than one style. With zero or one style, viewport controls remain aligned to the right.

Verify with `npm exec nx -- run maps-ui:lint` and consuming application production builds. Frontend unit/E2E work remains deferred.

Each style also supplies native tileGeometry. Source-pixel affine transforms independently scale X and Y into the logical extent, including unequal aspect ratios; native pyramids retain larger images' detail. Camera zoom constraints are independent of the selected source, so switching styles retains center/resolution. Transform registrations are scoped to the engine and removed when styles change or the engine is disposed.

## Markers

`CategoryIcon` (`maps-category-icon`) renders category and marker icons in UI panels. Pass `iconUrl` and an optional nullable `color`. With a color, the image supplies an alpha CSS mask and the category color supplies its fill. With no color (`null` or omitted), the original image is displayed with its own colors and transparency. With no URL, it uses the filled Material Symbols location pin. The host is decorative, defaults to `size-5` and accepts sizing classes. Remote icon URLs must allow cross-origin mask loading. Opaque images produce a solid silhouette; use transparent icon assets. OpenLayers uses the same alpha-mask coloring through Canvas `source-in`, preserving image proportions. A marker with `color: null` renders the original image without recoloring or a contour; the consumer must preserve null instead of substituting a default color. Masked canvases are cached by URL and color for the viewport lifetime. While loading or after an image error, a colored location pin remains visible. Markers render only their colored icon, with no circular backing; selection enlarges the icon, pending icons fade and failed placements use red. For colored icons, a crisp contour of approximately 0.75 CSS pixels uses black for light category colors and white for dark colors, chosen by relative luminance. It follows the alpha silhouette without blur or offset and is cached with each icon. Late image loads cannot overwrite a changed or removed marker.

Bind transport-independent `MapMarker[]` to `markers` and `MapTool` to `tool`. The vector layer preserves camera/tiles while points, categories or selection change. `markerSelected`, `markerPlaced` and `markerMoved` emit IDs or logical X/Y; only completed changed drags emit movement. Empty-space drag still pans in Move mode. `ready()` signals browser-engine initialization. Resources and callbacks are released on teardown or map changes.

Hover enlarges interactive marker icons and raises them above neighboring markers. Pan mode shows a pointer cursor; Move shows a grab cursor over markers and Add uses a crosshair without marker hover. Pending markers and touch pointers do not activate hover. Leaving the viewport, dragging, changing tools or removing a hovered marker clears the hover state.

Hovered markers show their plain-text name in an OpenLayers overlay using the shared Helm tooltip styles, since Canvas markers have no DOM tooltip trigger. The tooltip does not intercept pointer events, adjusts its alignment near map edges and closes with Escape or when hover clears. Starting a camera movement also clears hover and the tooltip.

`revealMarker(id, region)` accepts an unobscured pixel rectangle relative to the viewport. This explicit operation minimally pans a hidden selected point into the rectangle with padding, preserving zoom and marker coordinates; ordinary data/style updates preserve the camera. Use `getBounds()` to translate overlay bounds. This is used above the mobile marker Drawer and does not emit a persistence event.
