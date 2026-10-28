# Tasks

## 1. Public OpenAPI schemas

- [x] 1.1 Implement DTO-free schema naming with generic arguments and preserved Optional inlining; update naming contract assertions and verify focused OpenAPI tests.
- [x] 1.2 Normalize canonical media components, references, mappings, and single-variant wrappers without changing serialization; verify media contracts, all local schema references, conflicting-shape rejection, and existing serialization tests.
- [x] 1.3 Document schema naming and media representation rules in API conventions and verify documentation links.

## 2. Frontend migration

- [x] 2.1 Regenerate the existing Games and Media clients through their HTTP-based Nx targets; verify canonical models and reproducible generation.
- [x] 2.2 Migrate handwritten consumers, public exports, and type annotations without DTO aliases; update data-access documentation and verify affected frontend lint and admin/web production builds.

## 3. Integration verification

- [x] 3.1 Verify the backend build and affected OpenAPI/serialization coverage, validate the OpenSpec change, run the documentation checker and git diff --check, and review the final diff for unchanged runtime JSON and HTTP generation behavior.
