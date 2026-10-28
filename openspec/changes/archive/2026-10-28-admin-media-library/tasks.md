# Tasks

## 1. Backend folder context

- [x] 1.1 Use PaginatedResult<MediaItemDto> directly and add separate folder-details/breadcrumb DTO declarations; verify Application builds without changing Games pagination.
- [x] 1.2 Add GetMediaFolder query/handler and GET endpoint with current Folder and ordered Ancestors; keep lists on the shared pagination contract. Further tests are deferred by user instruction.
- [x] 1.3 Update API README and regenerate the Media client for separate folder context and list contracts. Further test updates are deferred.

## 2. Media transport and cache

- [x] 2.1 Add shared media/data-access with the required three Nx tags, public alias and uncached Media-only Orval target matching Games conventions; verify lint boundaries and regeneration produces only Media operations with committed kebab-case type aliases.
- [x] 2.2 Add handwritten normalized Media query options, runtime base URL and cancellation/error preservation; verify HTTP tests cover folder/search/pagination, ProblemDetails, cancellation and file URL resolution through the configured API prefix.
- [x] 2.3 Add mutation functions and focused cache reconciliation for asset edits, folder changes, upload and deletion; verify unit tests prove alt/cover edits do not trigger list requests, previews update, responses do not duplicate IDs, and uncertain search/order/page boundaries invalidate only affected keys.
- [x] 2.4 Configure upload event observation and the admin XHR transport without editing generated code; verify multipart fields, upload progress events, final response handling and failed generation diagnostics with transport tests and two stable regeneration runs.
- [x] 2.5 Document Media generation, runtime addressing and the cache-update/scoped-invalidation policy in frontend documentation; verify ordinary builds use committed clients without fetching OpenAPI.

## 3. Browser and navigation

- [x] 3.1 Add the admin-only media feature-library and lazy route replacing only the Media stub; verify Nx dependency lint and shell tests retain Games, root redirect, title strategy and active navigation.
- [x] 3.2 Implement URL-backed folder/search/offset/limit state, server breadcrumbs, 24-item pagination and page correction after removal; verify focused routing tests cover direct links, search/folder reset, invalid values and browser history.
- [x] 3.3 Implement mixed cards and loading/empty/no-match/missing-folder/request-error states using existing theme/icons/primitives and NgOptimizedImage for committed images/cover thumbnails; verify component tests cover API Base64 placeholders, null-placeholder disabling, constrained fill/dimensions, safe SVG image rendering, unavailable delivery and retry. Keep the default loader; defer custom loaders and backend size variants.
- [x] 3.4 Implement the responsive preview/details panel and explicit-save Signal Forms for name, image alt and video cover fields; verify read-only/unchanged fields are omitted, server validation retains drafts and successful response resets the baseline.

## 4. Upload lifecycle

- [x] 4.1 Add memory-only single-file upload operations and immediate destination-scoped temporary cards, separate from server pagination; verify tests cover transfer percentage, processing after transfer, disabled editing and reconciliation without duplicates.
- [x] 4.2 Keep upload operations alive across folder navigation and add explicit retry/removal; verify retained destination/File, no automatic POST retry, unrelated-folder visibility and resource/object-URL disposal with service tests.
- [x] 4.3 Document reload/disposal and lost-response duplicate limitations alongside upload behavior; verify no unload warning, persistence, queue or new backend recovery infrastructure is introduced.

## 5. Organization and edit protection

- [x] 5.1 Implement folder creation/rename and a breadcrumb-based move dialog for files/folders with root selection and self/descendant exclusion; verify tests cover successful moves, sibling conflicts, missing destinations and authoritative backend failures.
- [x] 5.2 Implement the folder-aware single-image video-cover picker and clear action; verify non-images cannot be selected, mixed-page navigation remains available and clearing sends explicit null without deleting assets.
- [x] 5.3 Implement the initial one-item confirmed deletion and verify server conflicts, 204 and direct counts/totals/pages. Deletion remains server-confirmed; further test work is deferred by user instruction.
- [x] 5.4 Implement shared dirty-draft confirmation for editor actions, same-route location changes and CanDeactivate; verify Continue editing cancels each action, Discard proceeds, query-parameter history cannot bypass protection and beforeunload is absent.
- [x] 5.5 Verify responsive panel/dialog behavior, accessible names, keyboard activation, focus restoration and announced operation states with focused component tests; document any manual viewport/theme checks without adding E2E infrastructure.

## 6. Integrated verification

- [x] 6.1 Run affected backend tests and frontend Nx unit tests/lint plus admin and web production builds; verify no module-boundary regressions, generated edits or unrelated web feature work, and report existing budget warnings without masking them.
- [x] 6.2 Format changed sources, run git diff checks and strict OpenSpec validation; verify implementation matches all three delta specs and every completed task has its verification evidence.

## 7. Media workspace refinements

The user paused test work during these refinements. Do not add or run suites until requested. Earlier verification in sections 1–6 describes the earlier implementation, not proof for the revised UI. Current acceptance uses source review, formatting, lint and production builds; interactive and updated unit verification remain deferred.

- [x] 7.1 Colocate related files in functional folders and persist frontend conventions; verify imports and module-boundary lint.
- [x] 7.2 Move breadcrumbs into the sticky shell header with overflow, remove duplicate headings, pin pagination and use an independently scrolling compact grid with a details Sheet drawer. Keep actions beside search with mobile icon buttons and a stable disabled-at-root Up button. Center theme controls in the aligned sidebar footer and cycle the collapsed button; verify production compilation and layout bindings.
- [x] 7.3 Replace card action rows with icon context menus and support validated internal drag/drop onto folders and parent breadcrumbs; review move bindings and preserve the keyboard Move dialog. Interactive verification is deferred.
- [x] 7.4 Replace MediaQueries/MediaMutations/MediaCache with typed MediaQueryOptions factories and injectMutation consumers, colocate the form in MediaDetails and use linkedQueryParam for URL state; review cancellation/cache handling and verify compilation without a separate cache/editor-state service.
- [x] 7.5 Use geometry-preserving busy button spinners, update documentation and run affected lint/builds, formatting and strict OpenSpec validation; report test deferral and bundle warnings.
- [x] 7.6 Keep dialog/guard flows Observable-based, extract templates longer than three lines, separate multiline properties and TanStack fields, add optimistic rename/move with rollback and Spartan error toasts, and use getMediaErrorMessage with Remeda and HttpStatusCode. Review concurrent-write fallback and verify production compilation/lint; tests remain paused.

## 8. Upload, controls and drawer refinements

- [x] 8.1 Add shared attribute Button with HlmButton composition, loading and IconType icons; replace native title hints with Spartan tooltips, strengthen action dots and retain consistent icon/text formatting.
- [x] 8.2 Match upload/completed card geometry, overlay transfer/processing feedback and add a separate indeterminate progress line. Retain cached data on refresh failures with loading Retry controls.
- [x] 8.3 Address drawer selection through the named details outlet; request guarded close on Escape/backdrop and preserve browser history navigation. Keep deletion server-confirmed.
- [x] 8.4 Update shared rules/artifacts and verify formatting, lint, production builds and strict OpenSpec validation. Do not add or run tests while deferred.

- [x] 8.5 Enable development-only TanStack Devtools, suppress redundant visible-label tooltips and center spinner rotation; verify affected lint and development/production builds without running tests.

- [x] 8.6 Add shared long-press touch tooltip behavior without changing short-tap actions, keyboard focus, mouse hover or existing tooltip restrictions; verify affected lint and production compilation without running tests.

- [x] 8.7 Remove frontend fileUrl rewriting, proxy canonical file paths, simplify renamed library/utils.ts with Remeda and move details into a guarded named route using routerOutletData; update artifacts and verify lint/builds without running tests.

- [x] 8.8 Remove synchronized error state/effect, render TanStack errors directly above cached data and replace search synchronization with linkedSignal; verify formatting, lint and compilation without running tests.

- [x] 8.9 Preserve user naming changes, document/query-before-mutation declaration order, bind standalone search directly with ngModel/linkedQueryParam and remove manual search copying, timeout/debounce and navigation rollback; verify lint and production compilation without running tests.

- [x] 8.10 Generate/adapt HlmNumberedPaginationQueryParams and its select dependency, bind numbered navigation to offset/limit URL state, preserve filters and details on bounds correction, and verify responsive structure, formatting, lint and production builds without running tests.

- [x] 8.11 Separate folder context from PaginatedResult lists, raise API maximum to 120/default 24, offer 24/48/72/96/120 limits, use divisor-based CSS container columns and RouterLink card navigation; verify formatting, lint, backend/frontend builds and strict OpenSpec validation without tests.

- [x] 8.12 Extract shared toPromise with optional AbortSignal and use it throughout Media options and Games queries; verify formatting, lint and production builds without tests.

- [x] 8.13 Replace MediaUploads with mutation-state-derived upload cards, retain only keyed progress in options, remove progress/completion callbacks and preserve previews, explicit retry and dismissal; verify lint and production compilation without tests. Obsolete feature tests were removed during the requested cleanup.

- [x] 8.14 Remove presentation metadata and global MutationCache toast handling; move user-facing error formatting and dialog/editor reactions into feature-level mutation callbacks without overriding cache rollback; record the boundary and verify lint/production builds without tests.

- [x] 8.15 Inline upload transport in mutationFn, remove upload dismissal lookups, extract typed uploadState options and use submittedAt with the upload key for stable attempt tracking; verify formatting, lint and production compilation without tests.

- [x] 8.16 Remove feature-library tests and unused runner configuration, obsolete URL parsing, unused icon registrations and duplicate injection; verify remaining production modules are reachable and lint/build pass without tests.

- [x] 8.17 Replace manual editor submit handlers with FormRoot/submission.action and awaited mutateAsync; use form submitting() for loading/locking/guards and mutation-derived error messages, record the convention and verify lint and admin production compilation without tests.

- [x] 8.18 Generate/adapt shared field and label primitives, centralize touched Signal Form error rendering/accessibility, move required/max-length copy into schema messages and compose both editors with HlmField; verify formatting, lint and production builds without tests.

- [x] 8.19 Change update variables to id/patch, resolve optimistic baselines inside options with an uncached fallback, add shared dirty-value extraction and use it in both editors with explicit preview dirty/reset handling; verify formatting, lint, production build and strict OpenSpec validation without tests.

- [x] 8.20 Replace custom upload progress markup/CSS with generated shared Spartan progress primitives, preserving determinate/indeterminate/error states and reduced motion; verify formatting, lint and admin production compilation without tests.

- [x] 8.21 Move card semantics/styles to the MediaCard host, replace native dragging with parent-owned CDK folder/breadcrumb drop targets, preserve the source slot and indicate eligible/active destinations; verify formatting, lint, production compilation and strict OpenSpec validation without tests. Compilation succeeds, but the production build is blocked by the initial bundle budget (approximately 1.05 MB versus 1 MB); do not raise the budget silently. Browser interaction verification remains deferred.

## 9. Loading and navigation refinements

These later refinements were implemented in the current session. The user separately requested component tests; the earlier test deferrals remain historical records rather than instructions to remove restored coverage. Subsequent UI changes were verified with focused browser checks, formatting, lint and production builds without introducing an E2E suite.

- [x] 9.1 Rename the admin media feature to feature-list, colocate functional components and replace generic query-parameter helper naming with descriptive linkedQueryParam configuration. Restore the requested component coverage; the media feature, admin and data-access suites passed 53, 14 and 10 tests respectively before later UI refinements.
- [x] 9.2 Add geometry-preserving non-interactive skeleton cards, responsive two-row fallback, exact known counts including zero and same-directory previous results. Guard empty states and bounds correction from provisional totals; verify through the requested tests and browser loading scenarios.
- [x] 9.3 Fit breadcrumbs using actual label widths and show header fetching through a bottom-edge indeterminate progress line without layout shifts; verify mobile/wide layouts and header/card geometry in the browser.
- [x] 9.4 Derive the Up destination from known current/cached metadata during pending or failed requests, preserving null-root versus unknown-parent semantics, limit and route guards; verify pending, failure and unknown-parent browser scenarios.
- [x] 9.5 Use CDK receiving/dragging classes for parent eligibility/active CSS, retain delayed entry and zero-delay removal, add mouse drag-start delay and preserve keyboard focus independently of pointer activation; verify real drag and disabled-to-enabled navigation in the browser.
- [x] 9.6 Compose shared projected error states and generated Spartan Empty presentation, persist the Spartan-first generation and local named-type conventions, and verify formatting, shared/feature lint, production compilation and focused browser scenarios. Retain existing build-budget warnings.
