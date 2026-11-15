# Spec Delta

## MODIFIED Requirements

### Requirement: Generate only the selected domain API

Each API data-access library SHALL expose a `generate-api` command that reads the running backend OpenAPI document over HTTP and selects operations by domain tag. Games generation SHALL use `http://localhost:5041/openapi/v1.json` and the exact Games and Public Games tags. Generated outputs SHALL be confined to that library and SHALL NOT contain unrelated resource services.

#### Scenario: Generate shared game clients

- **WHEN** Games generation runs against the available backend document
- **THEN** management and public game operations and their referenced contracts are generated beneath the library's `generated/` directory
- **AND** Media, Maps, Categories, Category groups and Markers services are absent

#### Scenario: Fail generation visibly

- **WHEN** the configured document cannot be fetched or parsed
- **THEN** generation exits unsuccessfully with an actionable diagnostic
- **AND** it does not silently substitute old static catalog data or rewrite handwritten query classes

## ADDED Requirements

### Requirement: Isolate public game reads

Public game listing and slug-based details SHALL use dedicated reusable query options and cache entries separate from management reads. List keys SHALL distinguish normalized pagination, search and sort parameters. Public options SHALL inherit application freshness defaults and preserve HTTP errors and cancellation.

#### Scenario: Read public and management lists

- **WHEN** both representations are cached
- **THEN** public reads use `/public/games` and cannot consume management entries
- **AND** equivalent public list parameters share an entry

### Requirement: Reuse public cards as temporary detail data

With no complete cached detail, a public detail read SHALL use the matching slug from the most recently updated successful cached public list as temporary placeholder data. Missing detail-only fields SHALL remain distinguishable from authoritative empty or null values. Placeholder use SHALL NOT prevent the complete detail request.

#### Scenario: Open a cached game

- **WHEN** cached lists contain the requested slug and there is no complete detail
- **THEN** available card fields appear immediately while the complete detail is fetched
- **AND** the partial card is not stored as authoritative detail

#### Scenario: Existing detail or another slug

- **WHEN** complete detail exists or the visitor switches to another slug
- **THEN** list placeholders do not overwrite complete detail or display another game's data

#### Scenario: Missing cache or failed retrieval

- **WHEN** no list contains the slug or detail retrieval fails
- **THEN** the request still runs and failures remain errors rather than a successful fabricated detail
