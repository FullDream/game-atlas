# Spec Delta

## MODIFIED Requirements

### Requirement: Display actual game content in cards

Game cards SHALL display API cover, nullable banner and description, marker/category counts, and map summaries. Map counts SHALL derive from maps.length. When a map summary has `status === 'draft'`, its preview thumbnail in the game card SHALL display a compact badge in the top-right corner with a visibility-off icon (`matVisibilityOffRound`) and a `Draft` tooltip; published map previews SHALL NOT display a status badge on the game card. Missing optional images SHALL have neutral fallbacks; missing description SHALL not show fabricated game text. Empty map collections SHALL show an empty state. Loading skeletons SHALL remain distinct from loaded zero counts and null previews.

#### Scenario: Show persisted content

- **WHEN** a game is loaded in grid or list layout
- **THEN** its banner, cover, description, numeric counts and owned map names and previews replace demonstration placeholders
- **AND** any map summary with `status === 'draft'` renders the top-right draft icon badge with a `Draft` tooltip on its preview thumbnail, while `published` map summaries render no status badge
- **AND** null images and empty maps render without broken image elements
