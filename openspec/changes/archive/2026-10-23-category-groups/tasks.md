# Tasks

## 1. Revise order semantics

- [x] 1.1 Update domain creation and atomic Update for int order > 0; remove ChangeOrder and verify domain tests cover null/zero/negative rejection, combined-update atomicity, and no-op dates.
- [x] 1.2 Add optional order to create/update commands and request DTOs with FluentValidation; verify omission, null, positive boundaries, fractional/overflow binding, and combined patches.

## 2. Remove reorder and adapt persistence flow

- [x] 2.1 Remove reorder endpoint, feature slice, request/mapping, registration, and obsolete tests; verify source and OpenAPI expose only four operations and no reorder contract.
- [x] 2.2 Use supplied order or transactional max + 1 from 1 at creation, handle exhausted int range with 422, update only the target, and list by order/createdAt/id; verify SQLite/API tests cover duplicates, stable ties, concurrent default appends, no-ops, and rollback.

## 3. Documentation and verification

- [x] 3.1 Update HTTP examples, resource documentation, OpenAPI and persistence tests for positive per-group order and unchanged other groups; verify name uniqueness, cascade deletion and transaction-abstraction tests still pass.
- [x] 3.2 Run dotnet test GameAtlas.slnx -c Release, formatting verification, git diff --check, and openspec validate category-groups --strict; verify all checks pass without changing the existing schema or exposing DatabaseFacade in Application.
