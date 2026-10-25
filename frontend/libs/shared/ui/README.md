# Shared UI

Owned Spartan Helm components and UI-specific helpers in one non-buildable Nx project, `ui-helm`, with `type:ui`, `scope:shared`, `platform:shared`.

Import the required entrypoint, for example `@spartan-ng/helm/button` or `@spartan-ng/helm/sidebar`. The root entrypoint deliberately does not re-export every component. Production builds can tree-shake unused exports.

`components.json` uses `componentsPath: libs/shared/ui/src/lib/helm`, `generateAs: entrypoint` and `buildable: false`. Future CLI additions belong to this project; do not create a project for each primitive. Keep the standard `ui-helm` project name because Spartan CLI uses it to detect the entrypoint container.

Our component selectors use `ui-` (for example, `<ui-theme-switcher>`); copied Spartan selectors retain `hlm-`. TypeScript class names remain unprefixed.

Our own reusable components belong directly under `src/lib`, without a `components` directory. Export their public API from `src/index.ts`, imported as `@game-atlas/shared/ui`. Spartan entrypoints remain isolated under `src/lib/helm`.

Theme tokens live in `src/theme/styles.css`; applications register that stylesheet before their own CSS in the `project.json` build `styles` array. Use Material Symbols Rounded icons and preserve project conventions when adapting copied components.

Run `npm exec nx -- lint ui-helm` from `frontend`. Application unit tests and production builds verify consumer integration.

`ThemeSwitcher` is a controlled three-option native radio group with browser-provided arrow-key and Tab navigation exported from `@game-atlas/shared/ui`; its `value` model uses the `ThemePreference` union (`light`, `system`, `dark`). It owns no browser storage. Theme color tokens use CSS `light-dark()` and Tailwind dark variants follow the consuming document’s color-scheme meta tag. The admin application owns persistence and the meta tag.
