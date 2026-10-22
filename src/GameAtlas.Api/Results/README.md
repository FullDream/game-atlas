# OutcomeResult

Namespace: `GameAtlas.Api.Results`.

`OutcomeResult` and `OutcomeResult<T>` adapt domain results to HTTP responses.
They implement both Minimal API `IResult` and MVC `ActionResult`.
`ResultProblemDetailsMapper` builds error responses for both execution paths.

## Minimal APIs

Register JSON, FluentValidation discovery, and OpenAPI through `AddApi()`:

```csharp
using GameAtlas.Api;
using GameAtlas.Api.Results;

builder.Services.AddApi();

var app = builder.Build();
var api = app.MapGroup("").WithOutcomeResults();

// Map feature endpoints on api or its nested groups.
```

`WithOutcomeResults()` adds response metadata for `OutcomeResult`, `OutcomeResult<T>`,
and their `Task<>` / `ValueTask<>` wrappers. Explicit endpoint response statuses are preserved.
Metadata conventions live in `GameAtlas.Api.OpenApi.Results`.

Both Minimal API and MVC conventions automatically document HTTP 500 as
`application/problem+json` with a `ProblemDetails` body. Do not repeat this metadata
on individual endpoints. Explicit response metadata still takes precedence.
This documents unexpected failures handled by the host's exception middleware;
it does not convert exceptions into domain results.

## MVC

MVC support is opt-in:

```csharp
builder.Services.AddControllers().AddOutcomeResults();

// After building the application:
app.MapControllers();
```

When MVC executors are registered, results use MVC formatters and JSON options.
Otherwise, they use Minimal API HTTP results and HTTP JSON options configured by `AddApi()`.
MVC JSON options must be configured separately when adding controllers.

## Tests

Run `dotnet test tests/GameAtlas.IntegrationTests/GameAtlas.IntegrationTests.csproj`.
