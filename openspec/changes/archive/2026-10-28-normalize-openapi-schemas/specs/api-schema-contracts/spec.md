## Purpose

Provide readable and reusable public OpenAPI schemas that describe the actual HTTP representations independently of internal DTO naming.

## ADDED Requirements

### Requirement: Publish schema identifiers without DTO suffixes

Public OpenAPI schema identifiers SHALL omit the `Dto` suffix from represented type names, including generic arguments. References SHALL resolve to the renamed components, and unrelated request, result, and enum names SHALL retain their meaning.

#### Scenario: Inspect resource and pagination schemas

- **WHEN** a consumer reads the development OpenAPI document
- **THEN** resource schemas include `Game`, `Map`, and `ImageAsset` rather than DTO-suffixed names
- **AND** generic pagination identifiers omit DTO suffixes in their item names
- **AND** optional PATCH values remain inline and preserve optional versus nullable semantics

### Requirement: Share canonical concrete media schemas

Media unions and concrete media references SHALL reuse `MediaFolder`, `ImageAsset`, and `VideoAsset` component schemas. Union discriminator mappings and variant references SHALL identify these components without parent-name prefixes. Concrete schemas SHALL retain complete fields and required literal `type` values.

#### Scenario: Inspect mixed and asset-only unions

- **WHEN** a consumer inspects `MediaItem` and `MediaAsset`
- **THEN** `MediaItem` maps folder, image, and video to their canonical schemas
- **AND** `MediaAsset` maps only image and video to the same canonical schemas
- **AND** no parent-prefixed concrete media duplicates remain

### Requirement: Describe concrete media without a single-variant discriminator

Concrete media schemas SHALL be objects without a single-variant polymorphic wrapper or discriminator. Video `preview` SHALL describe nullable `ImageAsset` directly; its image representation SHALL preserve required `type` with the sole value `image`. Existing HTTP payloads and runtime serialization SHALL remain unchanged.

#### Scenario: Inspect a video preview

- **WHEN** a consumer expands a video's non-null preview
- **THEN** it sees the `ImageAsset` object fields without selecting a single image variant
- **AND** null remains an allowed preview value
- **AND** concrete image, video, and folder JSON continues to emit exactly one canonical `type` property
