# GameAtlas frontend

Angular/Nx workspace for a separate administration SPA and public website. Run commands from this directory with npm and a Node.js version supported by the installed Angular toolchain.

```sh
npm ci
npm exec nx -- serve admin
npm exec nx -- build admin
npm exec nx -- run games-data-access:generate-api
```

Admin development runs on port 4201; `/api/**` and `/media/files/**` are proxied to the backend on port 5041. API generation needs the running backend, but normal builds use checked-in generated files and do not fetch OpenAPI.

Admin provides a Media library for folder browsing, file uploads, metadata editing, cover selection, moving items and confirmed deletion. Media is lazy loaded from `@game-atlas/media/feature-list`; API transport and query/mutation options live in `@game-atlas/media/data-access`. Games provides a searchable, sortable list and dedicated creation/editing pages, with library covers, publication controls and confirmed deletion. Authentication is not implemented yet.

- [Administration setup and hosting](apps/admin/README.md)
- [Shared Games API and generation](libs/games/data-access/README.md)
- [Media library](libs/media/feature-list/README.md)
- [Shared Media API and cache behavior](libs/media/data-access/README.md)
- [Architecture and dependency boundaries](../docs/frontend/architecture.md)
- [Frontend conventions](AGENTS.md)
- [Nx agent tooling and maintenance](../docs/development.md#nx-agent-tooling)

The public `web` application is currently a minimal, statically prerendered entry. Its old catalog prototype was removed when Games data access switched to the backend contract. Public features will be rebuilt separately.

```sh
npm exec nx -- run-many -t lint
npm exec nx -- run-many -t build --projects=admin,web
```

Frontend unit/E2E test work remains deferred until explicitly requested for the task. See the [verification policy](../docs/development.md#frontend).

Inspect a project's available targets with `npm exec nx -- show project <name>`. Use the installed Nx generators for new applications/libraries and create projects only when a real feature needs them.
