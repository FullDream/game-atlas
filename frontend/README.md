# GameAtlas frontend

Angular/Nx workspace for a separate administration SPA and public website. Run commands from this directory with npm and a Node.js version supported by the installed Angular toolchain.

```sh
npm ci
npm exec nx -- serve admin
npm exec nx -- build admin
npm exec nx -- run games-data-access:generate-api
```

Admin development runs on port 4201; its `/api` proxy targets the backend on port 5041. API generation needs the running backend, but normal builds use checked-in generated files and do not fetch OpenAPI. The initial admin contains foundation pages rather than resource CRUD.

- [Administration setup and hosting](apps/admin/README.md)
- [Shared Games API and generation](libs/games/data-access/README.md)
- [Architecture and dependency boundaries](docs/frontend-architecture.md)
- [Frontend conventions](AGENTS.md)

The public `web` application is currently a minimal, statically prerendered entry. Its old catalog prototype was removed when Games data access switched to the backend contract. Public features will be rebuilt separately.

```sh
npm exec nx -- run-many -t lint
npm exec nx -- run-many -t test --projects=admin,games-data-access,web
```

Inspect a project's available targets with `npm exec nx -- show project <name>`. Use the installed Nx generators for new applications/libraries and create projects only when a real feature needs them.
