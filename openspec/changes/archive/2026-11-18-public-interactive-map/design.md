## Context

The public map and admin editor share an OpenLayers viewport. Public visitors can record progress locally, while the editor needs draft indicators and pending marker styling.

## Goals / Non-Goals

The public route supports published map browsing and local progress tracking. Editing, account synchronization and offline tile caching are outside its scope.

## Decisions

### Data access and progress

`PublicMapsQueryOptions` uses the generated public map API with `@tanstack/angular-query`. Dexie stores completed marker records in `completedMarkers`, category visibility in `visibleCategories`, group preferences in `groupConfigs` and map settings in `mapConfig`. Live queries expose local changes as Angular signals. Storing the category and map IDs with each completion supports progress reporting.

### Marker presentation

`MapMarker` accepts optional `opacity` and `badges` alongside `draftStates`, `pending` and `failed`. The renderer uses draft states for the indicator dot and tooltip. Explicit opacity applies to both icons and fallback pins. The public adapter assigns 0.35 to completed markers and excludes them when the hide-completed preference is active.

### Layout and filters

The map fills the available height below the persistent 4rem header, which contains theme selection. The toolbar places the filter action before style selection. Filters use a left drawer on desktop and a bottom drawer on mobile. The panel includes map switching, home/game/map breadcrumbs, category search, group and category visibility, marker counts and bulk actions. Style previews resolve a concrete tile URL. Initial style selection prefers the default ready style and falls back to the first ready style.

### Marker inspection

On desktop a Spartan Popover tracks the selected marker's pixel position. Its bounded scrollable body shows the full description and a manual media carousel. The footer offers a completion checkbox. Expanding opens a right drawer with a return-to-popup action.

On mobile the inspector is a bottom drawer at 50dvh with a scrollable body and a completion checkbox in the header. Both drawers show the marker name, category, description and media. The inspector disables the backdrop and outside-pointer dismissal. Selecting another marker updates the details. A ResizeObserver measures the drawer so the viewport can pan the selected marker into the visible map area without changing zoom.

## Risks / Trade-offs

Progress belongs to the current browser's IndexedDB. Loading map details and tiles still depends on the published resources being available.
