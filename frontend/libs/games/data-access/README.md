# games-data-access

Shared backend access for admin and web: `type:data-access`, `scope:games`, `platform:shared`. Import from `@game-atlas/games/data-access`.

- `GamesService`: generated list/create/update/delete transport.
- Generated type aliases: `GameDto`, `ListGamesParams`, `UpdateGameRequest`, paginated responses and ProblemDetails contracts.
- `GamesQueries`: handwritten list `queryOptions`, reusable by components or QueryClient.
- `provideGamesBaseUrl`: application-owned runtime API addressing.

## Generation

Start the backend, then run from `frontend`:

```sh
npm exec nx -- run games-data-access:generate-api
```

Orval reads `http://localhost:5041/openapi/v1.json`, selects the `Games` tag and writes Angular HttpClient services plus referenced schemas to `src/lib/generated`. Other resource services are not generated. Override `GAME_ATLAS_OPENAPI_URL` when the development document is elsewhere; this is a generation input, not a runtime API origin.

Generation is deliberately uncached because the HTTP document can change without a local source-file change. The post-generation hook formats output with the repository Prettier configuration. Generated sources belong in version control; normal builds and tests do not fetch OpenAPI. An unchanged document and pinned tooling must regenerate without drift. Fetch/parse failures are errors, not a fallback to old catalog data. Orval cleans its generated directory before fetching; after a failed generation, rerun with an available document or restore the tracked generated files before building.

Generated filenames use kebab-case through output-level `namingConvention` (for example, `game-dto.ts`); TypeScript names and wire property names keep their original casing.

Do not edit generated files. Update `orval.config.ts` and regenerate. Generated schemas prefer `type` aliases and enum unions; add runtime `as const` values outside generated code only when needed. Orval transport helper declarations follow the generator's own format. Only generated transport is exempted from incompatible handwritten private-field, padding and inferred-type style rules; module boundaries remain enforced.

Future domain libraries follow the same pattern: maps selects `Maps`, `Category groups`, `Categories`, `Markers`; media selects `Media`. Do not scaffold them until a consumer exists.

## Runtime and query options

Applications provide Angular HTTP, TanStack Query and `provideGamesBaseUrl('/api')` (or another base URL). Admin development routes `/api` to its backend. Web can reuse the same library with its own providers; the library imports no application code.

`GamesQueries.list(params)` normalizes backend pagination/search defaults, includes request parameters in its cache key and preserves unspecified sorting. Sort direction defaults to `asc` only when sortBy is supplied. Invalid direction-without-field input is not silently discarded. Generated Observables are converted to Promises at the query boundary; cancellation unsubscribes the request, and HttpErrorResponse/ProblemDetails remain failures.

No mutations or invalidation policy are wrapped yet. There is no game-by-ID operation. UUID-v7 server identifiers remain strings, timestamps remain API strings, and PATCH omission is preserved rather than filled with nulls. The static GamesCatalog model/parser/JSON loader has been removed.

```sh
npm exec nx -- test games-data-access
npm exec nx -- lint games-data-access
```

The non-buildable library's test compilation uses the admin development build, while runtime source remains independent of admin. Tests use Angular's HTTP testing backend, not persistent backend data.
