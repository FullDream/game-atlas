# Frontend agent instructions

This Angular/Nx workspace contains the admin SPA, public web application and shared libraries. Paths in linked frontend documents are relative to this directory unless stated otherwise.

## Read the applicable guidance

Before editing frontend code, read [frontend architecture](../docs/frontend/architecture.md) and [TypeScript and template style](../docs/styleguide.md#typescript-and-angular-templates), then select the additional guidance for the task:

| Task                                                          | Required guidance                                                                                |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Angular components, services, signals, forms or routing       | [Angular conventions](../docs/frontend/angular.md)                                               |
| API generation, queries, mutations or their feature consumers | [Data access](../docs/frontend/data-access.md) and [API conventions](../docs/api-conventions.md) |
| UI components, themes, icons or interaction behavior          | [UI guidelines](../docs/frontend/ui.md) and [Angular conventions](../docs/frontend/angular.md)   |
| Running, verifying, deployment or moving files                | [Development workflow](../docs/development.md)                                                   |

## Current verification policy

Frontend unit/E2E test work remains deferred until explicitly requested for the task. Follow the [current verification policy and commands](../docs/development.md#frontend), preserving existing coverage.

## Installed Nx guidance

Nx skills are installed under `.agents/skills/`. If a skill named below is not listed in the current session, read its local `SKILL.md` directly:

- [nx-workspace](.agents/skills/nx-workspace/SKILL.md) for workspace discovery and resolved project configuration.
- [nx-generate](.agents/skills/nx-generate/SKILL.md) before scaffolding applications, libraries or project structure.
- [nx-run-tasks](.agents/skills/nx-run-tasks/SKILL.md) for running configured targets.

Use Nx MCP tools when available in the current session; otherwise use the installed Nx CLI through `npm exec nx --` from `frontend/`. Check CLI help for unfamiliar flags. The configured tools and installed skills do not override the project verification policy above.

The block below is maintained by `nx configure-ai-agents`. Keep project-specific guidance outside its markers. See [Nx agent tooling maintenance](../docs/development.md#nx-agent-tooling) for the generated configuration and skill locations.

<!-- nx configuration start-->
<!-- Leave the start & end comments to automatically receive updates. -->

## General Guidelines for working with Nx

- For navigating/exploring the workspace, invoke the `nx-workspace` skill first - it has patterns for querying projects, targets, and dependencies
- When running tasks (for example build, lint, test, e2e, etc.), always prefer running the task through `nx` (i.e. `nx run`, `nx run-many`, `nx affected`) instead of using the underlying tooling directly
- Prefix nx commands with the workspace's package manager (e.g., `pnpm nx build`, `npm exec nx test`) - avoids using globally installed CLI
- You have access to the Nx MCP server and its tools, use them to help the user
- For Nx plugin best practices, check `node_modules/@nx/<plugin>/PLUGIN.md`. Not all plugins have this file - proceed without it if unavailable.
- NEVER guess CLI flags - always check nx_docs or `--help` first when unsure

## Scaffolding & Generators

- For scaffolding tasks (creating apps, libs, project structure, setup), ALWAYS invoke the `nx-generate` skill FIRST before exploring or calling MCP tools

## When to use nx_docs

- USE for: advanced config options, unfamiliar flags, migration guides, plugin configuration, edge cases
- DON'T USE for: basic generator syntax (`nx g @nx/react:app`), standard commands, things you already know
- The `nx-generate` skill handles generator discovery internally - don't call nx_docs just to look up generator syntax

<!-- nx configuration end-->
