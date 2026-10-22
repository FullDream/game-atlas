# Proposal

## Why

Games can be created but clients cannot browse the catalog. A paginated list with name search and optional sorting enables catalog screens without loading all games into the client.

## What Changes

- Add unauthenticated GET `/games` returning all statuses, including Draft, with offset/limit pagination.
- Introduce the shared `PaginatedResult<T>(IReadOnlyCollection<T> Items, int Limit, int Offset, int Total)` contract and reuse the complete GameDto for both creation and listing.
- Add trimmed name substring search, case-insensitive for ASCII on SQLite.
- Support optional name, createdAt, and updatedAt sorting in either direction; apply no explicit ordering when sorting is omitted.
- Validate pagination and sorting through the existing Result-based validation pipeline and return 422 ProblemDetails for invalid values.
- Out of scope: authentication, status filters, single-game retrieval, Unicode case-insensitive search, normalized search columns, custom SQLite search functions, PostgreSQL, cursor pagination, and mandatory default ordering.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `game-catalog`: Add browsing, pagination, name search, optional sorting, and list-query validation requirements. Existing creation requirements remain unchanged.

## Impact

Application gains the shared pagination contract and ListGames slice; GameDto and its separate mapping extensions move from the CreateGame folder to Games. Api adds GET to the existing resource Endpoints file and query-to-command mapping to a separate mapping file. Infrastructure needs an orderable timestamp mapping because SQLite does not support direct DateTimeOffset ordering. Tests and API examples cover the new query contract and preserve Create Game compatibility. No search schema or external package is introduced.
