# Development workflow

Commands specify their working directory. Read the relevant configuration and project README before changing environment or deployment behavior.

## Git workflow

- When moving tracked files, use `git mv` where practical. If a move was already performed through another tool, stage both the old and new paths together with `git add -A -- <old-path> <new-path>`.
- Verify moves with `git diff --cached --name-status --find-renames`. Do not leave the new file staged while its old path's deletion remains unstaged. Git detects renames from content similarity rather than storing a separate move operation.

## Backend

Run the API from the repository root with `dotnet run --project src/GameAtlas.Api --launch-profile http`. The HTTP launch profile uses `http://localhost:5041`. Build the solution with `dotnet build GameAtlas.slnx`. Apply EF migrations explicitly; see [backend persistence](backend.md#persistence).

## Frontend

Run frontend commands from `frontend/` using npm. Use a Node.js version supported by the installed Angular build tools. See the [frontend README](../frontend/README.md) and [admin setup and hosting](../frontend/apps/admin/README.md).

- Test work is deferred until the user explicitly requests it for a task. Do not add new tests or run unit/E2E suites in the meantime. Use formatting, lint and production builds for verification. Preserve existing test coverage; resume test work only when requested.
- Choose checks for the changed files and affected projects. Run Prettier on supported handwritten files and ESLint through the affected projects' Nx targets when their linted sources or lint configuration change. Keep generated Nx agent assets in their installed format.

## Verification scope

Start with the smallest relevant checks. Broaden them when shared dependencies, configuration changes or failures justify it.

| Change                                                         | Verification                                                                                                                                                                                 |
| -------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Markdown documentation or agent instructions                   | Format handwritten Markdown, run `node scripts/check-docs.mjs` from the repository root, and run `git diff --check`. No application build or source lint is needed solely for prose changes. |
| Nx agent configuration or installed skills                     | Check referenced files, structured configuration and local skill references. Preserve the managed Nx block and generated assets.                                                             |
| Frontend TypeScript or templates                               | Format supported changed files and run lint for the affected projects. Build the consuming application when compilation, templates, providers or imports may be affected.                    |
| Frontend dependencies, build configuration or shared libraries | Run the relevant lint and consuming application builds; widen the project selection according to dependencies.                                                                               |
| Backend code                                                   | Build the affected solution/projects and run focused tests appropriate to changed behavior. The frontend test deferral applies only to frontend tests.                                       |

Report the checks actually run and their limitations. Formatting, lint and compilation do not establish runtime behavior. Do not regenerate API clients or fetch OpenAPI merely to verify an unrelated change.

## Nx agent tooling

Run `npm exec nx -- configure-ai-agents --agents codex` from `frontend/` to configure or refresh Nx agent integration for Codex. Review the resulting diff, including changes to the managed block in `frontend/AGENTS.md`.

Codex is the currently configured AI client. Nx skills live in the [frontend skills directory](../frontend/.agents/skills/), with [Codex configuration](../frontend/.codex/config.toml) and its [CI helper](../frontend/.codex/agents/ci-monitor-subagent.toml). These paths are relative to `frontend/`. Keep this setup scoped to Codex unless another client is explicitly requested.

Installation does not guarantee a tool is available in an already running session. Use available Nx MCP tools, or the installed CLI when MCP is unavailable.

Keep project conventions in `docs/` and the handwritten part of the agent entry points. Refresh generated Nx skills and agent configuration through Nx rather than editing their behavior or formatting independently. Retain the separately installed Angular, Spartan and UI/UX skills and their lockfile entries.

## Frontend verification commands

Use actual project names and configured targets discovered with `npm exec nx -- show project <project> --json`. Replace angle-bracket placeholders before running commands. Run these commands from `frontend/`:

```sh
npm exec nx -- run <project>:lint
npm exec nx -- build <application> --configuration=production
```

For tasks where frontend test work has been explicitly requested:

```sh
npm exec nx -- test <project>
```

For deliberate API regeneration with a running backend:

```sh
npm exec nx -- run games-data-access:generate-api
```

Formatting, lint and production builds do not establish interactive behavior or unit-test verification.

Keep Nx caching for normal build/lint/test tasks. HTTP generation is explicitly uncached; do not add it as a prerequisite of build. Root ESLint and relevant project configurations remain task inputs. Prettier and ESLint follow frontend conventions; any narrow generated-source exceptions leave handwritten checks and domain/platform boundaries intact.

## Documentation maintenance

Keep each convention in its owning document and link to it from the agent entry points. Update affected documentation in the same change as behavior or architecture changes. Distinguish implemented behavior from planned work, and preserve existing user constraints when moving instructions. Run the documentation checker below and `git diff --check` after documentation changes. Existing formatter/linter configuration owns automated style checks; frontend dependency directions are enforced by `@nx/enforce-module-boundaries` in `frontend/eslint.config.mjs`. Prose conventions still require review.

### Documentation checker

With Node.js 22 or newer and Git available, install the documentation tooling from the repository root:

```sh
npm ci --prefix scripts
```

Then run the checker:

```sh
node scripts/check-docs.mjs
```

The [checker](../scripts/check-docs.mjs) uses [mdast-util-from-markdown](https://github.com/syntax-tree/mdast-util-from-markdown) to parse Markdown, [mdast-util-to-string](https://github.com/syntax-tree/mdast-util-to-string) to extract heading text, and [github-slugger](https://github.com/Flet/github-slugger) to generate GitHub-style anchors. Dependencies are pinned in the separate [tooling package](../scripts/package.json) and [lockfile](../scripts/package-lock.json), independent of frontend packages. Checking needs no application server or network access after installation. The script locates the repository relative to itself, so invoking it by its path also works from another working directory.

It checks tracked and untracked non-ignored Markdown files, including agent entry points, `docs/`, current OpenSpec artifacts and project READMEs. It skips deleted files, archived OpenSpec changes, imported skills, `.codex/` Markdown and dependency/build output. Links from project documents into imported skills are still checked.

Supported checks include relative and repository-root paths, local images, percent-encoded paths, inline and resolved reference-style links, Markdown heading anchors (including duplicate suffixes) and explicit HTML anchors. Missing files or anchors report the source file and line and exit with code 1; success exits with code 0. The CommonMark parser handles code blocks, inline code, comments, escapes and multiline reference definitions. Unresolved reference-style text remains ordinary Markdown text. External URLs are not fetched. Non-Markdown fragments and arbitrary paths written as prose or code are not validated; use Markdown links for files that must be checked.

Codex `config_file` references in `.codex/config.toml` files are checked relative to their configuration directory. This checks referenced file existence, not full TOML syntax or tool availability. Keep skill and configuration entry points linked from the agent instructions or development documentation so missing files are detected.

The checker's own regression tests use temporary fixtures and can be run independently:

```sh
node --test scripts/check-docs.test.mjs
```
