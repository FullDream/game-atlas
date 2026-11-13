# Proposal

## Why

Maps need editable slugs in preparation for future public lookup endpoints. Games already have slugs, but their validation is duplicated and their domain representation is a plain string; a shared value object will give both resources the same invariant.

## What Changes

- Introduce a shared Domain `Slug` value object and an Infrastructure EF Core string converter; migrate game domain operations to the value object while preserving existing game HTTP behavior.
- **BREAKING**: Require a client-supplied slug alongside name when creating a map. Allow slug changes through the existing ID-based PATCH and include string slugs in complete map responses and game map summaries.
- Enforce map slug uniqueness within a game through a unique `(GameId, Slug)` index, retaining existing map name uniqueness and the global game slug index. Recognized slug conflicts return 409, including save-time races.
- Backfill existing maps with `map-{id:N}` using lowercase, hyphenless UUIDs, preserving identities, timestamps, content and files.
- Keep name mandatory at creation: nullable input types allow validation of invalid requests, never nullable persisted map names.
- Adapt existing admin map creation/editing controls and regenerate affected clients so the required creation field remains usable.
- Preserve existing generated-client compatibility exposed by regeneration: retain nullable value-type PATCH fields in OpenAPI and adapt Games sorting consumers to current generated enum names.
- Out of scope: public endpoints, slug-based queries or routes, redirects/history, automatic slug generation for new resources, transliteration, and changes to map rendering or processing.

## Capabilities

### New Capabilities

None. Shared value-object implementation is covered by the existing resource capabilities.

### Modified Capabilities

- `maps`: Required map slugs, metadata PATCH, representation, game-scoped uniqueness and safe migration.
- `game-catalog`: Add slug to lightweight map summaries; preserve existing game slug behavior during the internal refactor.
- `admin-maps`: Collect and edit slugs in existing map metadata forms using the backend rules.

## Impact

Domain Games/Maps and shared slug types; Application creation/update validation, handlers and mapping; API map contracts, OpenAPI and centralized conflict classification; Infrastructure converters, configuration and a new SQLite migration. Existing ID-based endpoints and queries retain their signatures and addressing. Frontend Maps/Games generated clients and map metadata form consumers require compatible updates. Backend regression coverage and migration checks are required; frontend unit/E2E work remains deferred under the current project policy. Update affected API, persistence and consumer documentation during implementation.
