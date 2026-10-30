# Code style

Read the section for the language being changed. Formatting and naming configuration takes precedence: [C# EditorConfig](../.editorconfig), [frontend EditorConfig](../frontend/.editorconfig), [Prettier](../frontend/.prettierrc) and [ESLint](../frontend/eslint.config.mjs).

## C#

- Follow the formatting, naming, and code style rules defined in `.editorconfig`. Treat `.editorconfig` as the source of truth.
- Use `camelCase` for private instance and static fields without an underscore (`_`) prefix. Keep constants in PascalCase.
- Follow standard C# naming conventions for constants, non-private static fields, types, methods, properties, and parameters, unless overridden by `.editorconfig`.
- Do not use Hungarian notation or prefixes that encode a variable's type or scope.
- Preserve established .NET naming conventions, including the `I` prefix for interfaces (e.g., `IService`).
- Prefer expression-bodied members for simple, single-expression implementations when consistent with `.editorconfig`.
- Use partial classes only when the type must be split across files, including required generated declarations. Do not introduce partial solely to generate a regex in a domain entity; use a cached regex instead.
- Insert a blank line before `return` statements, except when `return` is the first statement in a block.
- Insert a blank line after a group of local variable declarations before the next statement.
- Separate adjacent fields or properties with a blank line when either declaration spans multiple lines, including attributes, accessors, or an initializer. Compact single-line declarations may remain grouped.
- Insert a blank line after a completed multiline invocation (including fluent chains and calls with block lambdas) before the next statement. Do not insert blank lines inside a continuous invocation chain or before a closing delimiter when no statement follows.
- Separate control-flow statements and blocks (`if`, `for`, `foreach`, `while`, `do`, `switch`, `try`, etc.) from surrounding statements with blank lines. Do not insert blank lines between related clauses such as `if`/`else`, `try`/`catch`/`finally`, or `do`/`while`.

## TypeScript and Angular templates

- Prefer type aliases over interfaces. Generate enum schemas as union types. Add runtime `as const` values only when consumers need them.
- Extract structured object types from inline type annotations and generic arguments into named type aliases. If a type is used only by one component, declare it in the same file above the component; extract shared types into an appropriately named file when needed by multiple consumers.

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
- Use inline component templates only when they contain at most three lines. Move longer templates into colocated `.html` files through `templateUrl`.
- Separate class methods, constructors and accessors from adjacent class members with a blank line, including between two single-line methods.
- Surround every multiline class property declaration with blank lines between adjacent members, whether the line breaks occur in its type, initializer, callback or chained call. Compact single-line properties may remain grouped. Preserve these boundaries after Prettier formatting.
- Separate adjacent properties with a blank line when either declaration spans multiple lines. Keep TanStack query and mutation blocks separate from surrounding fields and from each other. Do not insert blank lines between adjacent single-line declarations within the same query or mutation group (including injectMutationState in the query group). Separate the query and mutation groups from each other and from other field blocks with a blank line. Retain blank lines around multiline declarations.
- Keep Angular template control-flow blocks (`@if`, `@else`, `@for`, `@switch`, `@case`, `@empty`, `@defer`) multiline: opening braces end their own line, content is indented, and closing braces occupy their own line. Separate adjacent independent blocks and surrounding markup with blank lines; keep related `} @else {` clauses together. Never inline a block body.
- Do not add unnecessary blank lines inside empty or trivial blocks.

### Naming

- Prefix component selectors by their purpose/domain, without encoding the library type: shared UI uses `ui-` (for example, `ui-theme-switcher`); games uses `games-`; maps, including category groups/categories/markers, uses `maps-`; media uses `media-`. Application composition components use `app-`; copied Spartan Helm components retain `hlm-`. Use the same domain prefix for both feature and UI libraries. Keep TypeScript class names descriptive and unprefixed (for example, `ThemeSwitcher`). Match project generator prefixes and ESLint selector rules to these conventions when creating libraries.
- Use kebab-case for frontend source file and directory names, including generated API files (for example, `game-dto.ts` and `games-queries.ts`). Preserve required tool filenames such as `AGENTS.md`, `README.md` and standard configuration names. Keep conventional dot-separated suffixes such as `.spec.ts` and `.service.ts`.
- Name handwritten function and configuration files by their purpose and contents. Do not use generic `helpers` or `utils` filenames.
- Name exported route configuration constants in CONSTANT_CASE (for example `MEDIA_LIBRARY_ROUTES`, `APP_ROUTES` and `SERVER_ROUTES`). Use the conventional `.routes.ts` filename suffix, such as `media-library.routes.ts`; preserve Angular server routing filenames such as `app.routes.server.ts`.
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
