# GameAtlas architecture

GameAtlas is a platform for interactive video game maps. Paths in this document are relative to the repository root.

## Backend layers

- Start with a layered monolith using the Api, Application, Domain, and Infrastructure projects. Group functionality by feature within these projects. Consider separate module projects when feature areas become large enough to justify them.
- Keep domain rules in Domain and use-case orchestration in Application. Keep HTTP concerns in Api and persistence implementations and EF Core configurations in Infrastructure.

| Project                        | Responsibility                                                 | Project dependencies                |
| ------------------------------ | -------------------------------------------------------------- | ----------------------------------- |
| `src/GameAtlas.Domain`         | Domain entities, rules and domain errors                       | None                                |
| `src/GameAtlas.Application`    | Use cases, validation and persistence contracts                | Domain                              |
| `src/GameAtlas.Infrastructure` | EF Core, SQLite, migrations and infrastructure implementations | Application                         |
| `src/GameAtlas.Api`            | HTTP contracts, endpoints and composition                      | Application, Infrastructure, Domain |

Keep dependencies directed inward. Domain must not depend on API DTOs or infrastructure. See [backend conventions](backend.md) for feature organization, mapping, validation and persistence, and [API conventions](api-conventions.md) for HTTP contracts.

## Frontend and domains

The Angular/Nx workspace in `frontend/` contains an admin browser SPA and a separate statically prerendered public web application. Shared libraries use explicit type, domain and platform boundaries. Games and maps may consume media; shared libraries remain independent of product domains. Category groups, categories and markers belong to maps.

See [frontend architecture](frontend/architecture.md) for application structure and the dependency matrix. ESLint enforces frontend import boundaries; backend project references expose layer dependencies but do not by themselves check every architectural convention.

## Documentation and requirements

Engineering conventions live in `docs/`; [AGENTS.md](../AGENTS.md) and [frontend/AGENTS.md](../frontend/AGENTS.md) select the documents to read for a task. Existing product requirements and change artifacts remain in `openspec/specs/` and `openspec/changes/`. Documentation and agent-instruction maintenance can be performed directly without creating an OpenSpec change.
