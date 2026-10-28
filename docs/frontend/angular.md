# Angular conventions

Applies to handwritten Angular code and owned Helm components. Read the [TypeScript and template style](../styleguide.md#typescript-and-angular-templates). Paths are relative to `frontend/`.

## Routing and component state

- Initialize browser-only packages in browser render hooks. Do not introduce a production Node.js rendering server for admin.
- Keep route titles limited to page names. Admin's `AdminTitleStrategy` appends `| GameAtlas Admin` centrally and uses the application name when no page title is available.
- Use template `routerLink` and `queryParams` bindings for navigation controls rather than imperative navigation helpers. Keep programmatic router calls for actions such as guarded dismissal and URL correction.
- Route-backed drawers use named router outlets and CanDeactivate. Pass existing page query data through routerOutletData rather than adding a state service or repeating the query. When changing listing query parameters and closing an outlet together, use one atomic router navigation; read URL state through linkedQueryParam.
- For offset/limit APIs, keep `offset` and `limit` in URL state and pass them directly to transport. Let the pagination UI derive page numbers internally; do not introduce a page query parameter or convert pages in feature/data-access code.
- Use ngxtension `linkedQueryParam` for reactive query-parameter state. Omit default values when serializing and derive normalized request values without rewriting the input signal. Bind standalone URL filters directly; keep route guards authoritative for unsaved editors, browser history and external links. Do not mirror filter state and restore it after rejected navigation.
- Keep a feature's form model, validation and mutations in its component. Do not extract a state service solely to share an editor with its parent or route guard. Use component inputs/outputs and a small route-scoped navigation guard bridge when necessary.
- Preserve Observables for dialog results, confirmations and router guards; subscribe with bounded lifetimes for component actions. Do not convert them to Promises merely to use `async`/`await`. Convert HTTP Observables only at TanStack boundaries that require Promises.
- Use semantic links with `RouterLink` for navigation, including the parent-folder control. Preserve a disabled link's space at root with a null router link and appropriate disabled semantics; keep navigation guards responsible for unsaved edits.

## Angular classes

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
