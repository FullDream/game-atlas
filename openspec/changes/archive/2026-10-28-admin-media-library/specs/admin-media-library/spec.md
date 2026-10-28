# Spec Delta

## Purpose

Let administrators browse, upload and organize reusable media through an accessible folder-based manager with visible operation states.

## ADDED Requirements

### Requirement: Browse media with addressable folder navigation

Administration SHALL display folders, images and videos in a grid with breadcrumbs, current-folder name search and pages defaulting to 24 server items. Folder, search, offset and limit SHALL be represented in the URL and restored by direct links and browser history. Changing folder or search SHALL reset offset to zero.

#### Scenario: Open a nested folder directly

- **WHEN** an operator opens a URL identifying a nested folder
- **THEN** its contents and server-provided ancestor breadcrumbs render without visiting parents first
- **AND** folder clicks navigate contents and browser Back restores the previous location

#### Scenario: Search and paginate

- **WHEN** an operator changes search or selects another page
- **THEN** results follow the backend's immediate-child search, folders-first ordering and total
- **AND** search changes reset pagination without recursively searching descendants

### Requirement: Present useful browsing states safely

The manager SHALL distinguish loading, empty folder, no search matches, request failure and missing folder. Image cards SHALL use available placeholders while loading. Failed media delivery SHALL show an unavailable state without removing metadata. Uploaded SVG SHALL only render in an image context, and metadata SHALL render as escaped text.

#### Scenario: Recover from an unavailable file

- **WHEN** image or video delivery fails for a listed asset
- **THEN** a fallback identifies the unavailable preview while metadata actions remain available
- **AND** no raw SVG or metadata is injected as HTML

#### Scenario: Recover from a failed list request

- **WHEN** the current listing request fails
- **THEN** the manager exposes an understandable error and retry action rather than successful empty results

#### Scenario: Show a confirmed empty folder

- **WHEN** the current listing succeeds with no items, no active search and no pending upload cards
- **THEN** an illustrated empty-folder state identifies the empty folder and explains that files can be uploaded or folders created
- **AND** loading and request failures are not presented as successful empty results

#### Scenario: Explain an empty search result

- **WHEN** the current listing succeeds with no items for a nonempty name search and no pending upload cards
- **THEN** the empty state identifies that no matches were found and suggests changing or clearing the search
- **AND** it does not claim the entire folder has no contents

### Requirement: Upload one file with an immediate progress card

File selection SHALL accept one file per upload action into the current folder. A temporary card SHALL appear immediately, show transfer percentage when measurable, and show Processing after all bytes are sent until the server responds. Editing SHALL remain disabled until success. A successful response SHALL replace temporary identity with authoritative asset data.

#### Scenario: Separate transfer from processing

- **WHEN** file transfer reaches 100 percent before the response arrives
- **THEN** the card shows Processing with an activity indicator and no invented processing percentage
- **AND** state is understandable without hovering over a tooltip

#### Scenario: Complete admission

- **WHEN** the server returns the committed asset
- **THEN** the pending card is reconciled without a duplicate asset card
- **AND** the returned server ID and metadata become available for editing

### Requirement: Keep uploads independent of folder navigation

An upload SHALL continue across folder navigation within the manager. Its card SHALL belong only to the captured destination folder. Failed cards SHALL offer retry and removal; retry SHALL reuse the retained file and destination. Files SHALL remain memory-only and SHALL NOT be restored after page reload.

#### Scenario: Navigate during transfer

- **WHEN** the operator enters another folder during an upload
- **THEN** transfer continues and its card is absent from the other folder
- **AND** returning to the destination reveals its current state

#### Scenario: Retry a failed upload

- **WHEN** the operator retries a failed card
- **THEN** one new request uses the retained file and original destination and replaces the failed attempt with one pending card for the same local upload key
- **AND** removing the failed card releases its local resources

### Requirement: Edit metadata explicitly in a details panel

Selecting an asset SHALL open a right-side preview and details drawer over the grid, using the full width on narrow viewports. Name and image alt or video cover SHALL be editable with explicit Save. Read-only server metadata SHALL be viewable. Only changed mutable fields SHALL be submitted; failures SHALL retain the draft and show relevant errors.

#### Scenario: Save an image draft

- **WHEN** an operator edits an image name or alt and saves
- **THEN** a single metadata update preserves omitted fields and successful server data becomes the saved state
- **AND** an unchanged draft does not send an empty patch

### Requirement: Choose an optional video cover

Videos SHALL allow selecting one existing library image as a cover through a folder-aware image picker and clearing that selection. Without a cover the grid SHALL show a video icon; the details panel SHALL provide a video player. Automatic frame extraction SHALL NOT occur.

#### Scenario: Select or clear a cover

- **WHEN** an operator saves a selected image or clears a video's cover
- **THEN** the update supplies the image ID or explicit null respectively
- **AND** neither operation deletes an image or replaces video bytes

### Requirement: Protect unsaved changes inside administration

Leaving the Media page, closing its editor or selecting another asset with unsaved changes SHALL offer Continue editing or Discard changes. Continuing SHALL preserve the draft and cancel the action. Discarding SHALL allow the action. Browser reload and tab closure SHALL NOT be intercepted in this version.

#### Scenario: Cancel navigation

- **WHEN** an operator attempts to leave with a dirty draft and chooses Continue editing
- **THEN** the current location, selection and draft remain intact

#### Scenario: Discard an editor draft

- **WHEN** the operator closes the editor and confirms Discard changes
- **THEN** the panel closes without saving

### Requirement: Create and move folders and files

Operators SHALL be able to create and rename folders and move a folder or asset through a folder-navigation dialog with breadcrumbs and Move here. Root SHALL be selectable. Folder moves SHALL prevent selection of self or descendants, and backend validation SHALL remain authoritative. Failed operations SHALL preserve the original state and show the reason.

#### Scenario: Move a folder

- **WHEN** an operator chooses a permitted destination and confirms a move
- **THEN** a single update changes its parent without changing descendant asset URLs
- **AND** self and descendant destinations cannot be confirmed

#### Scenario: Reject a conflicting folder name

- **WHEN** creation or rename conflicts with a sibling folder
- **THEN** the conflict is shown without pretending the operation succeeded

### Requirement: Delete one item with confirmation

Deletion SHALL require confirmation and operate on one item at a time. The item SHALL remain in cached listings until the server confirms deletion. A failed request SHALL keep the item visible and show an error toast without detaching resources. Bulk and recursive deletion SHALL NOT be offered.

#### Scenario: Refuse an in-use file

- **WHEN** the server rejects confirmed deletion because an asset is referenced
- **THEN** the item remains visible and a conflict toast explains the refusal

### Requirement: Reconcile successful changes without unnecessary reloads

Rename and move SHALL optimistically update known data with rollback and error toasts on failure. Success SHALL reconcile authoritative values when membership, order, totals and pagination can be maintained. The manager SHALL NOT unconditionally reload the current list. Ambiguous search/page boundaries or concurrent writes SHALL trigger only affected refreshes.

#### Scenario: Optimistically rename or move an item

- **WHEN** an operator submits a rename or move
- **THEN** affected cached data updates before the response and success replaces provisional fields with authoritative server values
- **AND** failure restores the cache snapshot and shows a toast; concurrent successful writes are preserved through narrowly scoped reconciliation or refresh

#### Scenario: Edit metadata without changing list membership

- **WHEN** only image alt changes in a cached listing
- **THEN** its cached representation and selected details update from the response without a new list request

#### Scenario: Preserve complex pagination

- **WHEN** a rename, insertion, move or deletion changes an incompletely cached sorted or filtered page
- **THEN** affected listings are refreshed as necessary to restore correct order, totals and boundaries
- **AND** unrelated folder listings are not unconditionally reloaded

### Requirement: Support keyboard and responsive use

Controls SHALL have accessible names, visible focus and keyboard operation. Modal dialogs SHALL manage focus and support dismissal subject to dirty-state protection. Progress and errors SHALL be available to assistive technology. Layout SHALL support both themes and narrow screens without relying on hover-only actions.

#### Scenario: Operate the manager without a pointer

- **WHEN** an operator navigates cards, pagination and dialogs using a keyboard
- **THEN** actions remain reachable, named and visibly focused
- **AND** closing a dialog restores focus to a meaningful triggering control

### Requirement: Keep navigation and pagination visible

Folder breadcrumbs SHALL appear in the sticky header, with overflow in an ellipsis menu. The toolbar SHALL omit duplicate headings and place search, Up and create/upload actions together. Mobile actions SHALL be named icon buttons. Up SHALL remain present but disabled at root, preserving geometry. The center SHALL scroll independently above bottom pagination.

#### Scenario: Browse a long folder path

- **WHEN** the ancestor path exceeds available header width
- **THEN** current location remains identifiable and hidden ancestors can be opened from the overflow menu
- **AND** scrolling cards leaves the header and pagination visible

#### Scenario: Navigate up on a narrow screen

- **WHEN** the operator activates Up in a nested folder
- **THEN** its parent opens with search and pagination reset
- **AND** at root the disabled Up control preserves its space and create/upload actions remain reachable without crowding the header

### Requirement: Put card semantics on the component host

MediaCard SHALL remain an element component with an article role on its styled host and no redundant article wrapper.

#### Scenario: Render a media card

- **WHEN** a media item is rendered
- **THEN** the media-card host carries its article role, border and card layout
- **AND** its template contains card content without an outer article element

### Requirement: Organize compact cards through contextual actions

Cards SHALL use compact previews and expose icon-labelled actions through a context menu and a named menu button. Permanent action rows SHALL NOT appear under each card. The parent feature SHALL orchestrate Angular CDK dragging and folder drops, with a dashed source placeholder and distinct eligible/active folder treatment. Internal drag and drop SHALL move an item into an eligible visible folder or parent breadcrumb; the Move dialog SHALL remain available without dragging.

#### Scenario: Move by dragging

- **WHEN** an operator drops a file on another folder or a parent breadcrumb
- **THEN** the same validated move operation updates its destination
- **AND** self/same-parent drops are refused and server failures remain visible

#### Scenario: Move to the parent with the Up control

- **WHEN** an operator drops an item on the Up control beside search in a nested folder
- **THEN** the same validated move operation moves it to that folder's parent, including the library root
- **AND** eligible and active drop states are highlighted without navigating
- **AND** at root or without known parent metadata the control refuses drops

### Requirement: Keep busy button geometry stable

A busy operation button SHALL overlay a spinner while retaining its original content with hidden visibility. Its original width and height SHALL remain unchanged, and duplicate activation SHALL be disabled while pending.

#### Scenario: Save a draft

- **WHEN** a save operation is pending
- **THEN** its original label remains in layout but is hidden and a spinner appears in its place
- **AND** the label returns when the operation settles

### Requirement: Keep upload geometry and loading feedback stable

Pending cards SHALL use completed-card geometry with a 4:3 preview. Percentage and Processing SHALL overlay a blurred, dimmed preview. A separate 5–6px progress line SHALL become indeterminate during processing. Loading controls SHALL retain content dimensions under their spinner. Hover hints SHALL use Spartan tooltips without duplicating fully visible labels. Responsive actions SHALL disable hints when text is shown. Spinner rotation SHALL remain centered at supported sizes.

#### Scenario: Process uploaded bytes without layout shifts

- **WHEN** transfer completes and server processing continues
- **THEN** the card retains its dimensions and shows Processing over the preview with an indeterminate progress line

### Requirement: Preserve loaded results on refresh failure

The manager SHALL show a compact error banner above cached items when refresh fails, retaining pagination. A failure without data SHALL show a centered icon, message and spaced recovery actions. Cached-data Retry SHALL show a loading spinner while refetching. Query state SHALL be the source of truth without a separately synchronized error signal.

#### Scenario: Retry a failed refresh

- **WHEN** a refresh fails with cached data and the operator retries
- **THEN** existing cards and pagination remain visible and Retry displays a dimension-preserving spinner

### Requirement: Address and dismiss the details drawer safely

Selection SHALL use the named details outlet at files/:id alongside listing query context. The editor SHALL consume routerOutletData without a second query. Back/Forward, Escape and backdrop dismissal SHALL respect unsaved-change guards. Explicit close SHALL clear the outlet without adding history.

#### Scenario: Cancel backdrop dismissal

- **WHEN** the operator clicks the backdrop with a dirty draft and chooses Continue editing
- **THEN** the drawer, selection URL and draft remain intact

#### Scenario: Return from details with browser Back

- **WHEN** the operator opens a file and presses Back with a clean draft
- **THEN** the details outlet is removed and the drawer closes

#### Scenario: Change listing context with details open

- **WHEN** the operator changes folder, search or page and confirms any required discard
- **THEN** the details outlet clears atomically with the query update

#### Scenario: Resolve a detail URL from its page

- **WHEN** a details URL is opened with its folder and paging context
- **THEN** the editor uses the listing data and shows a selection message if the asset is absent

### Requirement: Show touch tooltips only after a hold

A short touch tap SHALL activate its control without opening a tooltip. Holding an eligible trigger for 500ms SHALL show its supplementary tooltip. Release, cancellation or scrolling SHALL dismiss or cancel it. A recognized hold SHALL NOT activate the control when released. Mouse hover and keyboard focus SHALL retain tooltip support. Existing visible-label and overflow restrictions SHALL still apply.

#### Scenario: Tap an icon action

- **WHEN** an operator briefly taps an icon action on a touch screen
- **THEN** its action runs and no tooltip opens from touch-origin focus

#### Scenario: Hold to inspect an icon action

- **WHEN** an operator holds the trigger without scrolling
- **THEN** its tooltip appears after the hold threshold
- **AND** releasing dismisses it without activating the action

### Requirement: Numbered pagination and URL page size

The library SHALL use HlmNumberedPaginationQueryParams with numbered links, ellipses and a size selector. The URL SHALL use offset and limit; only the paginator SHALL derive page numbers. Valid offset and limit SHALL be forwarded unchanged to the API. The footer SHALL remain at the bottom and fit narrow screens.

#### Scenario: Parse pagination parameters

- **WHEN** the library reads URL pagination state
- **THEN** offset defaults to zero and accepts nonnegative Int32 integers
- **AND** limit defaults to 24 and accepts multiples of 24 through 120
- **AND** invalid values fall back to their defaults and defaults are omitted when writing the URL
- **AND** the size selector offers 24, 48, 72, 96 and 120

#### Scenario: Forward an unaligned offset

- **WHEN** the URL contains offset=7 and limit=24
- **THEN** the API receives offset=7 and limit=24 without rounding offset to a page boundary
- **AND** page numbers are derived only inside the paginator

#### Scenario: Change page size

- **WHEN** an operator selects 48 items per page
- **THEN** limit=48 is reflected in the URL and the first page requests offset=0 and limit=48
- **AND** later page links retain folder, search and limit
- **AND** folder navigation retains the selected limit

#### Scenario: Correct a removed last page

- **WHEN** a confirmed deletion or move leaves the current page beyond the available total
- **THEN** only the offset query parameter is replaced with the last valid page boundary
- **AND** filters and the named details outlet are preserved without signal writes inside pagination computations

### Requirement: Fill complete grid rows with container queries

The file grid SHALL use CSS container queries starting at one column, with supported column counts 1, 2, 3, 4, 6, 8 and 12. Each count SHALL divide every offered page size. Folder navigation SHALL use RouterLink and queryParams bindings, preserving limit while resetting search and offset.

#### Scenario: Render a full server page

- **WHEN** a response contains the full requested 24, 48, 72, 96 or 120 items
- **THEN** all rows are filled at every supported column breakpoint
- **AND** the column count depends on the content container width rather than viewport width

#### Scenario: Follow a folder link

- **WHEN** an operator activates a folder card or the Up link
- **THEN** routerLink navigates to that folder, clears the details outlet and resets offset/search
- **AND** the selected limit is retained

#### Scenario: Reuse folder context during pagination

- **WHEN** offset or limit changes inside the same folder
- **THEN** only list query parameters change; cached folder metadata and ancestors are reused

### Requirement: Derive upload cards from mutation state

The library SHALL select pending/error uploads through injectMutationState and mutationKey. File, destination, status and error SHALL come from mutation variables/state, without a separate upload-state service or mirrored card array. Options SHALL keep only percentages by local upload key and remove them when the attempt settles.

#### Scenario: Display concurrent uploads independently

- **WHEN** several upload mutations are pending
- **THEN** the current folder displays each matching attempt, tracked by mutation identity
- **AND** each card reads progress using the key from that mutation's variables

#### Scenario: Keep requests across navigation

- **WHEN** an operator navigates away from the library while an upload is pending
- **THEN** the mutation can finish and reconcile the server result independently of the destroyed component
- **AND** returning while it remains pending reconstructs the card from mutation state

#### Scenario: Retry a failed upload

- **WHEN** an operator retries a failed mutation card
- **THEN** the old attempt is removed and a new mutation uses the same File and folder with a fresh AbortController
- **AND** completed/failed attempts do not retain progress registry entries

### Requirement: Render Signal Form validation through shared fields

Validated editor inputs SHALL use hlm-field and hlmFieldLabel, with validation messages defined in schema message options. The owned shared field SHALL render its bound Signal Form errors after touched or an invalid submit attempt, without duplicated error loops in feature templates.

#### Scenario: Display a required-name error

- **WHEN** the operator touches an empty name or submits the invalid editor
- **THEN** HlmField renders the required error message from the Signal Form schema through HlmFieldError
- **AND** the visible error is associated with the control through aria-describedby

#### Scenario: Provide explicit field errors

- **WHEN** a consumer projects custom HlmFieldError content
- **THEN** automatic error output is suppressed to avoid duplicate messages

### Requirement: Submit only dirty editor fields

Folder and asset editors SHALL build PATCH bodies from Signal Forms dirty fields through shared getDirtyValues. Pristine fields SHALL be omitted, explicit null/empty values SHALL be preserved and empty patches SHALL NOT be sent. Update mutation variables SHALL contain only id and patch; optimistic baselines SHALL be resolved inside data-access.

#### Scenario: Edit one field

- **WHEN** an operator edits only the asset name
- **THEN** PATCH contains name without alt or previewAssetId
- **AND** the caller supplies id and patch without a previous DTO

#### Scenario: Clear a video cover

- **WHEN** an operator removes the selected cover
- **THEN** previewAssetId is marked dirty and PATCH contains previewAssetId=null

#### Scenario: Update without a cached baseline

- **WHEN** update options cannot resolve the entity from cached data
- **THEN** the HTTP request executes without fabricating an optimistic DTO
- **AND** success invalidates Media queries to restore authoritative membership and totals

### Requirement: Show loading skeletons with media-card geometry

Uncached listing loads SHALL show non-interactive skeleton cards preserving preview and metadata geometry. Unknown folder counts or counts greater than the selected limit SHALL use two responsive rows. A known count no greater than the limit SHALL determine the exact number of skeleton cards, including zero.

#### Scenario: Load the library without a known item count

- **WHEN** the first listing is loading and its folder item count is unknown
- **THEN** the grid shows exactly two visible skeleton rows at its current column count
- **AND** preview, name and content-type areas use placeholders without links or action menus
- **AND** loading is exposed to assistive technology without announcing individual decorative skeletons

#### Scenario: Enter a folder with a known count

- **WHEN** a folder listing is loading and the folder's known direct item count does not exceed the selected page limit
- **THEN** exactly that many skeleton cards are shown
- **AND** a known empty folder shows zero skeleton cards while its request is pending
- **AND** a newer cached zero count is not overridden by an older nonzero folder representation

#### Scenario: Enter a larger folder

- **WHEN** the known direct item count exceeds the selected limit
- **THEN** loading uses two responsive skeleton rows rather than one skeleton for every folder item

#### Scenario: Replace loading placeholders with cards

- **WHEN** the listing response replaces skeletons with loaded media cards
- **THEN** card preview proportions and metadata space remain stable without a card-height jump

### Requirement: Preserve browsing results within the current directory

Search, offset and limit changes within the same directory SHALL retain previous listing results while the replacement request is pending. Directory changes SHALL NOT display another directory's results as provisional contents. Empty-result decisions and pagination bounds correction SHALL wait for the replacement response.

#### Scenario: Search or paginate without clearing loaded cards

- **WHEN** an operator changes search, offset or limit within a folder whose results are already loaded
- **THEN** previous cards remain visible while the new URL state is requested
- **AND** loading skeletons do not replace those previous results
- **AND** the replacement response supplies the new cards and total

#### Scenario: Wait for the new total before correcting pagination

- **WHEN** a new offset is being requested while previous results and their total are still displayed
- **THEN** that previous total does not cause the requested offset to be rewritten
- **AND** a provisional empty result does not display a successful empty-folder or no-match state

#### Scenario: Enter a different directory

- **WHEN** an operator changes directories and the destination listing is not cached
- **THEN** the previous directory's cards are not displayed as the destination's contents
- **AND** the destination uses its applicable loading skeleton count

### Requirement: Navigate up whenever parent metadata is known

Up navigation SHALL remain available during listing or folder-context loading and failures when the current folder's parent is known from current or cached metadata. A known null parent SHALL identify the library root. At root or without known parent metadata, Up SHALL remain disabled and preserve its space.

#### Scenario: Navigate up while requests are pending

- **WHEN** an operator enters a folder whose cached metadata identifies its parent while destination requests are pending
- **THEN** Up already links to that parent and can be activated without waiting for those requests
- **AND** the selected page limit is retained while search, offset and details selection are reset

#### Scenario: Recover through the parent after a request failure

- **WHEN** a listing or folder-context request fails and the current folder's parent is still known
- **THEN** Up remains available to navigate to that parent, including the library root
- **AND** the same known parent can receive an otherwise permitted item drop

#### Scenario: Do not invent an unknown parent

- **WHEN** the operator is at root or no current or cached metadata identifies the current folder's parent
- **THEN** Up has disabled semantics and no active destination and refuses drops
- **AND** an unknown parent is not assumed to be the library root

#### Scenario: Keep editor guards authoritative

- **WHEN** an operator activates Up with an unsaved details draft
- **THEN** the existing discard confirmation determines whether navigation proceeds
- **AND** continuing editing preserves the current location and draft

### Requirement: Separate drag feedback from ordinary navigation

Ordinary clicks and taps SHALL NOT produce drag-target treatment. Dragging SHALL distinguish permitted destinations from the active target, with delayed highlight entry and no highlight-removal delay when the drag ends. Keyboard navigation SHALL retain a visible focus indicator independently of pointer navigation and drag feedback.

#### Scenario: Avoid starting a drag during an ordinary click

- **WHEN** a pointer is released or moved before the drag-start threshold
- **THEN** ordinary folder activation does not start drag feedback
- **AND** drag initiation requires a 120ms mouse hold or a 220ms touch hold before dragging

#### Scenario: Reveal the parent as an eligible destination

- **WHEN** an item is dragged and the Up control can receive it
- **THEN** the control's eligible border treatment appears after 160ms even before the item is over it
- **AND** an ineligible or disabled Up control does not receive that treatment

#### Scenario: Highlight the active parent drop target

- **WHEN** the dragged item enters the eligible Up control
- **THEN** its active border and ring appear after a 160ms delay
- **AND** leaving the control starts removing its active ring without an extra delay
- **AND** ending the drag starts removing all drag feedback with zero delay

#### Scenario: Enable Up after entering a folder

- **WHEN** a click or tap navigates from root into a folder and Up becomes enabled
- **THEN** enabling the control alone does not display a drag ring or pointer-origin focus ring
- **AND** reaching the same control with Tab still shows keyboard focus
