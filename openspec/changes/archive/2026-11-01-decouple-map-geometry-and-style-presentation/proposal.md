# Proposal

## Why

Map creation currently requires manual dimensions and rejects styles with different resolutions. Administrators need interchangeable images of the same territory, stable marker coordinates, and control over style order and the default.

## What Changes

- **BREAKING**: Create maps with a name only; initialize immutable logical geometry when the first style attachment commits.
- **BREAKING**: Represent geometry as absent until initialization; retain existing maps' geometry and content.
- Accept supported library images independently of map dimensions, retaining existing media admission limits.
- Stretch each complete source image over the entire logical extent, including independent horizontal and vertical scaling. Warn in the frontend when aspect ratios differ without blocking submission.
- Persist a selectable ready default independently of style order, and persist order changed through CDK drag and drop with a card drag handle.
- Preserve originals, marker coordinates, and the camera across style changes.
- Out of scope: source replacement, automatic semantic alignment, image cropping, map resizing, marker editor UI, live processing updates, and changes to media upload limits.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `maps`: Deferred logical geometry, independent source tile geometry, persisted default and style order, API and migration compatibility.
- `map-viewer`: Map native-resolution style tiles onto the logical extent while preserving the camera.
- `admin-maps`: Name-only creation, aspect-ratio warning, default action and accessible CDK style ordering.

## Impact

Affected areas include Domain Maps, Application Maps, API map contracts/OpenAPI, EF mappings and a new migration, map processing claims/manifests, Angular maps data-access and generated clients, feature-list, feature-editor, and shared maps UI. Game summary previews must follow the effective default. Existing map/tile data must survive. Existing specifications explicitly requiring equal dimensions, creation order, and no editable default/order are superseded by this change's deltas.
