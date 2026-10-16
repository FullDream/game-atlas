<!-- nx configuration start-->
<!-- Leave the start & end comments to automatically receive updates. -->

# General Guidelines for working with Nx

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

## Module boundaries

- Tag each project with a type (`type:app`, `type:feature`, `type:ui`, `type:data-access`, or `type:util`) and a scope (`scope:web`, `scope:editor`, or `scope:shared`). Do not use `type:shell`.
- The future `editor` application should use `type:app` and `scope:editor`. Its libraries use the matching type and `scope:editor`.
- Keep web and editor libraries isolated; put reusable libraries in `scope:shared`. Shared libraries must only depend on other shared libraries.
- Follow the dependency directions in `eslint.config.mjs`; scope and type constraints both apply.
- Prefer non-buildable internal libraries. See `docs/frontend-architecture.md` for the planned structure and tag rules; create libraries only when needed.

## Static web output

- The public web app uses `outputMode: "static"`. Keep prerendering and hydration; do not introduce a production Node.js server or depend on the editor API.
- Initialize browser-only packages with dynamic imports in browser render hooks such as `afterNextRender()`.
- Add future parameterized prerender routes only with their features and a published-manifest-backed `getPrerenderParams()`.

## UI and styling

- Use Tailwind CSS v4 for styling. PostCSS is configured in `.postcssrc.json`; global styles and the Spartan theme are in `apps/web/src/styles.css`.
- Spartan UI dependencies and theme are configured. `components.json` defines the destination and import alias for any future Helm components.
- Reuse existing project components and library APIs. Generate Helm components only when explicitly requested.
