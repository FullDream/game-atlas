# Proposal

## Why

OpenAPI exposes C# DTO suffixes, duplicates concrete media schemas under union-specific names, and presents video previews as a choice with only one image variant. These details make Scalar and generated frontend contracts harder to read.

## What Changes

- **BREAKING**: Rename public OpenAPI schema identifiers to omit DTO suffixes, including generic arguments; regenerate the existing Games and Media frontend clients and migrate consumers to the new names.
- Reuse canonical `MediaFolder`, `ImageAsset`, and `VideoAsset` schemas across media unions and concrete references, with consistent discriminator mappings.
- Describe concrete media objects without single-variant polymorphic wrappers; retain required literal `type` values and nullable previews.
- Preserve runtime JSON, C# DTO names, endpoint behavior, HTTP-based client generation, and development Scalar hosting. Build-time OpenAPI generation is explicitly out of scope.

## Capabilities

### New Capabilities

- `api-schema-contracts`: Public schema naming and reusable representations in the generated OpenAPI document.

### Modified Capabilities

- `frontend-api-clients`: Generated and handwritten frontend contracts consume canonical public schema names without DTO aliases.

## Impact

API OpenAPI configuration and transformers, existing contract tests, generated Games and Media clients, their frontend consumers and public exports, and API/data-access conventions. No package upgrades, database changes, or frontend test additions are required.
