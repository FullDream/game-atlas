# Administration media library

Lazy administration feature, tagged `type:feature`, `scope:media`, `platform:admin`.

## TypeScript and template conventions

The code was compared with headless-cms IAM's [RoleList](https://github.com/FullDream/headless-cms/tree/main/frontend/libs/iam/feat-roles/src/lib/role-list), [RoleEditor](https://github.com/FullDream/headless-cms/tree/main/frontend/libs/iam/feat-roles/src/lib/role-editor) and query options. The applicable patterns are adapted to this workspace's `AGENTS.md` and Spartan components:

| Concern               | Media implementation                                                                                                                                                               |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Component state       | Dependencies precede queries and mutations. Query source signals are initialized first. Template members are protected; implementation details use ECMAScript private members.     |
| Naming                | `mediaQuery`, `folderQuery`, `mediaForm`, `folderForm` and descriptive mutation/dependency names identify the operation or resource.                                               |
| Editing               | Typed form models stay in the editor with Signal Forms validation and submission. `linkedSignal` resets editable state from its source; `getDirtyValues` builds PATCH payloads.    |
| UI reactions          | Features supply `onSuccess` and `onError`. Transport errors are derived from mutation state; data-access retains cache reconciliation and rollback.                                |
| Search and pagination | Standalone searches bind directly through `ngModel`. Computed request parameters feed query options; both the list and picker pass offsets directly to transport.                  |
| Templates             | Page header portals own actions, breadcrumbs and loading. Templates use local aliases, multiline control flow and separate logical sections. Component hosts own outer containers. |

PrimeNG components, explicit OnPush metadata and generated TanStack integrations from the reference are not part of this workspace's conventions. Media retains Spartan, Angular 22 defaults, handwritten query options and its existing guarded drawer and upload lifecycle.

The URL carries `folderId`, `search`, `offset` and `limit`; defaults are omitted. Listing defaults to 24 server items per page. HlmNumberedPaginationQueryParams provides numbered links, ellipses and a 24/48/72/96/120 size selector. Changing size resets offset to zero; folder navigation preserves the selected limit. Invalid UI limits fall back to 24; supported sizes are multiples of 24 through 120. Breadcrumbs and current folder come from GET /media/folders/{id}, cached separately from list pages. The root needs no folder request. The app header is sticky; folder breadcrumbs collapse into an accessible overflow menu. The center scrolls independently of the bottom pagination. Cards navigate using RouterLink/queryParams bindings and have icon context menus and support Angular CDK drag/drop onto folders or parent breadcrumbs, with the Move dialog retained as a keyboard alternative. Details open in a right-side Sheet drawer. The FeatureList page lives directly in src/lib as feature-list.ts with its template, styles and query-parameter configuration (feature-list.html, feature-list.css and feature-list-query-params.ts). The media.routes.ts route configuration stays directly in src/lib alongside media-details, media-card, media-upload-card, folder-editor-dialog, confirmation-dialog, media-picker-dialog and media-thumbnail component folders, following the IAM features in headless-cms. Page actions, breadcrumbs and request loading are projected into the application header through PageHeaderPortal. The named details outlet addresses the selected file; its draft stays local to the details component. Saves are explicit and dirty drafts are protected by the same dialog for editor actions,
route departure and same-route location changes, including browser history. Reload and tab closure
are deliberately not intercepted.

Folder, search, offset and limit use ngxtension `linkedQueryParam` with ready-to-use parse/stringify option objects from `feature-list-query-params.ts`. Search binds directly through ngModel; its change binding explicitly resets offset to zero. Query parameters update immediately, with no copied form model, timeout or rejected-navigation reset. Query declarations precede mutation declarations after their signal dependencies. Details use the named `details` router outlet at `files/:id`, for example `/media/(details:files/<id>)?folderId=<folder>`. Create/upload controls sit beside search;
on mobile they use named icons. Up remains disabled at root to prevent layout shifts. When current folder metadata is available from detail or listing caches, Up and its drop target use the known parent even while requests are pending or have failed. Without a known folder, Up remains disabled rather than assuming the parent is root. The details
component owns its form and TanStack mutation; `PendingChanges` only bridges its leave check to guards.
Dialog results and leave checks remain Observables; HTTP conversion is limited to TanStack transport
functions. Templates longer than three lines live in colocated HTML files. Rename and move
update query data optimistically, roll back on failure and report failures through application toasts.

Uploads accept one file per selection. Cards derived from injectMutationState appear immediately and do not contribute to the
server total or canonical page order. The card belongs only to its captured destination and survives
folder changes. XHR transfer progress becomes indeterminate Processing after 100 percent, until the
server responds. Failed cards retain their File for explicit retry or removal. No automatic POST retry
is performed: a response lost after commit can lead to a duplicate on retry. Uploads remain in the application mutation cache across folder and feature navigation. Reloading discards in-memory files and unfinished requests. No cross-reload persistence or upload queue
is provided. Object URLs are owned by card subscriptions and revoked on removal/disposal; unverified SVG is never locally embedded.

Committed image previews use NgOptimizedImage and the API Base64 placeholder when non-null. SVG and
small originals have no placeholder. The default loader serves originals; resizing, transformation URLs
and custom loaders are deferred. Native video `poster` uses the optional chosen library image URL.

Folder and image pickers retain mixed-list pagination; videos cannot be selected as image covers.
Delete is confirmed one item at a time and never detaches referenced assets. Missing file delivery
changes presentation only, preserving metadata and its available actions.

Component tests use Angular TestBed and Vitest through `npm exec nx -- test media-feature-list`.
The library has its own test target and TypeScript configuration, using the admin development build as
the compilation harness. Specs are colocated with their components. They exercise real templates,
Signal Forms, TanStack options, dialogs and router guards against mocked API transport, including
dirty-only PATCH payloads, explicit null/empty values, upload previews and cache reconciliation.
Browser API stubs cover jsdom's missing resize/media-query APIs; these tests do not verify actual
layout, media decoding, touch gestures or visual appearance. Admin shell tests run separately with
`npm exec nx -- test admin`. Formatting, lint and production builds remain additional checks.

Deletion waits for server success before removing cached items; refusals keep the item visible and show an error toast.

The details drawer requests guarded dismissal on Escape and backdrop clicks. Browser history restores or clears selection; explicit close removes the details outlet without adding a history entry. The routed component receives current listing data through routerOutletData; links retain folder, search, offset and limit context. No separate asset GET is introduced. Named-route CanDeactivate and parent navigation guards protect unsaved drafts. Folder/search/offset navigation clears the outlet atomically with the new query parameters.
Upload previews share completed cards' 4:3 geometry. A blurred, dimmed overlay carries percentage/status; a separate 6px line becomes indeterminate during processing. Cached listings stay visible under a compact refresh-error banner. Retry controls preserve their dimensions with a loading spinner.

Pagination stays in the bottom footer, wrapping into two rows on narrow screens. Queries forward URL offset and limit directly, including valid offsets that are not multiples of limit. Only the paginator derives page numbers and converts numbered links into offsets. Bounds correction updates only offset with replaceUrl and preserves filters and the details outlet. Pagination computations are pure; they never write models during rendering.

The grid uses CSS container queries with 1, 2, 3, 4, 6, 8 and 12 columns. All column counts divide 24, so full responses at every offered size fill complete rows. Temporary upload cards and final partial pages may have incomplete rows.

The first uncached listing shows two responsive rows of MediaCard skeletons. When entering a folder with a cached item count no greater than the selected limit, it shows exactly that many skeletons; otherwise it uses two rows. Skeleton mode replaces the preview, name and content type with Spartan skeletons while preserving card dimensions and omitting interactive controls. Search, pagination and limit changes within the same directory retain previous results through TanStack placeholderData. Directory changes never retain another directory's results. Empty states and pagination bounds correction wait for the actual response rather than using placeholder totals.

MediaUploads and its mirrored card array are removed. The grid selects pending/error mutations by uploadKey and destination. File, folder, controller, status and error come from the mutation; only progress percentages live in options, keyed by variables.key and removed on settlement. Retry removes the old attempt and creates a new mutation for the same file/destination. Standard mutation-cache garbage collection governs retained attempts.

The feature owns getMediaErrorMessage and all toast/dialog/router reactions. Per-call onSuccess closes dialogs or updates the editor baseline; onError displays inline feedback/toasts. Data-access options keep transport, validation errors, optimistic rollback and cache reconciliation only; no errorToast metadata or global MutationCache presentation handler is used.

Validated editors use FormRoot and the form options submission.action with awaited mutateAsync calls. Form submitting() controls loading and leave protection. Schema-based disabling follows the mutation's pending state so getDirtyValues runs before disabling fields: Angular excludes non-interactive fields from dirty state. There are no manual submit handlers, preventDefault calls or duplicate validity/busy guards. Server messages are derived from mutation errors, so retries remain possible without changing otherwise valid input. Successful saves use feature onSuccess callbacks and reset the form baseline.

Folder and asset editors compose inputs with ui-field and its label/description inputs. Required/max-length validation messages are defined in the Signal Forms schema; shared Field handles error rendering centrally. General server feedback remains a separate force-shown field-error.

Editors pass only id and patch to update mutations. getDirtyValues extracts patches from Signal Forms dirty fields. Choosing/clearing a video cover marks previewAssetId dirty and uses a nullable ID, preserving explicit null. Success/discard resets form flags; pristine fields never enter PATCH bodies and empty patches are not submitted.

Upload progress uses HlmProgress/HlmProgressIndicator, preserving the 6px bottom line. Processing/unknown length use null for built-in indeterminate animation, and failures use a complete destructive indicator. The former custom progress markup and animation stylesheet are removed.
