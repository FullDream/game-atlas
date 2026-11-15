# Design

## Context

See [proposal.md](proposal.md) for motivation and scope. Existing API feature Endpoints.cs files mix management reads and mutations. ListGamesHandler counts all content, while GetMapHandler returns MapDto with styles and processing details, but no categories or markers. Game, map, group, category and marker publication statuses are independent. Existing tests cover this independence and the explicit bulk map publication operation.

The working tree already contains PublicGameDto, PublicGameMapDto and a ListPublicGames placeholder. Preserve unrelated edits and evolve these declarations into the dedicated contracts rather than adding competing types. The API currently has no administrator authorization setup, which makes a draft HTTP endpoint inappropriate in this change.

## Goals / Non-Goals

**Goals:** Independent public representations, consistent hierarchy visibility, and one reusable detailed-map read that can later serve protected preview.

**Non-Goals:** No changes to existing management handlers, schema IDs or publication writes; no new service/module project, repository abstraction, frontend generation or preview route.

## Decisions

### Endpoint organization

Create Games/PublicEndpoints.cs and Maps/PublicEndpoints.cs, registered explicitly in Program.cs, with distinct extension names and tags `Public Games` / `Public Maps`. Operations are ListPublicGames, GetPublicGame and GetPublicMap at the paths in the specs. Slug pairs match public navigation and game-scoped map uniqueness. Retaining existing ID-based management routes avoids compatibility changes. A mixed file with tag overrides was considered but would blur ownership.

### Dedicated read contracts

Use operation-specific Application handlers and public DTOs following the existing Application DTO convention; Api owns HTTP request binding. Keep mapping extensions separate, synchronous and pure.

- PublicGameCardDto: compact catalog fields from public-games.
- PublicGameDto: detail metadata, counts and PublicGameMapDto summaries.
- PublicMapDetailsDto: logical geometry, ready PublicMapStyleDto styles, PublicCategoryGroupDto trees and flat PublicMarkerDto markers.
- Public category/marker DTOs omit status and administrative timestamps; reuse PositionDto, MapTileGeometryDto and canonical media DTOs where representations match.

Reusing management DTOs would couple future management additions to anonymous responses. Separate cards avoid loading map arrays and long descriptions for every catalog item. Marker order is deterministic by id; group/category order follows persisted display order with id as a stable tie breaker. Map summaries use createdAt then id.

### Visibility and preview reuse

Add a narrow internal visibility enum with PublishedOnly and IncludeDrafts to the Application detailed-map retrieval path. The public endpoint constructs PublishedOnly explicitly and exposes no mode parameter. A later authorized preview handler can call the same read path with IncludeDrafts. This is an application behavior option, not an authorization mechanism.

Apply mode selection consistently to the game, map and descendants. PublishedOnly requires every ancestor and node to be published; IncludeDrafts removes publication predicates at every level but never removes game/map ownership constraints. Compute counts from the same visible sets used in responses. Missing and hidden roots produce identical not-found results. Do not alter publication records while reading.

A caller-controlled HTTP flag was rejected because it bypasses public visibility. Copying the map query for preview was rejected because filtering, styles and assembly would drift.

### Complete map response and persistence reads

The expected ceiling is approximately 1000–2000 markers per map. Load the complete visible content once rather than introducing viewport or marker paging APIs. Use IApplicationDbContext directly, AsNoTracking reads and bounded batched projections: root/ready styles, visible groups/categories, markers, and distinct media references. Avoid a giant Include graph, per-marker database calls and parallel operations on one DbContext.

Read the detailed aggregate within a transaction using the existing BeginTransactionAsync pattern so concurrent edits cannot create dangling category references or inconsistent counts. Materialize explicitly before pure DTO mapping. Preserve the selected ready default; blank or style-less maps are valid and return empty styles rather than introducing a new publication readiness condition. IncludeDrafts does not expose unfinished styles. Tile URLs and geometry come from existing rendering metadata, not newly constructed file paths.

### OpenAPI and compatibility

Reuse existing schema normalization and canonical media unions. New public schema names must not collide with management schemas. HTTP errors use OutcomeResult and centralized ProblemDetails. Public list query binding, validation, escaping and stable sorting follow existing ListGames behavior. Existing frontend generators are not executed; public operation tag filtering is documented for later adoption.

## Risks / Trade-offs

- Large marker descriptions or media metadata can enlarge a 2000-marker response → measure a representative serialized response and SQL round trips; no artificial truncation or hard 2000-item API limit.
- Visibility predicates can diverge across counts and detail reads → share narrow predicates where useful and test mixed-status ancestor chains, not only draft leaf nodes.
- Existing management routes and static media/tile URLs remain accessible under their current contracts → document that this change defines public read behavior and does not provide system-wide access protection.
- Complete reads hold a SQLite snapshot during assembly → keep the read transaction short and batch database operations.

## Migration Plan

Deploy additive handlers, endpoint registration, schemas and documentation without a migration or frontend deployment. Rollback removes the newly registered public routes. Existing frontend consumers continue using their existing operations; future preview ships only together with administrator authorization.
