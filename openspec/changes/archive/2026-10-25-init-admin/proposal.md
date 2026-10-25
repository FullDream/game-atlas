# Proposal

## Why

The backend now exposes the content-management API, but the frontend only contains an earlier static catalog prototype. Establish a separate administration application and a reusable generated API boundary before implementing resource-management screens.

## What Changes

- Use Material Symbols Rounded consistently, including owned Helm components. E2E tests are deferred; validate with unit tests, lint, production builds and live integration checks.
- Add an Angular `admin` SPA in the existing Nx workspace, with a Spartan UI sidebar, compact header, breadcrumbs, responsive navigation and routed content area.
- Add a Light / Default (system) / Dark header theme control, persisted with ngxtension `injectLocalStorage` and applied through a head color-scheme meta tag and CSS `light-dark()` tokens.
- Start with Games and Media navigation destinations and explicit foundation-only page content. Do not implement resource CRUD or pretend to show live data.
- Configure Angular HTTP and TanStack Query for the administration application; keep query options in handwritten injectable classes rather than generating TanStack integration.
- **BREAKING**: Replace the existing `games/data-access` static catalog models, parser and loader with shared backend API access. Retain its library identity/import alias and use `type:data-access`, `scope:games`, `platform:shared`.
- Generate Angular HTTP services and model types with Orval from the running backend at `http://localhost:5041/openapi/v1.json`. Use tag filtering, tag-split output, a library-local `generated/` directory and a `generate-api` Nx target.
- Generate union types for enum schemas and prefer type aliases over interfaces. Introduce runtime `as const` values only for an actual consumer requirement.
- Remove obsolete static-catalog dependencies, tests and assets as needed to keep the workspace coherent. Public-web preservation or redesign is not an acceptance requirement, but do not leave broken imports or misleading documentation.
- Record lasting frontend conventions in frontend documentation/AGENTS and add frontend context to the root OpenSpec configuration during implementation.

## Capabilities

### New Capabilities

- `admin-shell`: A separately runnable administration SPA with accessible responsive navigation, routed foundation pages and shared UI conventions.
- `frontend-api-clients`: Regenerable per-domain API clients, shared game contracts and handwritten query-options integration for frontend consumers.

### Modified Capabilities

None. Existing backend API and persistence contracts remain unchanged.

## Impact

- Frontend application/project configuration, routing, styling, shared Spartan components, the existing `libs/games/data-access`, its dependent prototype code and related tests/docs.
- Dependencies: Orval and TanStack Query Angular; reuse the installed Angular/Nx/Tailwind/Spartan stack and generate only the needed Helm components.
- The already prepared type/scope/platform boundary changes are the architectural baseline; preserve domain grouping of category groups, categories and markers under maps.
- Generation needs a running backend; ordinary builds use committed generated sources. Backend data, migrations and endpoints are not changed.
- Out of scope: game CRUD screens, working media browser, map viewer/editor, authentication, user/role management, dashboard metrics, publication/static export, API generation for unused domains, and redesigning the public website.
