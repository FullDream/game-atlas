# Tasks

## 1. Public game reads

- [x] 1.1 Evolve existing public game declarations into dedicated catalog cards, detail DTOs and map summaries; implement ListPublicGames validation and published-only query with visibility-aware counts. Verify focused validation and integration tests cover filtering before total/paging, literal search, sort ties, empty pages and invalid parameters.
- [x] 1.2 Add slug-based GetPublicGame with ordered published map summaries and canonical images. Verify integration tests cover hidden/missing roots, mixed-status descendant counts, default-style previews, empty games and unchanged timestamps.
- [x] 1.3 Add Games/PublicEndpoints.cs and registration with Public Games tags and ListPublicGames/GetPublicGame operation IDs. Verify HTTP and OpenAPI tests cover dedicated schemas, ProblemDetails, canonical media references and unchanged existing game operations.
- [x] 1.4 Document public game contracts and visibility in docs/api-conventions.md and feature documentation as needed; verify documentation matches the delivered operations and run node scripts/check-docs.mjs.

## 2. Complete public map retrieval

- [x] 2.1 Add public map/style/group/category/marker DTOs and separate pure mappings. Implement reusable detailed-map retrieval with PublishedOnly and IncludeDrafts modes, explicit ancestor scoping, ready styles, ordered category trees and flat markers. Verify focused integration tests cover complete draft hierarchies in preview mode, hidden ancestors in public mode, matching counts, media, style-less maps, style order/defaults and game-scoped slug collisions.
- [x] 2.2 Use batched no-tracking reads and a short consistent read transaction. Verify a representative 2000-marker fixture returns all content, no dangling category references, and a bounded SQL read count independent of marker count using the existing SqlReadCapture pattern; record serialized payload size without adding an arbitrary API cap.
- [x] 2.3 Add Maps/PublicEndpoints.cs and registration exposing GetPublicMap under Public Maps, always selecting PublishedOnly. Verify HTTP and OpenAPI tests cover path scope, 404 for hidden roots, ignored draft-enabling query input, complete nested schemas, canonical media references and no preview parameter or route.
- [x] 2.4 Document full map response, hierarchy filtering, internal preview mode and deferred protected preview route in docs/api-conventions.md and backend/architecture guidance where affected. Verify documentation links and ensure existing tile/media access behavior is described accurately.

## 3. Integration verification

- [x] 3.1 Build GameAtlas.slnx and run focused backend suites for new public operations and affected existing game/map/category/marker and OpenAPI behavior; verify existing management reads still include drafts and retain their response schemas.
- [x] 3.2 Format changed supported files, run node scripts/check-docs.mjs, git diff --check and openspec validate add-public-games-and-maps-api --strict. Verify the implementation diff contains no frontend, generated client, unrelated user-change or add-web-home-catalog edits.
