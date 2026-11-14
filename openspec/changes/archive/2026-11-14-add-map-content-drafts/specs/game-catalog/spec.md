# Spec Delta

## MODIFIED Requirements

### Requirement: Return lightweight map summaries

Every game response SHALL include read-only maps ordered by map createdAt then id ascending. Entries SHALL contain only id, name, slug, status and preview. status SHALL be the map's persisted publication status (`draft` or `published`). preview SHALL be canonical ImageAsset from the current default style source, or null when no ready style exists. Empty games SHALL return maps=[]. Complete map endpoints and default-style rules SHALL remain unchanged. PATCH maps or counts SHALL return 400.

#### Scenario: Resolve the current default source

- **WHEN** a map has unfinished and ready styles
- **THEN** preview uses the earliest-created surviving ready style source
- **AND** subsequent reads reflect default changes and return null when no ready style remains

#### Scenario: Preserve blank maps and isolation

- **WHEN** a game has blank maps and another game has styled maps
- **THEN** only owned maps are returned, including null previews, without geometry or styles

#### Scenario: Return map slugs consistently

- **WHEN** a game is returned by listing, retrieval, creation or update
- **THEN** every included map summary contains its current required string slug and string status (`draft` or `published`)
- **AND** a map slug or status update is visible on subsequent game reads without changing the owning game's timestamps
