# Verification

Validated on 2026-11-01. Implementation tasks are tracked in [tasks.md](tasks.md).

## Backend

- `dotnet build GameAtlas.slnx --no-restore --verbosity quiet` passed. Existing nullable warnings remain in OptionalJsonConverter and CategoryGroupApiTests.
- `dotnet test tests/GameAtlas.Domain.Tests --no-restore --filter "FullyQualifiedName~Map" --verbosity quiet`: 42 passed.
- `dotnet test tests/GameAtlas.Application.Tests --no-restore --filter "FullyQualifiedName~Map" --verbosity quiet`: 4 passed.
- Integration filter `FullyQualifiedName~Map|FullyQualifiedName~GameMetadataApiTests|FullyQualifiedName~CategoryIconMigration`: 152 passed. It includes the populated migration, geometry initialization, processor, source preservation, ownership, HTTP/OpenAPI, and default/order cases.
- After allowing nullable order elements in the request schema and rejecting them with 422, the presentation/update subset passed all 17 tests.
- After simplifying the name-only map fixture, the final integration filter `FullyQualifiedName~MapApiTests|FullyQualifiedName~AddMapStyleApiTests|FullyQualifiedName~MapPresentationTests|FullyQualifiedName~GameMetadataApiTests|FullyQualifiedName~MediaAcceptanceTests` passed all 48 tests.
- The populated migration test retains odd dimensions, an initialized styleless map, source references, ready default, creation-derived order, marker coordinates and an existing tile file. Rollback against a populated store is refused before altering it.
- Native processing checks exercise 1000-to-8000 and 8000-to-1000 source widths, with distinct heights, source-resolution maximum zoom and the last native tile. Source images are never normalized to the first style's raster resolution.

## Frontend

Commands below ran from `frontend/`:

- `npm exec nx -- run maps-data-access:generate-api` regenerated contracts explicitly from the updated local API, including nullable geometry, style tile geometry and presentation PATCH fields.
- Prettier formatted changed handwritten TypeScript/templates and Markdown; generated clients used their configured post-generation formatter.
- `npm exec nx -- run-many -t lint -p maps-data-access maps-feature-editor maps-feature-list maps-ui admin games-data-access --output-style=static`: all six projects passed.
- `npm exec nx -- build admin --configuration=production --output-style=static`: passed.
- `npm exec nx -- build web --configuration=production --output-style=static`: passed using the normal Nx cache; the public app currently does not consume the map viewer.

A temporary browser driver exercised the production admin build in headless Edge. This was an ad hoc browser acceptance check; no frontend unit/E2E suite was added or run. Browser HTTP fixtures supplied map snapshots and colored native tile images; actual API persistence and native WebP processing were verified separately by the backend integration tests above.

Browser checks passed for:

- 1000x500 logical geometry with an 8000x8000 style, the reverse resolution order, and odd logical dimensions 1025x513.
- Identical visible image boundaries across styles and matching four colored control points; pan/zoom followed by switching retained the viewport and off-center image anchors. Fit and fullscreen toggling completed without runtime errors.
- Explicit default changes, persistence on reload and independent local style selection.
- Keyboard menu ordering and pointer drag starting at the separate handle, including persistence on reload and rollback after a simulated failed reorder.
- Aspect ratio warnings for square imagery on rectangular maps, disappearance for equal-ratio images of another resolution, and no warning before first initialization. The warning did not disable the otherwise valid form.
- An initialized map with no styles retained its OpenLayers viewport; an uninitialized map showed Awaiting first style without creating an engine.
- Name-only map creation without numeric inputs, rename and reload of the uninitialized map.
- No JavaScript runtime errors during these checks.

Marker editor UI remains out of scope. Stored marker positions were checked through integration tests, not rendered marker overlays. No existing application database migration was applied during implementation; migration tests used isolated temporary databases.

## Final checks

`openspec validate decouple-map-geometry-and-style-presentation --strict`, `node scripts/check-docs.mjs`, and `git diff --check` passed. Temporary browser/build/test output files were removed from the workspace.
