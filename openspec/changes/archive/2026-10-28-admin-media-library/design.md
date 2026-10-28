# Design

## Context

See proposal.md for motivation. Admin currently lazy-loads FoundationPage for Media. Games data access establishes Orval HttpClient generation, normalized query keys, cancellation and runtime base-URL configuration. Media already supports all required mutations and paginated mixed listings, but lacks breadcrumb context. PaginatedResult<T> supplies the common list contract. MediaBrowseTests verifies stable query counts as page size grows.

## Goals / Non-Goals

**Goals:** Reuse existing transport, query and UI conventions; keep server data authoritative; make transfer state independent of folder navigation; update caches without corrupting server ordering or pagination.

**Non-Goals:** Generic file-manager framework, entity store, speculative public-web components, automatic retries of non-idempotent uploads, backend cleanup machinery, content attachment integrations or a database migration.

## Decisions

### 1. Retrieve navigation context separately from pages

GET /media returns the existing PaginatedResult<MediaItemDto> without a derived pagination record. The default limit is 24 and the API accepts integers 1 through 120. Preserve missing-folder errors, filtering, ordering and total semantics.

GET /media/folders/{id} returns MediaFolderDetailsDto containing the complete Folder and ordered Ancestors projections. Read both within one transaction, loading only the parent chain. Root is synthetic and needs no folder request. Cache folder context by ID, independently of search, offset and limit; pagination does not refetch it. Folder-name changes update cached breadcrumbs, counts update on membership changes, and ancestor moves invalidate affected paths. No domain or schema changes are needed.

### 2. Use two new media libraries initially

- libs/media/data-access: type:data-access, scope:media, platform:shared. Orval Media-tag SDK, committed kebab-case generated types, base URL provider and handwritten MediaQueryOptions with typed CreateQueryOptions/CreateMutationOptions factories. Components consume writes with injectMutation. Pure list transformations stay next to the options; no separate MediaCache service. Follow Games generation settings; no generated TanStack API.
- libs/media/feature-list: type:feature, scope:media, platform:admin. Lazy manager, cards, editor, picker/move dialogs and upload orchestration. Keep local components here until a real cross-feature consumer warrants extracting media UI.

Admin composes providers and routes. No shared data-access dependency on admin/UI is introduced. Use Angular 22 signal APIs and Signal Forms for validated editors. Standalone search uses ngModel directly with a linkedQueryParam signal. UI follows existing theme tokens, Material Symbols Rounded, shared Helm entrypoints and accessible focus handling. Review existing project UI and the Spartan catalog first. Reuse suitable primitives and generate missing Helm entrypoints into the existing shared UI project as part of the requested implementation; a separate generation request is not required.

Alternative: putting everything in the admin app or precreating multiple UI/util libraries. Rejected to retain domain boundaries without speculative fragmentation. Reuse folder browsing and image selection locally for moves and covers; later attachment screens are deferred.

### 3. Keep location, editor and upload state separate

Use /media with folderId, search, offset and limit query parameters; omit root/default values. Default limit to 24; HlmNumberedPaginationQueryParams provides numbered RouterLinks and 24/48/72/96/120 page sizes. Forward offset and limit unchanged to data access. Only the paginator derives page numbers and converts numbered links into offsets. Size changes reset offset to zero; retain the selected limit across folder navigation. Invalid URL state is normalized safely. Bind name input directly to linkedQueryParam through ngModel; reset offset explicitly on ngModelChange. Do not add a copied search model, manual debounce timeout or rejected-navigation reset. Let browser history restore location. If deletion leaves the page beyond the last page, replace only the offset query parameter with the last valid page boundary, retaining filters and the details outlet.

Selection is addressed by the named details route; the routed details component owns its linked form model, validation, mutation and dirty confirmation. There is no editor state service. A route-scoped PendingChanges bridge calls the Observable leave check without storing form data. Dialog results remain Observables through guards and bounded component subscriptions; Promise conversion is limited to TanStack HTTP boundaries. File selection opens a right-side Spartan Sheet overlay, full-width on narrow screens. Save sends changed fields and resets the baseline from the response. Guards protect query changes and departure; CanDeactivate alone is insufficient. No beforeunload handler.

Use ngxtension linkedQueryParam for folder, search, offset and limit URL state. Validate and normalize values at this boundary, omit default values when writing and coalesce related changes. Retain browser-history and external-link route guards. The modal details editor prevents interacting with the background search while editing; ordinary route navigation delegates to guards instead of duplicating their checks and restoring search state.

Place FeatureList directly in src/lib as feature-list.ts with its HTML, CSS and feature-list-query-params.ts. Colocate media-details, media-card, media-upload-card, folder-editor-dialog, confirmation-dialog, media-picker-dialog and media-thumbnail in their own functional folders directly under src/lib. The route configuration is media.routes.ts; upload orchestration stays in query options. Media data-access handwritten files live directly under src/lib beside generated. Template control-flow blocks are multiline with blank lines between independent blocks and surrounding markup. The shell owns its sticky header layout; a page-header portal/outlet contributes feature breadcrumbs and can support actions. Media keeps create/upload actions beside search, showing only named icons on mobile. An Up control opens the current folder's parent, resets paging/search and remains disabled at root without changing layout. It also accepts CDK drops to move items up one level, highlights eligible/active drops and rejects drops at root or while the parent is unknown from both current and cached folder metadata. Use existing Spartan breadcrumb and menu primitives for overflow. Cards use RouterLink/queryParams bindings for folder/file navigation rather than an imperative navigate helper. Compact cards expose icon context menus and Angular CDK folder/parent drag targets; retain the keyboard Move dialog. MediaCard stays an element component with role article and all card styling on its host, without an article wrapper. The parent feature owns CDK drag/drop; cards only render destination states. A dashed placeholder/anchor retains the original grid slot, eligible folders change icon and treatment, and the active target is highlighted. Shared breadcrumbs receive IDs through CDK drop events, with the shell providing their common drop-list group. The source grid explicitly connects to its nested folder drop lists through a view query: CDK drop lists stop group inheritance for nested lists. The center scrolls while pagination stays at the bottom. Sidebar theme controls are centered in an equally tall footer; collapsed navigation shows one active-mode button that cycles light, system, dark. Busy buttons hide their original content with visibility and overlay a spinner to preserve dimensions.

Alternative: automatic saving. Rejected to keep validation, discard and atomic PATCH behavior predictable.

### 4. Represent uploads as local operations rather than server entities

Use injectMutationState filtered by the upload mutationKey to render concurrent pending/error attempts. Keep each File, captured folder, local upload key and AbortController in mutation variables. Derive status and errors directly from mutation state; do not maintain a mirrored cards array or MediaUploads service. MediaQueryOptions owns only a readonly progress registry keyed by variables.key, initialized onMutate and removed onSettled. No onProgress/onUploaded callback variables are needed.

Use generated HttpClient events with reportProgress and an XHR backend. The inline mutationFn updates percentages and filters the final response before toPromise. A completed transfer shows Processing while the mutation remains pending through server response and cache reconciliation. Committed assets update relevant server query caches; temporary cards never insert fake entities or alter totals, search membership or canonical ordering.

Only safe raster files receive object-URL previews. Card subscriptions create/revoke previews when files change or cards leave the view; unverified SVG uses a generic icon. Pending uploads continue across folder and feature navigation in the application mutation cache. Retry removes the failed attempt and starts one mutation with the same file, captured destination and local key but a fresh controller. The remove action is shown only for failed, settled attempts. The selected upload exposes a remove action implemented inside options; features never access MutationCache directly. onMutate removes failed attempts with the same local upload key before retrying. No dismissal wrapper or redundant abort is needed. Standard mutation GC governs settled attempts; reload never restores File objects. Explicit retries remain potentially duplicating after a lost response because uploads are not idempotent.

Alternative: inserting fake assets into the server query cache. Rejected because local IDs, unfinished metadata and server pagination would be conflated.

### 5. Apply mutation responses to the cache where correctness is known

MediaQueryOptions owns normalized keys including folder, offset, limit and trimmed search. Mutations use server responses on success, not unconditional invalidateQueries. Keep entity-replacement and list-reconciliation logic close to these keys, without building a generic cache framework. Cancel affected in-flight reads before committing local cache changes so late responses cannot overwrite them.

| Mutation effect                                                   | Cache action                                                                                                                                                        |
| ----------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Alt or video cover only                                           | Replace matching assets in cached lists and selected details without list refetch; propagate image changes into cached video preview representations.               |
| Insert/rename/move/delete with complete affected collection known | Reconcile membership, server-compatible order, Total, slices and cached folder direct counts; deduplicate by server ID.                                             |
| Search or incomplete pages make order/membership uncertain        | Update known entity representations immediately; narrowly invalidate affected source/destination listings for authoritative membership, totals and page boundaries. |
| Folder rename/move                                                | Update known Folder/Ancestors labels and counts; invalidate cached descendant contexts whose changed chain cannot be derived reliably.                              |

Do not approximate SQLite sorting or search using localeCompare or broad Unicode lowercasing. If exact membership/order cannot be guaranteed, classify the operation as complex and invalidate relevant keys. Never append a returned entity blindly to a sorted page. Never seed an uncached folder as though a response represented its complete contents. Image edits can affect embedded previews outside their containing folder; reconcile known occurrences too. Unrelated domains and folders are not blanket-invalidated.

Rename and move use TanStack onMutate snapshots and optimistic changes. Cancel affected reads, retain each affected list's prior state and actual structurally shared optimistic reference, and update known items, direct counts and unfiltered totals. Success replaces provisional fields with the server response without applying move count deltas twice. Data-access onError restores snapshots. Feature-level per-call onError displays error toasts and inline feedback; onSuccess closes dialogs or updates editor baselines after authoritative cache reconciliation. Data-access does not encode UI flags in metadata; user-facing error formatting stays in the feature. If another write/read has changed a list since its snapshot, narrowly refresh that list instead of overwriting concurrent data. Complex filtered/page/hierarchy changes refresh only affected keys after settlement. Upload and creation still reconcile server responses without invented IDs. Failed editors retain their drafts.

### 6. Reuse existing media safety and HTTP contracts

Use Angular NgOptimizedImage for committed image assets and image-based video cover thumbnails in cards, the picker and details panel. Bind the resolved original URL through ngSrc and the existing API Base64 placeholder through placeholder; when placeholder is null, disable it rather than requesting loader-generated placeholders. Use intrinsic dimensions where appropriate or fill inside a positioned, size-constrained preview container for unknown dimensions and thumbnail layouts. Preserve alt text and unavailable-image handling. Native video poster remains a URL attribute, not an NgOptimizedImage binding.

Keep the default image loader for now: no custom IMAGE_LOADER, transformation URLs, invented size variants or backend image-resizing work. Loader configuration and responsive server variants are deferred until the backend supports them. NgOptimizedImage does not itself resize originals. Local blob previews for unfinished uploads remain separate ordinary images; do not send data/blob URLs through the optimized-image pipeline.

Use runtime API addressing for SDK calls and consume canonical file paths unchanged. Development proxies /media/files/** directly to storage delivery; /api/** is reserved for SDK requests. Do not rewrite IDs or server values. Show ProblemDetails field errors and meaningful 404/409/413/422 feedback. Moving a folder uses ancestor information to disallow self/descendant destinations; server cycle validation remains authoritative. The image picker filters eligible image choices client-side while preserving server pagination and navigation; mixed-page counts can include nonselectable videos, with no new type-filter API in this change.

Deletion uses confirmation and removes cached cards only after server success; refusals preserve the card and show an error toast. Physical deletion remains the server's responsibility. A failed media load is presentation state only. No availability probing service, inline SVG, external media URL attachment, detach-on-delete or automatic generated cover is introduced.

## Risks / Trade-offs

- [Verification scope varies by refinement] → Run suites only when explicitly requested. The requested component-test work restored media/admin coverage; subsequent UI adjustments use formatting, lint, production builds and focused browser checks. Earlier suite results do not prove later changes without rerunning the relevant tests.

- [Incomplete pages make cache-only mutations unsafe] → Explicitly support scoped invalidation and test boundary-changing mutations; no blanket refetch policy.
- [Upload response lost after commit] → Explicit retry with no deduplication guarantee; backend idempotency is out of scope.
- [Reload/tab closure loses drafts and pending local files] → Accepted limitation; no persistence or unload warning.
- [Deep ancestry requires additional reads] → Project only ancestors; verify listing work does not grow per returned item and avoid whole-tree loading.
- [Mixed-page image picker can show few eligible choices] → Keep navigation/pagination and explain empty eligible results; no new filtering API yet.
- [Media UI increases the existing initial-bundle warning] → Lazy-load the feature and icons; do not raise budgets simply to hide growth.

## Migration Plan

Deploy the additive list contract first, regenerate the Media client against the running backend, then deploy admin. No schema migration or historical-data conversion is expected. Existing Games response shape stays unchanged. Roll back the frontend independently; if reverting the backend too, remove the dependent frontend first. Generated sources remain committed and builds remain backend-independent.

## Upload and drawer refinements

Pending uploads use the same 4:3 preview and metadata geometry as committed cards. Percentage/status overlays a dimmed, blurred preview. A separate 6px progress line becomes indeterminate during processing. Native title hints are replaced with Spartan tooltips only when they add information; responsive action hints are disabled when their labels are visible. Breadcrumb and file-name tooltips activate only when their text overflows. Spinner icons fill a square host and rotate independently around their own center. Admin enables the installed TanStack development-only withDevtools feature. Ordinary controls use shared attribute Button with HlmButton composition, IconType icons and dimension-preserving loading.

Initial list failures use a centered illustrated error state. Refetch failures preserve cached items and pagination beneath a compact banner; Retry displays a spinner while cached data is being refreshed; a retry without data uses the query loading state.

The named details outlet at files/:id identifies the details drawer alongside folder/search/offset/limit query context. Explicit close clears the outlet with replaceUrl; Back/Forward restores selection through route guards. The routed editor receives the listing through routerOutletData and owns its form; it does not add a second list query or an asset endpoint. Missing selections are reported rather than fetching a separate asset endpoint. Escape and backdrop clicks request dismissal through the component's dirty guard; the Sheet's automatic dismissal stays disabled so a dirty editor cannot close before confirmation.

Touch tooltip activation uses a shared owned Helm adapter: a primary touch held for 500ms requests the existing Brain hover lifecycle, avoiding private methods or a second tooltip overlay. Touch-origin focus does not trigger a tooltip. Release, pointer cancellation, leaving or movement beyond 10px cancels the hold and dismisses its tooltip. A recognized hold consumes the subsequent pointer click and native context menu; a short tap retains the original action. Mouse hover and keyboard focus remain unchanged. The adapter is composed into both HlmTooltip and the sidebar tooltip host.

Media URLs are consumed directly from API responses. Development and production routing must forward canonical /media/files/** delivery separately from API /api/** requests. The fileUrl helper is removed. List reconciliation lives in media/data-access/src/lib/utils.ts and uses Remeda immutable pipelines while retaining SQLite UTF-8 BINARY ordering and conservative refreshes for incomplete results.

List error presentation reads TanStack query.error/isError/data directly; no separate error signal or synchronization effect is maintained. A failed refresh renders a banner above existing data. Search binds through ngModel directly to linkedQueryParam. A computed location derives normalized search text; no extra writable search object is maintained. Route guards decide navigation outcomes, without imperative filter restoration.

TanStack declarations are ordered as query observers, then mutation observers. Declare their plain injected dependencies and required source signals first. Standalone search is not an editing form: no Signal Form, local mirror, OnDestroy debounce cleanup or searchChanged handler is needed.

## Numbered URL pagination

Generate pagination and its select dependency into the existing shared Helm entrypoints. Adapt HlmNumberedPaginationQueryParams to accept offset and limit models bound to linkedQueryParam offset/limit. Defaults (offset zero and limit 24) are omitted. Validate UI limits as multiples of 24 up to 120 (the API accepts all integers 1–120) and nonnegative Int32 offset. Forward valid offsets unchanged, including offsets that are not multiples of limit. Derive page numbers only inside the paginator. The footer wraps on mobile and limits visible page links with ellipses. Preserve query parameters when following pagination links.

Remove the generated computed-side currentPage write; page arrays and bounds helpers remain pure. The listing performs explicit replaceUrl navigation to correct a page beyond the total, changing only offset rather than invoking folder navigation that clears details. Continue displaying pagination with cached results after refetch errors. Test work remains deferred.

### Container-based file grid

Use CSS container queries on the scrollable content region, starting with one column and increasing to 2, 3, 4, 6, 8 and 12 columns at 21, 32, 43, 65, 87 and 131rem container widths. Every column count divides 24, so a full response at any offered page size fills complete rows. Pending upload cards are extra local overlays, and the final partial server page may naturally end with an incomplete row. No viewport-based column calculation or JavaScript resize state is needed.

### Shared transport conversion

Use toPromise from the non-buildable shared/util library at Media and Games query boundaries and Media mutation/upload boundaries. It resolves the first emission and preserves source errors. AbortSignal cancellation rejects with signal.reason and unsubscribes HTTP; an already-aborted signal prevents subscription. Keep progress filtering before conversion and preserve Observable-based dialog/guard flows.

### Cache mutation flow

Use one updateCache routine for provisional and authoritative cache writes. It cancels affected reads, applies the existing pure transformations and returns rollback snapshots. Commit reconciles the server response and deduplicates required invalidations by query hash. Rollback restores only unchanged optimistic snapshots; concurrent cache writes trigger a scoped refresh. Select folder/list shapes through property narrowing rather than repeated subtype assertions.

The typed uploadState(folderId) options factory owns mutation filtering and immutable state selection. It returns a cache-owned remove action plus variables, status, error and submittedAt. Features pass this factory directly to injectMutationState and track attempts by submittedAt plus the local upload key, since millisecond timestamps alone can collide.

### Signal Form submission

Bind validated editor forms through FormRoot. Configure submission.action at form creation and await TanStack mutateAsync until transport/cache reconciliation finishes. Use form submitting() for loading and close guards, and mutation pending state for field disabling so dirty patch extraction precedes disabling; FormRoot owns event handling, validation and duplicate-submission protection. Success reactions stay in feature callbacks. Read server feedback from mutation error state rather than a writable error mirror; do not turn transient transport errors into persistent validation failures that would prevent unchanged retries.

### Shared Signal Form field errors

Generate field and label entrypoints into the existing shared UI project. Adapt owned HlmField into an element component that projects content and observes its single FormField. Render errors after touched using kind/message, centrally through HlmFieldError, with visible error IDs registered for aria-describedby. Explicit projected errors suppress automatic output. Keep field labels/descriptions on Spartan directives and validation copy in validator message options. General server feedback stays independent of client validators so transient failures do not prevent unchanged retries.

### Update variables and dirty form patches

Update options accept only id and patch. Data-access resolves current entities from folder detail/list/embedded-preview caches before optimism; caller DTOs are not baselines. When missing, skip optimism and invalidate Media queries after success because source membership is unknown. Rollback and authoritative reconciliation remain internal.

Signal Forms exposes per-field dirty state but no built-in dirty-value serializer in the installed version. Shared getDirtyValues selects dirty top-level values through Remeda pickBy and preserves explicit null/empty values. Editors use this helper instead of DTO property comparisons. Preview selection marks its programmatically edited field dirty. Successful save and confirmed discard reset the form; no empty patch is sent. Existing forms are flat; nested serializers are deferred and dirty object/array values would be included whole.

Upload cards compose HlmProgress and HlmProgressIndicator rather than a custom progressbar. Pass numeric percentages during transfer, null during processing/unknown length and 100 with the destructive variant on failure. The shared primitive owns animation, RTL direction and progressbar semantics; custom upload keyframes are removed.

## Loading, navigation and presentation refinements

MediaCard has a non-interactive skeleton mode using existing Spartan skeleton primitives for the 4:3 preview, name and content-type lines. Unknown counts and counts exceeding the selected limit render two responsive rows; a known smaller count renders exactly that many cards, including zero. Existing container queries select the visible number of placeholder cards without introducing JavaScript layout state. Skeletons are decorative; the grid exposes loading status.

List placeholderData delegates to keepPreviousData only when the previous and next normalized folder IDs match. Search, offset and limit changes retain previous results while directory changes do not. Empty-state presentation and offset correction ignore placeholder results until the replacement response supplies the actual data and total. Folder details continue to use their existing independent query; synthesizing folder detail or breadcrumb placeholder data was discussed and deferred.

Current folder metadata is derived from the detail observer and matching cached detail/list representations, choosing the freshest available cached representation. The same value supplies skeleton counts and the Up destination. Unknown metadata remains distinct from a known null parent, which identifies root. Loading and query failure do not disable a known parent link; RouterLink preserves the limit, resets listing context and keeps route guards authoritative.

WorkspaceBreadcrumbs measures rendered label widths and available header space rather than allocating a fixed width per crumb. Short mobile paths remain visible; overflow ancestors stay in the existing accessible menu. The shell replaces its in-row spinner with an absolutely positioned 2px HlmProgress/HlmProgressIndicator at the header bottom. A null progress value is indeterminate; navigation loading and page-header fetching drive visibility without changing header geometry.

The Up drop target uses CDK's receiving class for eligibility and dragging class for the active destination. Feature-local CSS references the shared Tailwind theme and uses @apply for borders, rings and focus states; no manually synchronized parent drop attribute or signal is retained. Border entry waits 160ms, the active ring waits 160ms and ring exit has no delay; all delay resets to zero when dragging ends. Mouse drag initiation waits 120ms and touch initiation 220ms. CdkMonitorFocus distinguishes keyboard focus from clicks/taps, including a disabled-to-enabled Up transition. Existing route guards and move validation are retained.

Shared ErrorState exposes icon/variant inputs and projects heading, description and recovery controls through ng-content. Features own messages, retry loading and navigation actions. The media feature uses the default initial-error layout and compact cached-data refresh-error layout. Empty folder and no-match presentation uses the generated Spartan empty entrypoint in the existing ui-helm project; it remains separate from errors, pending skeletons and upload cards.

Verification covered responsive skeleton rows and card geometry, cached zero counts, same-directory result retention, mobile breadcrumb fitting, layout-stable header progress, Up navigation during pending/error requests, keyboard versus pointer focus and real drag eligibility/active feedback. The requested suites previously passed 53 media feature, 14 admin and 10 data-access tests; later refinements were checked through targeted browser scenarios, lint, formatting and production compilation rather than assuming those suites had been rerun. Current builds retain the initial-bundle warning and a 5.22kB feature stylesheet warning against its 4kB warning threshold; budgets were not raised.
