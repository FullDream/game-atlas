# Shared UI

Owned Spartan Helm components and UI-specific helpers in one non-buildable Nx project, `ui-helm`, with `type:ui`, `scope:shared`, `platform:shared`.

Helm components use Spartan's Maia style. `components.json` selects `maia` for future CLI additions; existing components contain the copied Maia classes, retaining Material Symbols Rounded and project-specific behavior. The shared semantic color tokens and light/system/dark theme selection apply to both applications.

Import the required entrypoint, for example `@spartan-ng/helm/button` or `@spartan-ng/helm/sidebar`. The root entrypoint deliberately does not re-export every component. Production builds can tree-shake unused exports.

Use `@spartan-ng/helm/date-picker` for ready-made date pickers, composed with the owned Calendar and Popover entrypoints. Prefer importing the specific components needed by a view. These generated entrypoints use Material Symbols Rounded and the workspace Angular conventions.

Use `@spartan-ng/helm/empty` for empty states: compose HlmEmpty, HlmEmptyHeader, optional HlmEmptyMedia, HlmEmptyTitle and HlmEmptyDescription. Media uses it for empty folders and searches with no matches.

`components.json` uses `componentsPath: libs/shared/ui/src/lib/helm`, `generateAs: entrypoint` and `buildable: false`. Future CLI additions belong to this project; do not create a project for each primitive. Keep the standard `ui-helm` project name because Spartan CLI uses it to detect the entrypoint container.

Our component selectors use `ui-` (for example, `<ui-theme-switcher>`); copied Spartan selectors retain `hlm-`. TypeScript class names remain unprefixed.

`ErrorState` (`ui-error-state`) renders a shared error alert with an optional registered icon and default/compact variants. Project heading and description through the `[uiErrorStateHeading]` and `[uiErrorStateDescription]` ng-content slots, using ng-container to retain the component's heading and paragraph semantics. The default slot accepts action buttons and links; consumers own error formatting, loading state and action handlers. Import it from `@game-atlas/shared/ui`.

Group related reusable files under functional folders within `src/lib` (theme, button, page-header and navigation), without a `components` directory. Export their public API from `src/index.ts`, imported as `@game-atlas/shared/ui`. Spartan entrypoints remain isolated under `src/lib/helm`.

Theme tokens live in `src/theme/styles.css`; applications register that stylesheet before their own CSS in the `project.json` build `styles` array. Use Material Symbols Rounded icons and preserve project conventions when adapting copied components.

Run `npm exec nx -- lint ui-helm` from `frontend`; production builds verify consumer compilation. Unit and E2E test work is paused until requested.

`ThemeSwitcher` normally provides a controlled three-option native radio group with arrow-key and Tab navigation. With `compact`, it shows one named active-mode button cycling light, system, dark. Admin centers it in the sidebar footer; mobile retains the radio group. Its `value` model uses the `ThemePreference` union (`light`, `system`, `dark`). It owns no storage; admin owns persistence and the document meta tags.

`PageHeaderOutlet` scopes header content to the workspace. `PageHeaderPortal` contributes a feature template and breadcrumb input; the shell renders its portal through CDK. `WorkspaceBreadcrumbs` uses Spartan overflow menus. `PendingChanges` is a route-scoped leave-check bridge, containing no form model or server data.

Sonner's owned Helm entrypoint supplies application error toasts with Material Symbols Rounded. Features
own toast calls and user-facing error formatting through their mutation callbacks; transport
libraries never depend on UI or encode presentation flags in metadata. Dialog/guard results remain Observables. Inline templates are limited
to three lines; longer templates live alongside their components as HTML files.

Use Button on button[ui-button] or a[ui-button]. It hosts HlmButton and exposes variant, size, disabled, loading and optional icon (IconType). Loading preserves projected content dimensions and prevents activation; navigation controls remain real anchors. Specialized menu/sidebar primitives retain their own directives.

Owned pagination and select entrypoints support numbered URL pagination through HlmNumberedPaginationQueryParams. It accepts offset and limit models and derives page numbers internally. Its page calculation is pure; the listing owns invalid-page URL correction. Navigation links merge existing query parameters and omit offset zero. A named page-size trigger and Material Symbols preserve keyboard/accessibility conventions.

The shared Field component (`<ui-field>`) composes the Helm field primitives. Pass label and description text through its inputs and project one Signal Forms control. It discovers nested FormField controls and renders validation messages centrally after interaction or submission. Keep transport/server notifications separate from field validation.

Use the progress entrypoint through HlmProgress and HlmProgressIndicator. A null value selects Brain/Helm indeterminate behavior. The indicator supports default, muted and destructive variants with reduced-motion support; Brain owns progressbar semantics and accessible numeric values.

ColorPicker supports the optional `clearable` input (disabled by default). When enabled, the popover value input includes a Clear color button and accepts an empty value on blur or Enter. Clearing emits an empty string and keeps the popover open. Enable it only for fields that accept an absent color.
