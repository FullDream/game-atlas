# Tasks

## 1. Domain and persistence

- [x] 1.1 Replace the game image string with required ImageAssetId and an ImageAsset navigation, update domain creation/update rules and errors, and remove obsolete image string limits. Adapt game domain tests and backend fixture builders to IDs and persisted image assets; verify focused domain tests cover required IDs, replacement, and no-op timestamps, and the affected projects compile.
- [x] 1.2 Configure the required image FK, restrictive deletion and index; add a new guarded EF migration and snapshot update without modifying prior migrations. Add migration/persistence tests for the full fresh-database chain, reference round trips, FK protection, safe rejection of populated legacy games, and guarded rollback; run those focused tests. Document the empty-database support boundary and explicit migration procedure in the Games API README.

## 2. Game operations and shared responses

- [x] 2.1 Change create/update requests, commands, validation and API mappings to imageAssetId, retaining Optional presence semantics for PATCH and existing name/slug/status behavior. Adapt validation and HTTP tests for missing/null/empty UUID 422, malformed UUID 400, missing asset 404, folder/video 422, and legacy image input behavior; verify focused tests pass.
- [x] 2.2 Resolve image references and persist create/update within the existing attachment transaction pattern. Change GameDto.Image to ImageAssetDto and map through shared media mapping. Add focused handler/API tests for complete images on create, replacement, omitted image, same-image no-op, atomic failure and reopened persistence; verify existing game ownership and slug behavior still passes.
- [x] 2.3 Adapt list loading and game query/entity mapping to resolve images with bounded page reads and no per-game asset query. Extend list integration tests using SQL capture for several games with distinct/reused covers; verify pagination, total, search, sorting, empty pages, complete image objects, shared metadata updates without game timestamp changes, and missing physical originals.
- [x] 2.4 Update endpoint metadata, Games README examples and OpenAPI assertions for imageAssetId inputs, canonical required ImageAsset output, seven root response fields, both PATCH media types, and ProblemDetails. Verify focused OpenAPI tests confirm one canonical image schema and exactly one image type discriminator in HTTP payloads; run the documentation checker for the edited docs.

## 3. Media reference lifecycle

- [x] 3.1 Include game cover references in the existing media deletion guard before physical removal. Extend media reference/deletion tests for shared covers, missing originals, create/replace versus delete races, cover replacement and game deletion without asset deletion; run focused lifecycle and race tests and verify deletion is 409 while any reference remains and 204 only after all uses are removed.
- [x] 3.2 Update media API documentation to list game covers among protected asset uses and explain that replacement/deletion releases references without deleting assets; verify examples agree with lifecycle tests and the documentation checker passes.

## 4. Frontend contract integration

- [x] 4.1 Serve the updated API against a migrated disposable database and run the existing games-data-access:generate-api Nx target over HTTP. Verify committed output has Game.image as ImageAsset and create/update inputs as imageAssetId, no handwritten edits in generated files, and no unrelated Media service generation.
- [x] 4.2 Adapt affected handwritten exports, consumers and existing contract fixture values to the generated image object/ID shapes, preserving existing test coverage without adding or running frontend unit/E2E tests. Format edited files, run affected Nx lint targets and consuming production builds, and verify ordinary builds do not fetch OpenAPI.

## 5. Integrated verification

- [x] 5.1 Build GameAtlas.slnx and run the relevant backend game/media suites after fixture migration, including migration, HTTP, OpenAPI, lifecycle, and concurrency coverage. Verify the complete upload-image/create-game/list/replace/delete flow and confirm no unrelated map or catalog behavior regresses.
- [x] 5.2 Validate this OpenSpec change, format affected handwritten Markdown, run node scripts/check-docs.mjs and git diff --check, and record actual verification results and any limitations before handoff.
