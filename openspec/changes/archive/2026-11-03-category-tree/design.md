# Design

## Context

See [proposal.md](proposal.md) for motivation and scope.

The backend currently separates CategoryGroups and Categories at Domain, Application and Api folder level. ListCategoryGroups returns six-field group DTOs; ListCategories reads one group's complete categories. Category PATCH already supports atomic same-map transfer but preserves an independently writable numeric order. Both lists allow ties and sort by order/createdAt/id.

Category icons are required Guid references to library images. Group icons do not exist. The media deletion handler checks category icons but must also check groups before deleting bytes. The existing EF transaction abstraction is used for writes and SQLite is the only current provider.

The editor's MapStyles child owns style controls inside the editor's responsive aside. Prepared handwritten categories files already separate query keys, variables and options; these are user changes and the implementation must evolve them. Their current getList is per group and mutation cache reconciliation is not yet present.

## Implementation staging

The user explicitly requested the entire backend and UI in this implementation step, with simple success-only invalidation in category/group query options. Optimistic cache transformations, rollback snapshots and mutation scopes described for the next stage remain deferred under task 5.3. Current UI retains server data until refresh and disables conflicting writes while pending.

## Goals / Non-Goals

**Goals:** Make the existing two-level model easy to read and manipulate atomically, keep UI state in TanStack Query, and reuse the editor's layout and Spartan composition.

**Non-Goals:** A universal tree entity, a large map aggregate containing every marker, a repository framework, additional module projects, a second database provider, category SignalR events, or a CRDT/revision protocol for collaborative editing.

## Decisions

### 1. One functional Categories area, separate entities

Use this organization within existing layer projects:

```text
Domain/Categories/
  Category.cs
  CategoryErrors.cs
  CategoryGroup.cs
  CategoryGroupErrors.cs

Application/Categories/
  Groups/
    CreateCategoryGroup/
    ListCategoryGroups/
    UpdateCategoryGroup/
    DeleteCategoryGroup/
    CategoryGroupDto.cs
    ...
  CreateCategory/
  UpdateCategory/
  DeleteCategory/
  CategoryDto.cs
  ...

Api/Categories/
  Endpoints.cs
  Groups/
    CreateCategoryGroupRequest.cs
    UpdateCategoryGroupRequest.cs
    ...
  CreateCategoryRequest.cs
  UpdateCategoryRequest.cs
  ...
```

Domain entities share GameAtlas.Domain.Categories; Application retains operation-specific handlers and validators, and the existing ListCategoryGroups operation ID. One Api Endpoints file maps both route families with WithTags("Categories"). Markers stay in their existing feature. EF configurations retain their current infrastructure location and explicit table names.

A folder is an ownership boundary, not a requirement to make CategoryGroup the sole aggregate root. Category and group rules stay in their entities; Application orchestrates cross-entity placement using IApplicationDbContext directly.

Alternatives: retaining parallel top-level folders hides the now-shared use case; a universal parentId node introduces unsupported depth, ambiguous marker targets and a larger migration.

### 2. Extend the existing group collection read

Keep GET /games/{gameId}/maps/{mapId}/category-groups and ListCategoryGroups. Return CategoryGroup[]; each group adds nullable icon, markersCount and categories. Complete Category retains its nine current fields but icon becomes nullable and order becomes server-owned.

Group POST/PATCH return the same complete group shape, including children, to support authoritative reconciliation. Category POST/PATCH retain complete category responses. DELETE stays empty 204. Remove only category collection GET and its ListCategories handler/client operation; its POST route remains, so GET receives 405.

Read the tree with a bounded number of projected AsNoTracking queries inside a consistent read transaction: validate game/map ownership, retrieve ordered groups and categories with their images, and obtain grouped marker counts. Assemble DTOs with pure mapping functions; sum category counts into groups. Do not issue a query per node or materialize marker entities. Shared image metadata remains current. No pagination or duplicate read API is needed for this bounded editor collection.

Alternative: a new category-tree endpoint duplicates the existing collection read. Moving all mutation URLs is unrelated to combining OpenAPI tags and is avoided.

### 3. Relative placement through existing POST/PATCH operations

Replace writable order with beforeId: optional nullable UUID. Keep integer order in responses for persisted display sequence; do not expose fractional ranks.

| Operation                          | beforeId omitted      | beforeId null         | beforeId is sibling ID            |
| ---------------------------------- | --------------------- | --------------------- | --------------------------------- |
| Create category/group              | Append                | Append                | Insert before sibling             |
| Update metadata, same parent       | Preserve position     | Move to end           | Move before sibling               |
| Transfer category to another group | Append at destination | Append at destination | Insert before destination sibling |

Category PATCH uses categoryGroupId for destination and its existing route group as current ownership. Missing/foreign destination or anchor returns 404. A self anchor returns 422, malformed GUID 400, and name conflict 409. Numeric order input becomes unknown input (400), including creation. Group map membership remains immutable.

Apply the operation to the current committed sibling sequence: validate full resulting metadata and references, remove the moving entity, locate the anchor in the resulting destination, insert, and normalize affected positions to 1..N. Cross-group moves normalize source and destination. Creation follows the same placement rules. Deletion preserves surviving order values and timestamps; gaps are normalized at the next actual structural operation. If final membership/sequence and metadata do not change, do not normalize merely for housekeeping or advance timestamps.

Update the moved/edited entity's updatedAt only for an effective change. Siblings whose order changes solely due to normalization keep metadata timestamps. Group placement never updates its child metadata, and category placement never updates parent map/game/group timestamps.

Use database transaction serialization for structural reads and writes, including creation and competing deletions, so anchors and membership cannot change between validation and commit. Reuse the SQLite transaction path and verify the concrete provider behavior with concurrent integration tests. Do not replace it with a process-local lock or introduce PostgreSQL infrastructure.

Relative operations apply to the committed state when processed. Two valid concurrent moves serialize; an anchor deleted or moved out of the destination before processing causes 404 and client reconciliation. A tree revision/If-Match protocol is not needed for this scope: operations do not replace the entire tree, and live multi-user editing is excluded.

Alternatives: client-assigned integers retain collisions; fractional/LexoRank values add allocation/rebalance logic; full-tree replacement sends more data and can overwrite unrelated concurrent moves.

### 4. Nullable shared-media icons and derived counts

Change Category.IconAssetId to Guid? and add CategoryGroup.IconAssetId as Guid?. Use existing Optional<Guid?> PATCH semantics: omission preserves; explicit null clears. Resolve/check non-null values only, including metadata updates to iconless resources. Empty GUID is a validly shaped missing resource (404), not a substitute for no icon.

Add a restrictive group-image FK and extend the media deletion preflight before any physical-file deletion. Preserve the existing category FK deletion guard. Clearing references or cascading content deletion does not remove media bytes.

Keep category color required and normalized as today even when the icon is absent; adding group color or icon inheritance is outside scope. Group counts are read-only sums of current category marker counts, with no persisted counters.

### 5. One tree query and conservative optimistic mutations

Evolve the prepared categories files rather than replacing them with a new abstraction. getList(gameId, mapId) returns CategoryGroup[] and the key is ['categories', { gameId, mapId }]. Keep typed variables, generated transport, toPromise and explicit query/mutation return types. Add group factories alongside existing category factories, using descriptive names. Remove groupId only from the tree query key, not mutation route variables.

All category/group writes share a per-map mutation scope. The UI prevents a second conflicting write while the map's category mutation is pending, including forms and keyboard/drop actions; pending does not block expansion. This avoids queued writes using an obsolete route group after a transfer. Use TanStack scope rather than a custom promise queue. Do not assume scope serializes onMutate: cache updates still check current cached ownership.

For a known tree: cancel reads, capture a rollback baseline, immutably rename or place the row, update categoryGroupId, normalize affected cached order and adjust group counts using the moved category's markersCount. Keep pure tree transformations near the options. Do not create a separate persistent tree store.

On success, use mutation variables for authoritative placement, returned server identity/metadata, and known sibling arrays to reconcile order. Do not replace a newer whole subtree blindly with a group response. Creation waits for server IDs and deletion waits for persistence. If cached membership or concurrent changes make reconstruction uncertain, narrowly invalidate the current map tree; do not refetch every successful mutation by default.

Rollback only when the affected cache still matches the optimistic version owned by the failing operation. Otherwise reconcile the current map tree, preserving accepted changes. Resolve ambiguous transport outcomes through HTTP reconciliation. Late reads, map switches and obsolete responses must not overwrite newer accepted state. Features compose callbacks for toasts/dialog closure without replacing data-access lifecycle work.

Alternative: optimistic snapshots blindly replacing the whole tree can lose later writes; a second component-owned resource store drifts from Query state.

### 6. Angular Aria tree, Spartan composition and existing CDK drag/drop

The installed Spartan catalog has no Tree component. Add a compatible @angular/aria dependency and use Tree, TreeItem and TreeItemGroup from @angular/aria/tree. Use Spartan for dropdowns, dialogs, fields, empty/skeleton states and buttons, shared project wrappers and Material Symbols Rounded icons. Use semantic project tokens for row focus, hover and selected states; no screenshot-specific palette or Lucide substitution.

References checked during exploration: [Spartan components](https://spartan.ng/components) and [Angular Aria Tree](https://angular.dev/guide/aria/tree). The tree supplies keyboard navigation/expansion, not drag/drop or application CRUD. Verify nested action-button focus/keyboard behavior against its installed API rather than treating the whole row as one button.

Add a MapCategories child under MapStyles in the same aside. Rename styles-only panel labels/toggle copy to describe map content. The child owns tree retrieval, dialogs, expansion and keyboard/drop actions. Keep styles, camera and live style subscriptions independent. Use initially expanded groups for discoverability; expansion stays local across mutation reconciliation and does not require persistence.

Header Add menu offers Add group and Add category; group menus also offer Add category. Category forms choose an existing group; when there are no groups, direct the user to group creation rather than silently creating one. Use Signal Forms, dirty PATCH extraction and the existing media picker port. Group forms contain name and optional icon; category forms also contain required color and destination group.

CDK root drop list accepts only group drags; connected child lists accept category drags. Keep group headers as valid category destinations, including empty/collapsed groups; dropping on a header appends and reveals the moved child. Drag starts only from the labelled handle. Block category-as-root, group-as-child and category nesting. Map destination index to the next surviving sibling's ID or null after removing the dragged item. No-op drops send no request.

Menus provide Move up, Move down and Move to group for keyboard equivalence; disable unavailable boundary actions. Preserve focus by stable server IDs after movement and restore to a surviving row or Add control after deletion. Arrow keys navigate/expand without triggering menus or drag; embedded actions must not activate the row accidentally. Preserve handle interaction on touch, the editor's scrolling behavior, tooltip suppression during drag and reduced-motion support used by styles.

Use an optional icon slot with no broken image or forced generic image; reserve consistent row alignment. Counts and menus remain visible while names truncate. Category failures are local to the section and do not disable the map.

## Risks / Trade-offs

- [Breaking API and generated contracts] -> Update backend, OpenAPI and maps client together; remove stale services and migrate consumers in one change.
- [Dense ordering writes several siblings] -> Scope work to affected sibling sets and keep the simple model for current editor collections; no speculative rank service.
- [Concurrent structural requests or image deletion] -> Validate inside serialized database transactions and retain restrictive FKs; test failures before commit.
- [Optimistic state differs from an external writer] -> Apply relative operations to latest state and reconcile the map tree on conflict or ambiguity; no claim of live collaboration.
- [Aria, nested action buttons and CDK interactions] -> Manually verify keyboard focus, touch, collapsed/empty drops and scrolling against the installed versions.
- [Frontend tests are deferred] -> Use formatting, affected Nx lint, production admin build and explicit manual acceptance checks; do not add or run frontend unit/E2E suites.

## Migration Plan

1. Move feature files/namespaces and update references without changing table names or rewriting historical migrations. Preserve the user's prepared frontend changes.
2. Add a forward EF migration making category icons nullable, adding nullable group icon FK/index, and normalizing existing sibling order by order/createdAt/id. Preserve IDs, ownership, timestamps, markers, asset references and visible sequence.
3. Apply the migration explicitly with EF tooling, then deploy revised API and regenerate maps clients over HTTP through its uncached Nx target. Coordinate frontend/API deployment because numeric order inputs and ListCategories are removed.
4. Verify backend builds and focused category/group/media migration, ordering, API and concurrency tests. Verify frontend formatting, affected lint and production build, then manual interaction checks.
5. Update owning architecture/API/data-access/UI guidance and affected READMEs, and run documentation/link and whitespace checks.
6. Roll back application deployments together. Schema rollback to required category icons is unsafe after iconless categories exist; back up first and prefer a forward fix. A downgrade must explicitly reject null category icons without deleting data or inventing media references. Group icon references are also lost by dropping the added column, so preserve a backup before downgrade.

## Follow-up: group colors and creation controls

Groups now carry a required normalized hex color, defaulting to #F59E0B for omitted creation values and existing rows. PATCH preserves omission and rejects null. An additive CategoryGroupColor migration follows CategoryTree. The editor exposes color for both row kinds and renders colored map-pin fallback icons for categories; groups without library images render no icon. A Spartan Button Group splits the primary creation action from the dropdown; menus use Material Symbols icons. This does not change the deferred optimistic-update scope.
