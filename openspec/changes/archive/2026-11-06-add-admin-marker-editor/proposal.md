# Proposal

## Why

The map editor manages styles and categories, but administrators cannot place or edit markers visually. Add the next editing stage using the existing map-scoped marker API and established Spartan UI.

## What Changes

- Render category-styled markers, selection and placement/drag interactions in the shared viewport.
- Add a bottom-centered toolbar ordered Pan, Move marker, Add marker and Add several markers, with two separate creation buttons. Browse/select markers in a separate left-panel accordion instead of a toolbar selector.
- Add one marker form presented in a bottom Drawer on narrow screens and a right Sheet on wide screens, with icon-bearing grouped category selection, X/Y fields, plain-text description and a Spartan photo carousel.
- Support explicit field saves and confirmed deletion; save dragging immediately on release and repeated placements immediately on each click.
- Reconcile marker caches and category/group counts without backend contract changes.
- Out of scope: rich text, slug, notes, extra settings, public application features, clustering/search/filtering, bulk endpoints, marker live collaboration, adding video attachments, and frontend unit/E2E work. Preserve existing video attachments when editing markers.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-maps`: Extend the workspace with adaptive marker editing and mutually exclusive navigation, movement and creation tools.
- `map-viewer`: Add transport-independent marker rendering, hit selection, placement and movement events while preserving camera and browser lifecycle guarantees.

## Impact

- Existing maps/feature-editor, maps/ui and maps/data-access libraries; shared UI Helm Drawer, Sheet and Carousel entrypoints as needed.
- Existing generated MarkersService and shared image-picker port; no generated client edits or backend/schema changes planned.
- Frontend architecture/UI documentation and library READMEs updated during implementation to remove the marker-editor deferral.
