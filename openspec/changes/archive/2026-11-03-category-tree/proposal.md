# Proposal

## Why

The map editor needs categories beside map styles as an editable two-level tree. Separate group/category reads, mandatory category icons and duplicate numeric orders make that UI and optimistic drag-and-drop awkward.

## What Changes

- Return all map category groups with nested categories, optional shared image icons and computed marker counts through the existing group collection GET.
- **BREAKING** Remove the per-group category collection GET and its ListCategories operation.
- Place group and category operations under one OpenAPI tag, Categories; consolidate backend folders under Categories while preserving separate domain entities, tables and ownership rules.
- **BREAKING** Replace writable numeric order with relative beforeId placement, including atomic same-map category transfers. Keep order as server-owned display order in responses.
- Make category icons optional and add optional group icons with media deletion protection.
- Add category/group creation, metadata editing, confirmed deletion and accessible ordering to the editor's existing left panel.
- Extend the prepared maps data-access categories structure with one tree query and optimistic rename/move reconciliation.

Out of scope: marker authoring/rendering/filtering, public web features, arbitrary-depth nesting, cross-map transfers, real-time collaborative category editing, new module projects, and frontend unit/E2E test work.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `categories`: optional icons, relative placement and transfer, tree-only collection reading, revised OpenAPI operations.
- `category-groups`: nested tree reads, optional icons, aggregate marker counts, relative placement and common Categories tag.
- `admin-maps`: category tree and management dialogs beneath styles in the left panel.
- `frontend-api-clients`: unified category transport, map-scoped tree cache and optimistic mutation lifecycle.

## Impact

Backend Domain/Application/Api category folders, persistence configuration and a forward migration, media reference guards, OpenAPI and focused backend tests are affected. Existing separate CategoryGroups and Categories tables and marker CategoryId references remain.

Frontend maps data-access generation and handwritten categories options, map feature-editor, shared UI composition and Angular Aria dependency are affected. Existing user-prepared query keys, variables and options are the starting point and must be preserved and adapted.

Update owning architecture, API, frontend data-access/UI documents and affected READMEs during implementation. Existing specifications explicitly require mandatory icons, independent lists and duplicate orders; the delta specs replace those rules rather than introducing a parallel API.
