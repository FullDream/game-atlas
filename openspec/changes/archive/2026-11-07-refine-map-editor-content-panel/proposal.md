# Proposal

## Why

Tall headers and text buttons take up unnecessary space in the map editor's left panel. Expanding several sections divides the workspace, while the desktop panel cannot be hidden and the map cannot be renamed directly in the editor.

## What Changes

- Reduce the four section headers to 40 px and add leading icons.
- Replace style and category/group creation controls with separate icon-only ghost buttons and tooltips; remove the category creation dropdown.
- Allow only one expanded section, initially Map styles, while allowing all sections to be collapsed.
- Add a fourth Settings & Tools section containing Map name and an explicit Save action.
- Add a panel visibility toggle before the style selector, available even without the selector, and preserve the current section when hiding the panel.

- Remove inactive search and filter buttons; adapt selector width so map controls fit on one row on narrow displays.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `admin-maps`: compact headers and actions, single-section expansion, panel visibility and renaming within the editor.

- `map-viewer`: fullscreen can expand a supplied editor region together with its panels.

## Impact

- Frontend: maps/feature-editor, a panel-control projection slot in maps/ui, relevant READMEs and frontend UI guidance.
- Renaming uses the existing MapsQueryOptions.update and PATCH; no new APIs, dependencies or migrations are required.
- Existing modal renaming in the map list remains available.
- Out of scope: import, additional settings/tools, marker behavior changes, public web, panel state persistence across reloads, and unit/E2E tests.
