# Proposal

## Why

Games currently expose an arbitrary image string, while other resources resolve shared media-library assets. Referencing a library image gives game consumers the same URL, dimensions, alt text, and placeholder contract and protects images that are in use.

## What Changes

- **BREAKING**: Replace creation and PATCH input `image` with required-on-creation `imageAssetId`. PATCH omission preserves the image; explicit null remains invalid.
- **BREAKING**: Return the canonical complete `ImageAsset` object in `Game.image` for creation, listing, and updates instead of a string.
- Persist a required game-to-image relationship; validate asset existence and image type and include games in asset deletion protection.
- Preserve shared assets when a game is deleted or its image is replaced, and resolve current asset metadata without changing game timestamps for asset-only edits.
- Add a migration supporting fresh databases, without converting legacy game strings or automatically deleting existing data. Populated legacy game upgrades are unsupported.
- Regenerate the games API client and adapt affected consumers and documentation.
- Out of scope: optional game images, legacy import/conversion, automatic database reset, new game editing UI, new media upload behavior, and unrelated catalog changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-catalog`: Select required library images by ID and return full shared images across game responses; define PATCH, validation, lifecycle, and migration behavior.
- `media-library`: Treat game cover references as asset uses that block deletion.

## Impact

Affected areas are Domain games, Application game commands/validation/mapping/queries, API game requests and OpenAPI metadata, Infrastructure EF configuration and a new migration, media deletion checks, backend tests and fixtures, and the generated games frontend client and its consumers. Reuse the existing `ImageAssetDto`, image lookup, transactions, and media mapping; no new dependencies or endpoints are needed. Existing canonical media-schema and frontend generation requirements remain applicable without changing those capabilities.
