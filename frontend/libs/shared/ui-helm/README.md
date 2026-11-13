# Shared Helm UI

Each owned Spartan Helm primitive is a separate buildable Nx library under `<component>/`, with `type:ui`, `scope:shared` and `platform:shared` tags. For example, the `button` project lives in `button/` and exports `@spartan-ng/helm/button`; `utils` is also a buildable library. There is no enclosing Nx project. GameAtlas components remain in the sibling [ui library](../ui/README.md), which consumes Helm. Helm libraries do not depend on GameAtlas components.

`components.json` selects `componentsPath: libs/shared/ui-helm`, `generateAs: library`, `buildable: true` and Maia styling. Add future primitives through `npm exec nx -- g @spartan-ng/cli:ui --name=<component>`; default tags are configured in `nx.json`. Local source customizations, Material Symbols Rounded and shared theme tokens in `../ui/src/theme/styles.css` are preserved.

Each library has its own `project.json`, `ng-package.json`, package metadata and TypeScript configuration. Build output goes to `dist/libs/shared/ui-helm/<component>`. Nx builds required Helm dependencies first.

From `frontend/`, run `npm exec nx -- build button` or select all Helm libraries with `npm exec nx -- run-many -t build -p "libs/shared/ui-helm/*"`. Use the corresponding lint targets and consuming application production builds for verification. Unit and E2E test work remains deferred until explicitly requested.
