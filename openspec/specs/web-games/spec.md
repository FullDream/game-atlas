# web-games Specification

## Purpose

Provide the public home page game catalog (`libs/games/feature-catalog`) backed by `GamesQueryOptions` and Spartan Helm primitives so visitors can browse, search, sort, and paginate games.

## Requirements

### Requirement: Render hero banner with static key art and debounced search

The home page SHALL render a hero banner with a static `.webp` background and blur placeholder, theme-adaptive `bg-background` fade, heading, subtitle, two feature callouts (`Unlimited markers`, `Multiple maps`), and a pill search input (`HlmInputGroup`) that debounces input before updating the `search` query param and resetting `offset` to `0`.

#### Scenario: Search games from the hero input

- **WHEN** a visitor types `"elden"` into the hero search input
- **THEN** after the debounce delay, the `search` URL query parameter updates to `elden` and `offset` resets to `0`
- **AND** `public game list query options` queries `GET /public/games` with `search=elden`

#### Scenario: Render hero without multilingual callout

- **WHEN** the home page renders
- **THEN** the hero displays only the `Unlimited markers` and `Multiple maps` callouts and omits any `Multilingual` feature badge

### Requirement: Browse, sort, and paginate the public games grid

The catalog section SHALL render a section header, all 6 API sort options (`HlmSelect`), a responsive 1/2/3/4-column card grid, and `hlm-numbered-pagination-query-params` with selectable page sizes `[24, 48, 72, 96]` (default `24`). Query state (`search`, `offset`, `limit`, `sortBy`, `sortDirection`) SHALL sync with URL query params via `linkedQueryParam`.

#### Scenario: Load initial SSR catalog from URL query parameters

- **WHEN** a visitor requests `/?sortBy=createdAt&sortDirection=desc&offset=24&limit=24`
- **THEN** the SSR response renders the second page sorted newest-first using the exact query parameters from the URL

#### Scenario: Change sort or page size

- **WHEN** a visitor selects a different sort option (`Name A–Z`, `Name Z–A`, `Newest first`, `Oldest first`, `Recently updated`, `Least recently updated`) or a page size from `[24, 48, 72, 96]`
- **THEN** `offset` resets to `0` and the grid updates from `public game list query options`

### Requirement: Render public game cards without status badges

Each game card SHALL use `@spartan-ng/helm/card` and display `GET /public/games` items containing published games only, without client status filtering or draft/published badges. It SHALL render `banner` (`ui-media-thumbnail`) or a styled icon fallback when `banner` is `null`, plus `cover`, game name, `<mapsCount> map(s) · <markersCount> markers`, and a link to `/games/:slug`.

#### Scenario: Render game with banner and without status badge

- **WHEN** a `PublicGameCard` returned by `GET /public/games` has a non-null `banner`
- **THEN** the card renders the `banner`, `cover`, title, map/marker counts, and link to `['/games', game.slug]`
- **AND** no draft or published status badge is displayed on the card

#### Scenario: Render styled fallback when banner is null

- **WHEN** a `PublicGameCard` returned by `GET /public/games` has `banner: null`
- **THEN** the card header media renders a styled placeholder surface with a Material Symbols Rounded icon rather than a broken image

### Requirement: Handle loading, empty, and error states cleanly

While initial catalog data is pending on the client, the grid SHALL render card skeletons (`hlm-skeleton`) matching the active `limit`. When `GET /public/games` fails, the catalog SHALL display `ui-error-state` with a retry action that re-fetches without discarding previously cached data. When zero items match, the catalog SHALL display `hlm-empty` with a clear-search action whenever `search` is active.

#### Scenario: Recover from empty search result

- **WHEN** an active `search` filter returns zero games
- **THEN** `hlm-empty` informs the visitor and offers a button that clears `search` and resets `offset` to `0`

### Requirement: Render published game details

The game detail page SHALL display breadcrumbs back to the catalog, game name, cover, banner, optional description, map/marker counts and a map section following the supplied visual reference. The layout SHALL adapt to narrow screens and both themes using shared visual conventions.

#### Scenario: Open a published game

- **WHEN** a visitor opens `/games/elden-ring`
- **THEN** the page displays the public game name rather than the slug as its heading
- **AND** game images and available metadata come from the public detail response

#### Scenario: Optional content is absent

- **WHEN** banner or description is null
- **THEN** the page uses a styled banner fallback and omits the absent description without showing an endless skeleton

### Requirement: Render map summary cards

The detail page SHALL display published maps in API order in a responsive grid, with two columns on wide screens. Each card SHALL show the map name, preview or styled fallback, marker count and category count including zeros. Counts SHALL use readable singular/plural labels. Map previews, names and arrow actions SHALL link to `/games/:slug/maps/:mapSlug`, reserved without map components until the viewer is implemented.

#### Scenario: Map summaries contain counts

- **WHEN** a game returns maps with different counts and a missing preview
- **THEN** each card displays its own marker/category counts and the missing preview uses a styled fallback

#### Scenario: No maps are published

- **WHEN** the detail response contains an empty maps array
- **THEN** the section shows an explicit empty state

### Requirement: Render partial detail loading accurately

The detail page SHALL show available cached catalog fields immediately and skeletons only for unavailable detail fields during the first detail fetch. A direct client load without cached data SHALL show a full page skeleton. Completed null/empty fields SHALL not be treated as loading. Refetching complete details SHALL retain their content.

#### Scenario: Navigate from a cached catalog

- **WHEN** the detail request uses a matching catalog card as placeholder
- **THEN** name, images and counts are visible immediately while description and map content show skeletons
- **AND** skeleton map count follows the known mapsCount without fabricating map summaries

#### Scenario: Detail request fails or game is unavailable

- **WHEN** the detail request returns 404 or another error
- **THEN** 404 displays a game-unavailable state with a catalog link and other errors display a retry action
- **AND** placeholder content does not conceal the failure
