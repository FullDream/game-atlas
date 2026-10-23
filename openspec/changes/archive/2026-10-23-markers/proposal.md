# Proposal

## Why

GameAtlas needs markers to place category-owned points on game maps with descriptions and ordered image/video attachments. Existing categories already provide icons and colors, but their marker counts are placeholders until marker storage and operations exist.

## What Changes

- Add GET, POST, PATCH, and DELETE under `/games/{gameId}/markers`, returning complete marker representations and an unpaginated, unfiltered game-wide list.
- Add required category membership, nonunique validated names, optional plain-text descriptions, finite X/Y positions, and server-generated UUID v7 identities and UTC timestamps.
- Support recursive position merge patches and atomic category transfers within one game.
- Support an ordered media union with `image`, `youtubeVideo`, and `video` variants, internal child identities, whole-array replacement, image metadata, and optional video previews.
- Physically delete markers and media records and extend the existing parent cascade chain to them, without deleting linked files.
- Replace category count placeholders with computed counts in every category representation.
- Document polymorphic media with OpenAPI `oneOf` and a `type` discriminator, and cover the contract with domain, HTTP, persistence, rollback, and schema tests.
- Keep individual-marker GET, pagination, category filters, spatial filters, search, marker display order/status, separate media endpoints, public media IDs, uploads, file deletion, transcoding, and remote resource checks out of scope. Existing map management remains unchanged.

## Capabilities

### New Capabilities

- `markers`: Game-scoped category markers with finite positions, descriptions, polymorphic ordered media, atomic merge-patch updates, transfers, listing, and physical/cascading deletion.

### Modified Capabilities

- `categories`: Replace the temporary zero marker count with the actual read-only count, preserving the existing category DTO shape and other behavior.

## Impact

- Add Markers features across Domain, Application, Api, and Infrastructure, reusing IApplicationDbContext, transaction, validation, Optional, OutcomeResult, and PATCH integrations.
- Add the Marker aggregate, Position value object, media child hierarchy, typed contracts, mapping extensions, EF Core configurations, and an EF CLI migration for Markers and MarkerMedia.
- Update category DTO projections and mutation responses to compute counts without loading marker collections or persisting counters.
- Add focused polymorphism schema support only where needed to meet the union contract, plus API examples and migration documentation. No additional provider, external service, repository layer, or module project is required.
