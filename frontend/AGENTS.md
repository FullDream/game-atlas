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

- Tag each project with a type (`type:app`, `type:feature`, `type:ui`, `type:data-access`, or `type:util`) and a scope (`scope:web`, `scope:editor`, or `scope:shared`). Do not use `type:shell`.
- The future `editor` application should use `type:app` and `scope:editor`. Its libraries use the matching type and `scope:editor`.
- Keep web and editor libraries isolated; put reusable libraries in `scope:shared`. Shared libraries must only depend on other shared libraries.
- Follow the dependency directions in `eslint.config.mjs`; scope and type constraints both apply.
- Prefer non-buildable internal libraries. See `docs/frontend-architecture.md` for the planned structure and tag rules; create libraries only when needed.

## Static web output

- The public web app uses `outputMode: "static"`. Keep prerendering and hydration; do not introduce a production Node.js server or depend on the editor API.
- Initialize browser-only packages with dynamic imports in browser render hooks such as `afterNextRender()`.
- Add future parameterized prerender routes only with their features and a published-manifest-backed `getPrerenderParams()`.
- Catalog JSON is in `apps/web/public/content/games.json`; rebuild on publication to keep prerendered HTML and static JSON consistent.

## Entity identifiers

- Use stable lowercase UUID v4 identifiers for all entities, separate from URL slugs. Preserve existing IDs on content updates and publication.
- New browser-side entities must use `crypto.randomUUID()`. Do not use slugs, indices or incremental integers as identifiers.

## UI and styling

- Use Tailwind CSS v4 for styling. PostCSS is configured in `.postcssrc.json`; global styles and the Spartan theme are in `apps/web/src/styles.css`.
- Spartan UI dependencies and theme are configured. `components.json` defines the destination and import alias for any future Helm components.
- Reuse existing project components and library APIs. Generate Helm components only when explicitly requested.

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

- Use descriptive camelCase names for variables, functions, and methods.
- Use PascalCase for classes, interfaces, and types.
- Do not use Hungarian notation (`strName`, `bEnabled`, `arrItems`, `IUserService`).
- Do not prefix private members with underscores.
- Prefer ECMAScript `#private` members over TypeScript `private`, following the existing ESLint rules.
- Do not use abbreviated names unless they are widely understood.

### Quality

- Prefer readable code over clever or overly compact expressions.
- Keep functions focused and reasonably small.
- Do not introduce unnecessary abstractions.
- Do not add comments that merely restate the code.
- Run Prettier and ESLint on changed files before completing a task.

### Angular classes

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
