# Proposal

## Why

Game cards and editors show placeholders because the game contract lacks descriptive metadata, banner images, content counts and lightweight map previews. Exposing these values together allows the administration UI to display and edit actual persisted data.

## What Changes

- **BREAKING**: Rename response `image` to `cover` and request `imageAssetId` to `coverId`; add nullable `banner` with `bannerId` input.
- Add nullable release date, developer and plain-text description to game creation, editing and responses.
- Include game-wide marker/category counts and read-only map summaries containing only `id`, `name` and nullable `preview` from the default style source.
- Replace administration placeholders with API data, enable metadata and banner editing, and keep description formatting buttons disabled.
- Make game pagination deterministic with createdAt ascending by default and id ascending as the tie-breaker for every sort.
- Preserve existing map APIs and default-style selection. Rich text, map CRUD UI, SEO features and frontend unit/E2E test work are out of scope.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-catalog`: Extend persisted metadata and the shared game representation; rename image inputs and outputs; expose counts and map summaries.
- `admin-games`: Edit the added fields and display real banners, descriptions, counts and map previews.

## Impact

Affected areas include Game domain/persistence and a new additive migration, game application handlers and DTO mapping, API contracts/OpenAPI and contract tests, generated Games clients and their consumers, shared game forms, administration cards and editor pages, and API documentation. Reuse canonical ImageAsset schemas and existing media selection; no new runtime dependencies are required.
