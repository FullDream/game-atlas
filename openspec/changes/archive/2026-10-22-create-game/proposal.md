# Proposal

## Why

GameAtlas has a backend foundation but cannot yet store games. Creating a draft game is the first step toward managing games and their interactive maps.

## What Changes

- Add unauthenticated `POST /games` accepting a client-supplied slug, name, image string, and optional map ID.
- Create games with a server-generated UUID v7, UTC timestamps, and Draft status, returning HTTP 200 with the complete created game.
- Validate normalized input and enforce slug uniqueness, returning ProblemDetails for failures.
- Add shared Result-based request validation and centralized exception handling needed by this operation.
- Persist games in SQLite with EF Core migrations maintained in Infrastructure and applied explicitly through EF CLI.
- Out of scope: publishing, editing, listing or reading games, authentication, map existence checks, image uploads, PostgreSQL, separate module projects, and repository abstractions.

## Capabilities

### New Capabilities

- `game-catalog`: Game identity, draft creation, input constraints, and persistence-backed slug uniqueness.

### Modified Capabilities

None.

## Impact

Touches the existing Domain, Application, Infrastructure, Api, and test projects. Extends IApplicationDbContext with game access; introduces the game entity, CreateGame handler and validation, API mapping, EF configuration, and an initial migration. Reuses Mediator, FluentValidation, Result, and OutcomeResult. Application will reference Domain and EF Core abstractions; API tests may require a host-testing package. Existing persistence tests that assume an empty EF model must change.
