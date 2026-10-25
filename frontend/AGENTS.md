<!-- nx configuration start-->
<!-- Leave the start & end comments to automatically receive updates. -->

# General Guidelines for working with Nx

- For navigating/exploring the workspace, invoke the `nx-workspace` skill first - it has patterns for querying projects, targets, and dependencies
- When running tasks (for example build, lint, test, e2e, etc.), always prefer running the task through `nx` (i.e. `nx run`, `nx run-many`, `nx affected`) instead of using the underlying tooling directly
- Prefix nx commands with the workspace's package manager (e.g., `pnpm nx build`, `npm exec nx test`) - avoids using globally installed CLI
- You have access to the Nx MCP server and its tools, use them to help the user
- For Nx plugin best practices, check `node_modules/@nx/<plugin>/PLUGIN.md`. Not all plugins have this file - proceed without it if unavailable.
- NEVER guess CLI flags - always check nx_docs or `--help` first when unsure

## Scaffolding & Generators

- For scaffolding tasks (creating apps, libs, project structure, setup), ALWAYS invoke the `nx-generate` skill FIRST before exploring or calling MCP tools

## When to use nx_docs

- USE for: advanced config options, unfamiliar flags, migration guides, plugin configuration, edge cases
- DON'T USE for: basic generator syntax (`nx g @nx/react:app`), standard commands, things you already know
- The `nx-generate` skill handles generator discovery internally - don't call nx_docs just to look up generator syntax

<!-- nx configuration end-->

## Module boundaries

- Tag each project with exactly one type (`type:app`, `type:feature`, `type:ui`, `type:data-access`, or `type:util`), one domain scope, and one platform. Do not use `type:shell`.
- Domain scopes are `scope:games`, `scope:maps`, `scope:media`, and `scope:shared`. Category groups, categories, and markers belong to `scope:maps`. Reserve `scope:app` for application composition roots.
- Platforms are `platform:admin`, `platform:web`, and `platform:shared`. Keep admin and web libraries isolated; platform-shared libraries depend only on other platform-shared libraries.
- Games and maps may depend on media and domain-shared libraries; media depends only on media and domain-shared libraries. Domain-shared libraries depend only on other domain-shared libraries.
- Follow the dependency directions in `eslint.config.mjs`; type, scope and platform constraints all apply. A project missing any dimension has no permission for cross-project imports.
- Prefer non-buildable internal libraries. See `docs/frontend-architecture.md` for the planned structure and tag rules; create libraries only when needed.

## Applications and HTTP data access

- Admin is a browser SPA in `apps/admin`. Web remains a separate statically prerendered neutral entry while its product features are rebuilt. Do not restore the removed static GamesCatalog, its fixture or UUID-v4-only parser.
- Shared backend API libraries use `platform:shared`. The existing `libs/games/data-access` serves both platforms; do not create a parallel admin-specific Games client.
- Generate Angular HttpClient services with Orval into a library-local `src/lib/generated` directory, using tag filtering and `tags-split`. Do not generate TanStack integration or httpResource wrappers.
- Run generation through the library's uncached `generate-api` Nx target. The current input is `http://localhost:5041/openapi/v1.json`; `GAME_ATLAS_OPENAPI_URL` can override it. Keep generated sources in version control. Ordinary builds must not fetch OpenAPI.
- Never edit generated sources manually. Change generator configuration and regenerate; narrow generated-only lint exceptions must not weaken handwritten code or module boundaries.
- Prefer type aliases over interfaces. Generate enum schemas as union types. Add runtime `as const` values only when consumers need them.
- Keep query keys and query functions together in handwritten injectable query-options classes. Use generated services as transport and preserve HTTP errors/ProblemDetails. Unsubscribe HTTP requests when a consuming query is cancelled.
- Configure QueryClient and runtime API base URLs in consuming applications. The generation URL does not define the deployment URL. Admin development proxies only `/api/**` to the backend so page routes such as `/games` remain SPA routes.
- Domain tag ownership: games selects `Games`; maps selects `Maps`, `Category groups`, `Categories` and `Markers`; media selects `Media`. Add clients only when needed.
- Initialize browser-only packages in browser render hooks. Do not introduce a production Node.js rendering server for admin.
- Keep route titles limited to page names. Admin's `AdminTitleStrategy` appends `| GameAtlas Admin` centrally and uses the application name when no page title is available.

## Entity identifiers

- Backend entity identifiers and timestamps are authoritative server values. Accept UUID v7 IDs and preserve them unchanged; admin does not create IDs for backend resources.
- Do not derive entity identity from slugs, array indices or incremental counters. Use `crypto.randomUUID()` only for an explicitly browser-owned local identifier, never as a replacement for a server entity ID.

## UI and styling

- Use Tailwind CSS v4 for styling. PostCSS is configured in `.postcssrc.json`; shared theme tokens are in `libs/shared/ui/src/theme/styles.css`, registered before application CSS in each app’s `project.json` build `styles` array; do not repeat relative theme imports in application CSS.
- Theme selection uses a head `<meta name="color-scheme">`: `light`, `light dark` for the default/system choice, or `dark`. Use CSS `light-dark()` semantic tokens rather than toggling a `.dark` class. Keep Tailwind dark variants aligned with the meta value and system preference.
- Keep browser chrome hints aligned with the selected theme using two `theme-color` meta tags: white (`#ffffff`) and dark zinc (`#09090b`). In system mode use their `prefers-color-scheme` media queries; for an explicit choice enable only its tag with `all` and disable the other with `not all`. Apply these hints both in the startup script and at runtime. Browser UI coloring is best-effort and depends on browser/platform support.
- Admin persists `game-atlas.theme` (`light`, `system`, `dark`) as a raw string through ngxtension `injectLocalStorage`; the startup script at the beginning of `<body>`, before `<app-root>`, only reads the same key before Angular bootstraps. Keep browser persistence in the application; shared theme controls expose signal inputs/models.
- Use Material Symbols Rounded consistently, including icons inside owned Spartan Helm components. Import SVG icons from `@ng-icons/material-symbols/round` and register only the icons used with `provideIcons`. Do not load external icon fonts or mix icon families. Keep decorative icons hidden from assistive technology and give icon-only controls accessible names.
- Keep owned Spartan Helm code in the single non-buildable `ui-helm` project at `libs/shared/ui`, tagged `type:ui`, `scope:shared`, `platform:shared`. Place Spartan entrypoints under `src/lib/helm` and our own reusable components directly under `src/lib`, without an extra `components` directory. Our public API is `src/index.ts` via `@game-atlas/shared/ui`. Use `generateAs: entrypoint` and `componentsPath: libs/shared/ui/src/lib/helm` in `components.json`; add components as entrypoints, not separate Nx projects. Import specific entrypoints (`@spartan-ng/helm/button`, etc.) and keep UI-specific helpers within this project.
- Spartan UI dependencies and theme are configured. `components.json` defines the destination and import alias for any future Helm components.
- Reuse existing project components and library APIs. Generate Helm components only when explicitly requested.

## Verification

- E2E testing is deferred for the current phase. Do not scaffold E2E projects or add/run browser suites unless explicitly requested. Use focused unit tests, lint and production builds.

## Code Style

Follow the existing Prettier and ESLint configuration.

### Formatting

- Do not use statement-ending semicolons unless required for JavaScript correctness.
- Use single quotes in TypeScript.
- Separate logical sections with blank lines.
- Add a blank line after a group of variable declarations.
- Add a blank line before `return` when it follows another statement.
- Separate independent control-flow blocks with blank lines.
- Do not add unnecessary blank lines inside empty or trivial blocks.

### Naming

- Prefix component selectors by their purpose/domain, without encoding the library type: shared UI uses `ui-` (for example, `ui-theme-switcher`); games uses `games-`; maps, including category groups/categories/markers, uses `maps-`; media uses `media-`. Application composition components use `app-`; copied Spartan Helm components retain `hlm-`. Use the same domain prefix for both feature and UI libraries. Keep TypeScript class names descriptive and unprefixed (for example, `ThemeSwitcher`). Match project generator prefixes and ESLint selector rules to these conventions when creating libraries.

- Use kebab-case for frontend source file and directory names, including generated API files (for example, `game-dto.ts` and `games-queries.ts`). Preserve required tool filenames such as `AGENTS.md`, `README.md` and standard configuration names. Keep conventional dot-separated suffixes such as `.spec.ts` and `.service.ts`.
- Set Orval's output-level `namingConvention: 'kebab-case'`; regenerate files rather than renaming generated output manually. This setting controls filenames, not API property names or TypeScript type names.
- Use descriptive camelCase names for variables, functions, and methods.
- Use PascalCase for classes, interfaces, and types.
- Do not use Hungarian notation (`strName`, `bEnabled`, `arrItems`, `IUserService`).
- Do not prefix private members with underscores.
- Prefer ECMAScript `#private` members over TypeScript `private`, following the existing ESLint rules.
- Do not use abbreviated names unless they are widely understood.

### Quality

- Prefer readable code over clever or overly compact expressions.
- Do not use nested ternary expressions. Use explicit `if`/`else` branches or `switch` for multiple conditions.
- Keep functions focused and reasonably small.
- Do not introduce unnecessary abstractions.
- Do not add comments that merely restate the code.
- Run Prettier and ESLint on changed files before completing a task.

### Angular classes

- Angular 22+ uses OnPush change detection by default. Omit redundant `changeDetection: ChangeDetectionStrategy.OnPush` metadata and its import, including in owned Helm components. Explicitly select another strategy only when required by the component; verify the installed Angular version before changing this convention.

Order class members in the following groups:

1. Signal inputs (`input()` and `input.required()`).
2. Outputs (`output()` and `outputFromObservable()`).
3. Model inputs (`model()` and `model.required()`).
4. Injected dependencies (`inject()`).
5. Ordinary properties and variables.
6. Reactive state: signals, signal forms, computed values, linked signals, resources, and signal queries.
7. Constructor.
8. Methods and accessors.

Within each property group, prefer this order:

1. Public `readonly` properties (omit the `public` keyword).
2. Public mutable properties (no access modifier).
3. `protected readonly` properties.
4. `protected` mutable properties.
5. `readonly #private` properties.
6. Mutable `#private` properties.

- Order methods and accessors by visibility: public, protected, then `#private`.
- Initialization dependencies take precedence over member ordering. Declare a backing field before a public or protected property initialized from it, including transitive dependencies. Never reorder fields in a way that changes initialization behavior.
- Use `protected` for members used only by the component's template; keep members needed by consumers public.
- Use the component host as its outer container; do not add a redundant wrapper element in the template. Bind container classes, styles, attributes, and event handlers directly through the `host` metadata in `@Component`, rather than `@HostBinding()` or `@HostListener()`. Give the host an appropriate semantic ARIA role for the component's purpose, respecting any native element semantics.
- Inputs, outputs, and models must always be public and `readonly`, including when used in templates. Use the function-based APIs rather than `@Input()` or `@Output()`.
- Register signal effects (`effect()` and `afterRenderEffect()`) in constructors, not field initializers or other methods.
- Prefer `inject()` fields for dependency injection and `@Service()` for ordinary services. Use `@Injectable()` for specialized cases such as constructor-based injection or provider configurations that require it.
- Use Signal Forms from `@angular/forms/signals`. Do not introduce reactive forms or template-driven forms (`ReactiveFormsModule`, `FormsModule`, `FormControl`, `FormGroup`, `FormBuilder`, or `ngModel`).
- Prettier handles formatting; use standard ESLint rules and plugins for automated checks. Member ordering, public visibility of inputs/outputs/models, template-only visibility, effect placement, and specialized service choices are agent and code-review conventions; do not add custom lint rules for them.
