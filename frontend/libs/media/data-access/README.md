# Media data access

Shared Angular HttpClient transport and TanStack query options for admin and future web consumers.

Run `npm exec nx -- run media-data-access:generate-api` from `frontend` with the backend available at
`http://localhost:5041/openapi/v1.json`. `GAME_ATLAS_OPENAPI_URL` overrides the input. Generation selects
only the Media tag, writes kebab-case files under `src/lib/generated` and commits those sources.
Builds do not fetch OpenAPI. Never edit generated sources by hand. Generation cleans its output;
after a failed fetch, rerun against an available backend or restore the committed generated files.

Applications provide `provideMediaBaseUrl('/api')` (or a deployment API address) for API requests.
Media previews use canonical URLs returned by the backend directly, without a frontend URL helper.
Admin development proxies `/media/files/**` unchanged; production hosting must route that prefix to the backend/storage origin.
Admin uses XHR for observable upload progress; shared consumers choose their own HTTP backend.

`MediaQueryOptions` colocates normalized queries and typed mutation options. Features use `injectMutation`.
Rename and move snapshot affected lists and update them optimistically. Success reconciles the
server response without applying count changes twice; failure restores unchanged snapshots. Concurrent
cache writes and complex filtered/paginated results trigger narrowly scoped refreshes. Feature callbacks own
error presentation, dialogs and navigation; options contain no UI metadata or notifications. Creation
and uploads reconcile authoritative responses. No separate cache service or generic entity store is used.
Transport errors and ProblemDetails propagate unchanged; user-facing message formatting belongs to the feature.

Deletion waits for server success before removing cached items; refusals keep the item visible and propagate the error to the feature.

Feature-local reconciliation helpers live in src/lib/utils.ts. Remeda pipelines handle immutable replacements, deduplication and sorting, preserving SQLite UTF-8 BINARY name order and targeted invalidation for incomplete pages.

Lists use PaginatedResultOfMediaItem (items, limit, offset, total), default limit 24 and API maximum 120. MediaFolderDetails comes from the independent folder(id) query and is cached by folder ID, without search or pagination in its key. Optimistic snapshots and rollback cover both list items and folder metadata/ancestor names; moving a folder invalidates only affected ancestor paths.

List placeholderData uses keepPreviousData only when the previous and next folder IDs match, preserving results while search, offset or limit changes. Directory changes start without previous results. getCachedFolder exposes known folder metadata from detail and listing caches without issuing another request; the feature uses its item count when rendering loading skeletons.

Handwritten query options, reconciliation helpers live directly in src/lib; generated transport remains isolated in src/lib/generated.

Upload options own only a readonly progress registry indexed by the key in upload variables. onSettled removes progress; mutation variables hold file, destination and AbortController. Status/error ownership stays in TanStack. The feature observes uploadKey with injectMutationState; no progress or completion callbacks and no separate upload-state service are used.

The typed uploadState(folderId) factory provides filters/select for injectMutationState. Selected values include a remove action and submittedAt; consumers never manipulate MutationCache. HTTP events remain inline in upload().mutationFn. The remove action hides cache deletion inside options. Retry cleanup happens in onMutate by local upload key.

Folder/asset update variables are { id, patch }. Options resolve the optimistic baseline from folder details, mixed list items or embedded video previews; caller DTO snapshots are not accepted. With no cached baseline, the request still executes without a fabricated entity and success invalidates Media queries conservatively. Normal cached updates retain optimistic reconciliation and rollback.

## Picker adapter

MediaPickerAdapter implements the attachment-only MediaPickerSource port from shared/util. The application binds MEDIA_PICKER_SOURCE to this adapter. The shared dialog owns injectQuery, image selection and folder breadcrumbs, with no move operations. The adapter reads the current folder across transport pages, filters eligible folders and images before pagination, and returns accurate totals so excluded videos do not leave gaps in picker pages. The destination-folder dialog stays inside the Media feature and uses MediaQueryOptions mutations directly.

MediaQueryOptions.folderChoices reads the selected folder across transport pages and paginates only folders. The destination dialog uses the same compact Attachment grid and footer as image selection; files do not consume destination-page slots. Self and descendant destinations remain blocked.
