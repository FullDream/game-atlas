# Tasks

## 1. Application deletion slice

- [x] 1.1 Add Games/DeleteGame command returning Result, DeleteGameHandler using a primary-key-filtered ExecuteDeleteAsync with cancellation, and static DeleteGameErrors.NotFound; verify the Application project builds, zero affected rows return NotFound, and no existence query or SaveChangesAsync call is added.
- [x] 1.2 Add SQLite-backed handler tests in the integration test project for deletion of either status, zero affected rows, preservation of other games, and durable removal in a new context; verify the tests pass and command capture confirms deletion does not load an entity first.

## 2. HTTP endpoint and API contract

- [x] 2.1 Add DeleteGame to Games/Endpoints.cs with a Guid route argument, IMediator dispatch, and nongeneric OutcomeResult; add API tests for unauthenticated draft/published deletion returning an empty 204, missing and repeated deletion returning 404, and malformed GUID returning 400, and verify all tests pass.
- [x] 2.2 Add end-to-end tests that deletion removes the game from list items and totals, permits slug reuse through POST and PATCH, and makes a valid PATCH of the deleted identifier return 404; verify other games retain their metadata and all scenarios pass.
- [x] 2.3 Add a storage-failure API test that fails the DELETE statement before commit; verify safe 500 ProblemDetails, retained storage state, and no SQL or exception details in the response.
- [x] 2.4 Add OpenAPI coverage for DeleteGame, required uuid path id, absence of a required request body, empty 204 response metadata, and ProblemDetails for 400/404/500; verify the existing shared OutcomeResult convention supplies the contract without duplicating response declarations.
- [x] 2.5 Document physical deletion, repeat-delete 404, and slug reuse in Api/Games/README.md and add a DELETE example to GameAtlas.Api.http; verify the examples and documented statuses match the API tests.

## 3. Integration verification

- [x] 3.1 Run dotnet test GameAtlas.slnx -c Release and verify deletion tests and existing create/list/update tests pass without introducing a migration or a package dependency.
- [x] 3.2 Run openspec validate delete-game --strict and git diff --check; verify the artifacts pass validation and all changed files follow project formatting conventions.
