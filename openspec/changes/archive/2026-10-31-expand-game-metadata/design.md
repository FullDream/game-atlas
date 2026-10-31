# Design

## Context

See [proposal](proposal.md) for motivation. Game currently stores a required image reference and basic metadata. All game operations share GameDto. Map APIs return full geometry and styles; defaultStyleId selects the earliest ready style by persisted creation sequence. Administration already has disabled metadata placeholders and library cover selection.

This design is necessary because the change spans persistence, API contracts, generated clients and administration UI.

## Goals / Non-Goals

Provide one consistent game representation without loading full map content or issuing a request per game. Keep map ownership and mutations in existing map features. Do not introduce persisted counters, a selected map, rich text, new media processing or a new service abstraction.

## Decisions

### Persist optional metadata on Game

Use nullable DateOnly for releaseDate, nullable developer and plain-text description, and a nullable banner image relationship. Keep the cover relationship required. Developer is trimmed with blank values normalized to null; description preserves line breaks and normalizes empty text to null. Avoid arbitrary new text length limits in this scope; retain existing name/slug validation. Calendar dates avoid timezone shifts that timestamps would introduce. Optional values allow existing games and incomplete drafts to remain valid.

### Rename references together

Use coverId and bannerId in creation/PATCH, with canonical ImageAsset objects at cover and banner in responses. Rename the domain/persistence cover terminology consistently rather than retaining an image alias. Reuse current media validation, FK constraints and usage/deletion protections for both references; update media reference checks to include banners. PATCH omission preserves values, null clears only nullable values, and validation is atomic. Preserve no-op timestamp behavior for normalized equivalent values.

This is a coordinated breaking change; keeping old aliases would create ambiguous patch semantics and prolong duplicate terminology.

### Build summaries and counts in bounded reads

Keep the existing paginated game query as the page boundary. Default to createdAt ascending when sortBy is omitted, and append id ascending to every ordering before pagination. Resolve counts and ordered map summaries only for returned game IDs using grouped/projected reads, then resolve distinct cover, banner and default-source asset IDs in batches. Keep data reads directly in the existing GetGameHandler and ListGamesHandler. Create/update handlers dispatch the existing GetGame after commit to obtain their persisted response. Mapping extensions only synchronously construct DTOs from materialized inputs; they never accept a context or perform I/O. No additional query class, service or repository is introduced. Do not include marker, category or full style graphs. Keep category and marker counts in two separate SQL GROUP BY queries with scalar count projections. Avoid joins that multiply category and marker counts. Read-only queries must not change game timestamps.

Map summary fields are exactly id, name and preview. Preserve createdAt/id map ordering and existing ready-style creation-sequence selection. Return the original library source ImageAsset, not a tile URL or newly generated thumbnail. Full map APIs remain unchanged. Using maps.length avoids a redundant mapsCount field; a truncated server map list would make that count incorrect, so return all summaries.

### Connect existing forms and cards

Regenerate Games clients explicitly from the running backend OpenAPI. Update all consumers of the renamed fields, including shared/public consumers where present. Extend existing form models, dirty-value patches, validation, media picker previews, reset/discard and route protection to cover added fields. Translate cleared optional form values to null. Keep local unsaved edits across refetches and failures. Treat release dates as date strings without timezone conversion.

Render descriptions as text and keep every formatting control disabled. Replace list-card placeholders and editor map placeholders using game data; retain neutral null-image and empty-content states. Full map CRUD UI and SEO placeholders remain outside scope. Existing query refresh behavior is sufficient; no new live subscription or polling is introduced for cards.

## Risks / Trade-offs

- Breaking naming changes require matching server/client deployment; regenerate and migrate consumers in the same change.
- All map summaries increase response size for games with many maps; bound enrichment to the current game page and avoid full map graphs. Revisit a dedicated summary endpoint only with measured need.
- Concurrent content changes can occur between bounded reads; responses are current read projections, not a transactional snapshot guarantee.
- Shared images can be referenced in both cover and banner; include both in usage checks and never delete assets through game mutations.
- Existing catalog requirements repeatedly describe seven fields; update every affected requirement and API document to prevent contradictory contracts.

## Migration Plan

Add a new migration without changing applied migrations. Rename the existing image reference column to cover terminology while preserving IDs, references and timestamps; add nullable banner and descriptive metadata with appropriate image FK/index configuration. Existing new values are null. Verify both a fresh database and an upgrade from a database containing games and maps. Apply migrations explicitly according to repository guidance, never automatically reset data.

Deploy server and regenerated consumers together. For rollback, retain a database backup; reverting schema after new metadata is entered loses those new values, so prefer a forward fix. No automatic destructive rollback or application startup migration is introduced.
