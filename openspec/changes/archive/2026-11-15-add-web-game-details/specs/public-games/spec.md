# Spec Delta

## MODIFIED Requirements

### Requirement: Resolve published game details by slug

Unauthenticated GET /public/games/{gameSlug} SHALL return id, name, slug, cover, nullable banner, releaseDate, developer, description, mapsCount, markersCount, categoriesCount and maps. Maps SHALL be summaries with id, name, slug, markersCount, categoriesCount and nullable canonical ImageAsset preview, ordered by createdAt then id ascending. Missing, deleted or draft games SHALL return 404 ProblemDetails. Reads SHALL NOT change stored data or timestamps.

#### Scenario: Published game with mixed maps

- **WHEN** a published game has draft and published maps
- **THEN** only published maps appear and mapsCount equals the returned summary count
- **AND** previews use the selected ready default style source, or null when none exists

#### Scenario: Empty published game

- **WHEN** the game exists and is published but has no published maps
- **THEN** details return 200 with maps=[] and zero content counts

#### Scenario: Count content per map

- **WHEN** summaries include maps with mixed publication states in their groups, categories and markers
- **THEN** each summary counts only published categories under published groups and published markers under those categories
- **AND** empty maps return zero counts and game totals equal the sums of the map counts
