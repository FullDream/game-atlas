# Design

## Context

See [proposal.md](proposal.md) for motivation and the game-catalog and media-library deltas for behavior. `Game.Image`, both game request types, commands, validators, and frontend `Game.image` currently use strings. Application game mapping has both entity and query projections. Listing projects the page directly to DTOs. Category icons already use required image IDs, `FindImageAsync`, shared media DTO mapping, restrictive foreign keys, and transaction-scoped attachment operations. Media deletion checks categories, styles, markers, and previews but not games.

## Goals / Non-Goals

**Goals:** Reuse the existing image representation and relationship lifecycle, preserve bounded page loading, and keep shared asset metadata authoritative.

**Non-Goals:** New abstractions for attachments, new media endpoints, eager loading of maps in catalog queries, or duplicated image metadata on games. The proposal defines the product scope and legacy-data boundary.

## Decisions

### Required image relationship in Domain

Replace the game string with `Guid ImageAssetId` and an `ImageAsset Image` navigation, configured as a required relationship with a restrictive delete behavior and an index. Domain creation/update enforce non-null, nonempty image IDs; Application verifies existence and subtype. Domain remains independent of DTOs. Comparing IDs preserves no-op update behavior even if the loaded image object's metadata has changed.

This follows category icon persistence while adding the navigation needed for game image loading. Keeping a string would retain ambiguous references; copying asset metadata would make shared edits stale. A nullable relationship is outside the agreed scope.

### Separate write selection from response representation

Creation takes `Guid? ImageAssetId` to report omitted/null IDs as validation errors; PATCH takes `Optional<Guid?> ImageAssetId`. Keep name, slug, and status rules and both merge-patch media types. Remove string image length/trim validation and the obsolete metadata limit. Malformed UUIDs fail HTTP binding with 400, required/empty IDs with 422. Reuse `FindImageAsync` for 404 missing assets and 422 folders/videos. Legacy `image` cannot satisfy creation; creation continues its existing extra-field policy, while PATCH rejects it as unknown.

`GameDto.Image` becomes `ImageAssetDto`. Reuse the existing canonical DTO and mapping, including the single `type=image` discriminator and persisted placeholder. This avoids a game-specific cover schema. Keep seven root response fields and omit response `imageAssetId`.

### Resolve images with bounded reads

Adapt entity mapping and query mapping together. Prefer a game query helper that includes the required image, materialize only the requested page, then reuse shared image mapping. Keep count separate and retain existing filters, sorting and pagination before materialization. A single joined page read plus count avoids per-game lookups. Do not retain a custom nested mapping call in an SQL projection unless translation is verified.

Creation uses the validated image to build the response. Update loads the current image and resolves a replacement only when supplied; the response always maps the selected image, including no-op or metadata-only patches. Reads do not probe physical file availability. Asset metadata changes do not update game timestamps.

### Serialize assignments against media deletion

Wrap game creation and update reference resolution and persistence in the existing transaction pattern used by category attachments. Add `Games.AnyAsync(game => game.ImageAssetId == id)` to media deletion's in-use checks before file removal. Retain the restrictive FK as the persistence backstop. Exercise create/replace versus delete races with SQLite, following `MediaReferenceRaceTests`; do not depend solely on a late FK exception after bytes have already been deleted.

Game deletion and cover replacement remove only the relationship. No asset deletion, copying, expiry, or map cleanup entry is introduced for covers. Existing owned-map cleanup continues independently.

### Regenerate the existing frontend client

Run the existing Games-tag Orval generation against the updated running API over HTTP. Referenced `ImageAsset` models may appear within the games library without generating Media operations. Keep public resource names and exports consistent. Adapt any handwritten image consumers to the object shape and any create/update callers to asset IDs. Do not introduce game editing UI or cross-domain service dependencies. Preserve frontend test coverage; unit/E2E test work remains deferred unless separately requested.

## Risks / Trade-offs

- Breaking request/response shape: update API docs and committed client output in the same change; no dual string/object contract.
- Shared-image payloads are larger: load only the selected page and reuse stored placeholders without image processing on reads.
- Asset-deletion races can otherwise leave missing cover files: use existing serialized attachment transactions, restrictive FKs, and focused concurrency tests.
- Existing tests construct games with arbitrary strings: update backend fixtures to create image records and review HTTP/OpenAPI assertions rather than weakening them.
- Existing legacy rows cannot be inferred as asset references: explicitly reject populated legacy game upgrades, following `CategoryIconAssets` guards.

## Migration Plan

Add a new EF migration and update the model snapshot; never edit applied migrations. Before changing columns, guard that legacy `Games` is empty. Replace `Image` with required `ImageAssetId`, add its index and restrictive FK. Verify the entire chain against a fresh disposable SQLite database and verify a populated pre-change database fails without data mutation.

Apply migrations explicitly through EF CLI. The user does not require preserving existing games, but no migration or startup path automatically resets a database or deletes files. For an existing installation, manually preparing a fresh database is a separate operational action. Rollback also guards nonempty games instead of fabricating legacy image strings; populated rollback uses a backup. Regenerate clients against the migrated API and release the changed contract and consumers together.
