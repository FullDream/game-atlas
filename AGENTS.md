# GameAtlas agent instructions

GameAtlas is a platform for interactive video game maps, with a layered .NET backend and an Angular/Nx frontend.

## Read the applicable guidance

Before editing, read the documents for the affected area, including any applicable nested `AGENTS.md`. Linked documents contain project conventions and must be followed within their stated scope. Paths below are relative to the repository root. Load relevant documents as needed rather than reading the entire documentation tree for every task.

| Task                                                     | Required guidance                                                                                           |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Project structure, architecture or dependencies          | [Architecture](docs/architecture.md)                                                                        |
| Backend code, including backend tests                    | [Backend conventions](docs/backend.md) and [C# style](docs/styleguide.md#c)                                 |
| HTTP endpoints, contracts, API clients or contract tests | [API conventions](docs/api-conventions.md)                                                                  |
| Frontend changes                                         | [Frontend agent instructions](frontend/AGENTS.md)                                                           |
| Running, verifying or moving files                       | [Development workflow](docs/development.md)                                                                 |
| Documentation or instruction changes                     | [Documentation maintenance](docs/development.md#documentation-maintenance) and the affected topic documents |

## Working agreements

- Follow the applicable `.editorconfig` and formatter/linter configuration as the source of truth for automated style rules.
- Keep conventions in their owning documents; update links and affected documentation when moving or changing them.
- Preserve existing user changes and task-specific constraints. In particular, read the current frontend verification policy before adding or running tests.
- Documentation and agent-instruction maintenance does not require an OpenSpec change. Use existing OpenSpec artifacts when relevant to the requested product work.
