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
- Extract structured object types from inline type annotations and generic arguments into named type aliases. If a type is used only by one component, declare it in the same file above the component; extract shared types into an appropriately named file when needed by multiple consumers.
- Use `toPromise` from `@game-atlas/shared/util` to bridge HTTP Observables at TanStack query/mutation boundaries. Pass the query or upload AbortSignal when available; avoid duplicating abort listeners and Promise conversion in options classes.
- Data-access must remain UI-agnostic: do not encode toast, dialog or navigation instructions in mutation meta (for example errorToast), and do not invoke UI services from options. Features own notifications, dialogs, routing and editor reactions through mutation callbacks: onSuccess for successful UI actions and onError for error presentation. Pass per-call callbacks or compose options without overwriting data-access cache reconciliation/rollback.
- Keep query keys and query functions together in handwritten injectable query-options classes. Use generated services as transport and preserve HTTP errors/ProblemDetails. Unsubscribe HTTP requests when a consuming query is cancelled.
- Keep mutation-cache cleanup inside data-access options. Features invoke exposed operation actions and must not obtain MutationCache or remove its entries directly; retry cleanup belongs to the mutation lifecycle.
- Render concurrent pending/error mutation items through TanStack `injectMutationState` filtered by mutationKey. Derive files, destination, status and errors from mutation variables/state; do not mirror them in a separate card/state service. Keep only transport metadata absent from TanStack, such as progress by upload key, in options.
- Update mutation variables contain only the resource ID and patch data, never a previous DTO supplied by the caller. Resolve any optimistic baseline from cache inside data-access and keep rollback snapshots internal. If the baseline is unavailable, skip optimistic changes and invalidate relevant resource queries after success.
- Apply authoritative mutation responses directly to affected query caches when membership, ordering, totals and pagination can be maintained correctly. Do not unconditionally refetch the current listing after every mutation. Use narrowly scoped invalidation when filtered or incomplete results cannot be reconstructed safely.
- Configure QueryClient and runtime API base URLs in consuming applications. The generation URL does not define the deployment URL. Admin development proxies `/api/**` and canonical `/media/files/**` to the backend so page routes such as `/games` remain SPA routes.
- Domain tag ownership: games selects `Games`; maps selects `Maps`, `Category groups`, `Categories` and `Markers`; media selects `Media`. Add clients only when needed.
- Initialize browser-only packages in browser render hooks. Do not introduce a production Node.js rendering server for admin.
- Keep route titles limited to page names. Admin's `AdminTitleStrategy` appends `| GameAtlas Admin` centrally and uses the application name when no page title is available.
- Use template `routerLink` and `queryParams` bindings for navigation controls rather than imperative navigation helpers. Keep programmatic router calls for actions such as guarded dismissal and URL correction.
- Route-backed drawers use named router outlets and CanDeactivate. Pass existing page query data through routerOutletData rather than adding a state service or repeating the query. When changing listing query parameters and closing an outlet together, use one atomic router navigation; read URL state through linkedQueryParam.
- For offset/limit APIs, keep `offset` and `limit` in URL state and pass them directly to transport. Let the pagination UI derive page numbers internally; do not introduce a page query parameter or convert pages in feature/data-access code.
- Use ngxtension `linkedQueryParam` for reactive query-parameter state. Omit default values when serializing and derive normalized request values without rewriting the input signal. Bind standalone URL filters directly; keep route guards authoritative for unsaved editors, browser history and external links. Do not mirror filter state and restore it after rejected navigation.

## Entity identifiers

- Backend entity identifiers and timestamps are authoritative server values. Accept UUID v7 IDs and preserve them unchanged; admin does not create IDs for backend resources.
- Do not derive entity identity from slugs, array indices or incremental counters. Use `crypto.randomUUID()` only for an explicitly browser-owned local identifier, never as a replacement for a server entity ID.

## UI and styling

- Before implementing a feature or UI component, review the existing project UI and the Spartan component catalog for suitable components and composition patterns. Prefer reusing, composing and adapting Spartan components over writing custom equivalents, including empty states, alerts, loading indicators, dialogs and form controls. If a suitable Spartan component has not yet been generated in the project, generate it as part of the implementation without requiring a separate user request. Create custom UI only when the available components do not meet the requirements.
- Use Tailwind CSS v4 for styling. PostCSS is configured in `.postcssrc.json`; shared theme tokens are in `libs/shared/ui/src/theme/styles.css`, registered before application CSS in each app’s `project.json` build `styles` array; do not repeat relative theme imports in application CSS.
- Theme selection uses a head `<meta name="color-scheme">`: `light`, `light dark` for the default/system choice, or `dark`. Use CSS `light-dark()` semantic tokens rather than toggling a `.dark` class. Keep Tailwind dark variants aligned with the meta value and system preference.
- Keep browser chrome hints aligned with the selected theme using two `theme-color` meta tags: white (`#ffffff`) and dark zinc (`#09090b`). In system mode use their `prefers-color-scheme` media queries; for an explicit choice enable only its tag with `all` and disable the other with `not all`. Apply these hints both in the startup script and at runtime. Browser UI coloring is best-effort and depends on browser/platform support.
- Admin persists `game-atlas.theme` (`light`, `system`, `dark`) as a raw string through ngxtension `injectLocalStorage`; the startup script at the beginning of `<body>`, before `<app-root>`, only reads the same key before Angular bootstraps. Keep browser persistence in the application; shared theme controls expose signal inputs/models.
- Use Material Symbols Rounded consistently, including icons inside owned Spartan Helm components. Import SVG icons from `@ng-icons/material-symbols/round` and register only the icons used with `provideIcons`. Do not load external icon fonts or mix icon families. Keep decorative icons hidden from assistive technology and give icon-only controls accessible names.
- Keep owned Spartan Helm code in the single non-buildable `ui-helm` project at `libs/shared/ui`, tagged `type:ui`, `scope:shared`, `platform:shared`. Place Spartan entrypoints under `src/lib/helm` and our own reusable components under `src/lib`, grouped by functionality when they have related files, without an extra `components` directory. Our public API is `src/index.ts` via `@game-atlas/shared/ui`. Use `generateAs: entrypoint` and `componentsPath: libs/shared/ui/src/lib/helm` in `components.json`; add components as entrypoints, not separate Nx projects. Import specific entrypoints (`@spartan-ng/helm/button`, etc.) and keep UI-specific helpers within this project.
- Spartan UI dependencies and theme are configured. `components.json` defines the destination and import alias for any future Helm components.
- Reuse existing project components and library APIs. Generate missing Spartan Helm components when needed for the requested feature, following the configured entrypoint structure in the existing ui-helm project.

## Verification

- Test work is deferred until the user explicitly requests it for a task. Do not add new tests or run unit/E2E suites in the meantime. Use formatting, lint and production builds for verification. Preserve existing test coverage; resume test work only when requested.

## Code Style

Follow the existing Prettier and ESLint configuration.

### Formatting

- Do not use statement-ending semicolons unless required for JavaScript correctness.
- Use single quotes in TypeScript.
- Keep Prettier `htmlWhitespaceSensitivity: 'ignore'` enabled for readable Angular/HTML templates. Keep tag delimiters attached to their tags; never produce hanging `</span` / `><button` fragments or wrap interpolation braces across a tag boundary. Put multiline component content between its opening and closing tag on separate lines. Use CSS gaps for icon/text spacing instead of relying on inter-tag whitespace.
- Separate logical sections with blank lines.
- Add a blank line after a group of variable declarations.
- Add a blank line before `return` when it follows another statement.
- Separate independent control-flow blocks with blank lines.
- Place component metadata `templateUrl` immediately after `imports`. Separate the input/output/model declaration block from other fields with a blank line.
- Put icons and adjacent text on separate template lines; use CSS gaps for spacing.
- Use Spartan tooltips instead of native `title` hover hints. Do not repeat fully visible text in a tooltip. Responsive labelled actions show hints only when their labels are hidden; truncated names or additional error details may still need tooltips. Add `uiTooltipOnOverflow` alongside `hlmTooltip` for text that needs a hint only when clipped. Reuse a primitive's existing tooltip input when it already hosts BrnTooltip; do not apply a second tooltip directive to the same element.
- Use shared `button[ui-button]` / `a[ui-button]` for ordinary action controls. It composes HlmButton, accepts `loading` and optional `icon: IconType`, retains hidden content under the spinner and prevents activation while disabled/loading. Keep specialized menu and sidebar primitives on their own directives.
- Use inline component templates only when they contain at most three lines. Move longer templates into colocated `.html` files through `templateUrl`.
- Separate adjacent properties with a blank line when either declaration spans multiple lines. Keep TanStack query and mutation blocks separate from surrounding fields and from each other. Do not insert blank lines between adjacent single-line declarations within the same query or mutation group (including injectMutationState in the query group). Separate the query and mutation groups from each other and from other field blocks with a blank line. Retain blank lines around multiline declarations.
- Keep Angular template control-flow blocks (`@if`, `@else`, `@for`, `@switch`, `@case`, `@empty`, `@defer`) multiline: opening braces end their own line, content is indented, and closing braces occupy their own line. Separate adjacent independent blocks and surrounding markup with blank lines; keep related `} @else {` clauses together. Never inline a block body.
- Keep a feature's form model, validation and mutations in its component. Do not extract a state service solely to share an editor with its parent or route guard. Use component inputs/outputs and a small route-scoped navigation guard bridge when necessary.
- Pages contribute breadcrumbs and actions to the application header through the shared page-header portal/outlet. Keep the header's layout and theme controls in the application shell.
- Do not add unnecessary blank lines inside empty or trivial blocks.

### Naming

- Place a multi-file component used by only one parent in its own folder inside that parent's functional folder. Keep components shared by multiple parents in a common functional area, each in its own folder when they have multiple files. Do not add a redundant grouping directory to a data-access library when its handwritten files already form one feature; place them directly in src/lib beside generated.
- Group frontend files by functionality. When a component or feature has multiple related files (including templates and tests), colocate them in a dedicated functional folder. Avoid flat resource directories and type-based folders such as `components`, `services` or `models`.
- Name handwritten API option classes `*QueryOptions` and declare query/mutation factories with `CreateQueryOptions` and `CreateMutationOptions`. Consume writes through `injectMutation`; keep Observable-to-Promise conversion inside transport/query or mutation functions. Do not add a separate cache service for TanStack state.
- Preserve Observables for dialog results, confirmations and router guards; subscribe with bounded lifetimes for component actions. Do not convert them to Promises merely to use `async`/`await`. Convert HTTP Observables only at TanStack boundaries that require Promises.
- Make rename and move mutations optimistic when a known item can be updated safely: cancel affected reads, snapshot affected query data, apply the local change, replace it with the authoritative server response on success and roll back on error with a toast. Preserve concurrent successful writes rather than overwriting them with stale snapshots; narrowly invalidate ambiguous filtered, paginated or concurrent results. Wait for server success before removing deleted items from cache because deletion can be refused by reference constraints. Uploads and creations without server IDs stay outside the optimistic policy unless explicitly designed otherwise.
- Use semantic links with `RouterLink` for navigation, including the parent-folder control. Preserve a disabled link's space at root with a null router link and appropriate disabled semantics; keep navigation guards responsible for unsaved edits.
- Format HTTP errors with a descriptive helper such as `getMediaErrorMessage`; use Angular `HttpStatusCode` names for standard HTTP statuses. Status `0` is a transport failure, not an HTTP enum member.
- Shared Helm tooltips use a 500ms touch hold, never touch-origin focus from a short tap. Cancel on release, movement/scroll or pointer cancellation; consuming a recognized hold must prevent accidental control activation. Preserve mouse hover and keyboard focus, and compose the same behavior into specialized tooltip hosts.
- Busy buttons retain their original label/content with `visibility: hidden` and overlay a spinner. Preserve their width and height; do not replace labels with Saving/Moving text.

- Prefix component selectors by their purpose/domain, without encoding the library type: shared UI uses `ui-` (for example, `ui-theme-switcher`); games uses `games-`; maps, including category groups/categories/markers, uses `maps-`; media uses `media-`. Application composition components use `app-`; copied Spartan Helm components retain `hlm-`. Use the same domain prefix for both feature and UI libraries. Keep TypeScript class names descriptive and unprefixed (for example, `ThemeSwitcher`). Match project generator prefixes and ESLint selector rules to these conventions when creating libraries.

- Use kebab-case for frontend source file and directory names, including generated API files (for example, `game-dto.ts` and `games-queries.ts`). Preserve required tool filenames such as `AGENTS.md`, `README.md` and standard configuration names. Keep conventional dot-separated suffixes such as `.spec.ts` and `.service.ts`.
- Name handwritten function and configuration files by their purpose and contents. Do not use generic `helpers` or `utils` filenames.
- Name exported route configuration constants in CONSTANT_CASE (for example `MEDIA_LIBRARY_ROUTES`, `APP_ROUTES` and `SERVER_ROUTES`). Use the conventional `.routes.ts` filename suffix, such as `media-library.routes.ts`; preserve Angular server routing filenames such as `app.routes.server.ts`.
- Set Orval's output-level `namingConvention: 'kebab-case'`; regenerate files rather than renaming generated output manually. This setting controls filenames, not API property names or TypeScript type names.
- Use descriptive camelCase names for variables, functions, and methods.
- Use PascalCase for classes, interfaces, and types.
- Name components that handle both creation and editing with `Editor`, such as `FolderEditorDialog`; use corresponding kebab-case filenames, folders and selectors (`folder-editor-dialog`).
- Do not use Hungarian notation (`strName`, `bEnabled`, `arrItems`, `IUserService`).
- Do not prefix private members with underscores.
- Prefer ECMAScript `#private` members over TypeScript `private`, following the existing ESLint rules.
- Do not use abbreviated names unless they are widely understood.

### Quality

- Prefer readable code over clever or overly compact expressions.
- Use Remeda collection helpers when they make transformations clearer or shorter (for example, `takeLast`, `dropLast` or non-mutating sorting). Do not replace straightforward native operations merely to use the library.
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
5. TanStack query declarations (`injectQuery()`).
6. TanStack mutation declarations (`injectMutation()`).
7. Ordinary properties and variables.
8. Reactive state: signals, signal forms, computed values, linked signals, resources, and signal queries.
9. Constructor.
10. Methods and accessors.

Within each property group, prefer this order:

1. Public `readonly` properties (omit the `public` keyword).
2. Public mutable properties (no access modifier).
3. `protected readonly` properties.
4. `protected` mutable properties.
5. `readonly #private` properties.
6. Mutable `#private` properties.

- Order methods and accessors by visibility: public, protected, then `#private`.
- Declare all plain inject() dependencies before TanStack declarations. Keep injectQuery() declarations before injectMutation() declarations; initialize any URL/source signals they depend on first rather than relying on lazy callbacks to hide initialization ordering.
- Initialization dependencies take precedence over member ordering. Declare a backing field before a public or protected property initialized from it, including transitive dependencies. Never reorder fields in a way that changes initialization behavior.
- Use `protected` for members used only by the component's template; keep members needed by consumers public.
- Keep collection drag-and-drop orchestration in the parent feature using Angular CDK. Presentation cards accept visual drop-state inputs; do not put native DragEvent handlers or move orchestration inside them. Preserve the source slot with a dashed CDK placeholder/anchor, highlight eligible destinations, and retain a menu/dialog alternative.
- Use the component host as its outer container; do not add a redundant wrapper element in the template. Bind container classes, styles, attributes, and event handlers directly through the `host` metadata in `@Component`, rather than `@HostBinding()` or `@HostListener()`. Give the host an appropriate semantic ARIA role for the component's purpose, respecting any native element semantics.
- Inputs, outputs, and models must always be public and `readonly`, including when used in templates. Use the function-based APIs rather than `@Input()` or `@Output()`.
- Keep filter flow explicit: bind controls to signals, derive normalized query values with computed(), and let route guards accept or reject navigation. Do not add copied filter models, manual debounce timers, rejection resets or navigation helpers that secretly write unrelated signals. Show deliberate related updates, such as resetting pagination, at the event binding.
- Keep computed() pure. Do not write signals inside computed(), including through untracked(). Owned UI generators must be adapted to this rule; pagination bounds correction belongs to explicit router navigation, not a hidden model write while rendering.
- Do not use effect() to propagate state between signals or mirror query state with set()/update(). Read TanStack state directly; derive values with computed() and use linkedSignal() for editable state that resets when its source changes. Reserve effects for imperative integrations.
- Register signal effects (`effect()` and `afterRenderEffect()`) in constructors, not field initializers or other methods.
- Prefer `inject()` fields for dependency injection and `@Service()` for ordinary services. Use `@Injectable()` for specialized cases such as constructor-based injection or provider configurations that require it.
- Use the current object configuration for Signal Forms logic, such as `disabled(path, { when: ... })`, `hidden(path, { when: ... })` and `readonly(path, { when: ... })`. Do not use deprecated overloads that accept the condition function directly. Check declarations in the installed Angular version when an overload is uncertain.
- Compose editable controls with `<hlm-field>`, `hlmFieldLabel`, and optional `hlmFieldDescription`. Put validation copy in Signal Forms validators through their `message` option. Our HlmField automatically renders errors from its single projected FormField after it is touched (including a submit attempt); do not repeat error loops or hardcoded validation paragraphs in feature templates. Explicit projected HlmFieldError content suppresses automatic errors. Keep transport/server notifications distinct from field validation.
- Build editor PATCH payloads with shared `getDirtyValues(fields)` using each field's dirty() state; do not hand-compare DTO properties in components. Mark programmatic user edits dirty explicitly, preserve null/empty values, and reset form state after save or discard. The helper selects top-level dirty fields; their object/array values are sent whole. Omit pristine fields and do not send empty patches.
- Submit Signal Forms through `<form [formRoot]="fields">` and the form creation options `submission.action`. Await TanStack `mutateAsync` inside that action so the form owns the full submission lifetime. Use `fields().submitting()` for button loading, control locking and navigation protection; let FormRoot handle preventDefault, validation and duplicate submissions. Do not add manual submit event handlers or duplicate busy/validity guards. Keep successful UI reactions in feature callbacks and derive server errors from mutation state instead of mirroring them with writable signals.
- Use Signal Forms from `@angular/forms/signals` for validated editing forms. Use FormsModule/ngModel for standalone search or filter controls that need no form model; bind directly to their writable signal (for URL filters, linkedQueryParam). Do not introduce ReactiveFormsModule, FormControl, FormGroup or FormBuilder.
- Prettier handles formatting; use standard ESLint rules and plugins for automated checks. Member ordering, public visibility of inputs/outputs/models, template-only visibility, effect placement, and specialized service choices are agent and code-review conventions; do not add custom lint rules for them.
