# Public Maps Spec Delta

## Purpose

Provide complete public map data in one request and a reusable draft-aware read behavior for future protected administrator previews.

## ADDED Requirements

### Requirement: Resolve a published map within its game

Unauthenticated GET /public/games/{gameSlug}/maps/{mapSlug} SHALL resolve the map within the routed published game and require published map status. Missing, deleted, hidden or foreign resources SHALL return 404 ProblemDetails. The read SHALL NOT modify data, timestamps or processing state.

#### Scenario: Scope identical map slugs

- **WHEN** two games own maps with the same slug
- **THEN** the response contains only the map owned by the routed game
- **AND** a draft game hides even its published maps

### Requirement: Return complete public map data

Map details SHALL include id, gameId, name, slug, nullable width/height and logical geometry, nullable defaultStyleId, styles, categoryGroups and markers. Groups SHALL contain nested categories; markers SHALL be a flat array referencing categoryId. Empty collections SHALL be arrays. The response SHALL omit publication statuses, operational processing details and administrative timestamps.

#### Scenario: Load all map content in one response

- **WHEN** a visible map contains 2000 visible markers
- **THEN** one successful response includes all markers and categories without pagination or viewport parameters
- **AND** media and tiles are represented by URLs rather than embedded bytes

### Requirement: Expose only ready rendering styles

Styles SHALL contain id, name, backgroundColor, tileGeometry and non-null tilesUrlTemplate, in persisted display order. Only ready styles SHALL appear in either visibility mode. defaultStyleId SHALL reference a returned ready style or be null. A map without ready styles SHALL remain readable with styles=[] and defaultStyleId=null, preserving nullable or previously initialized logical geometry.

#### Scenario: Mix processing states

- **WHEN** ready, queued, processing and failed styles belong to a map
- **THEN** only ready styles appear, preserving their relative display order and selected ready default
- **AND** no progress, failure diagnostics or source image metadata appear in style representations

### Requirement: Filter the entire content hierarchy

Public groups, categories and markers SHALL require published status and visible ancestors. Groups and categories SHALL preserve display order and include id, name, icon, color and markersCount; categories SHALL include categoryGroupId. Markers SHALL include id, categoryId, name, nullable description, position and media. Counts SHALL equal visible descendant marker counts. Returned markers SHALL reference returned categories.

#### Scenario: Avoid hidden content and dangling references

- **WHEN** groups, categories or markers are draft within an otherwise published map
- **THEN** hidden nodes and their descendants are omitted and counts exclude them
- **AND** visible empty groups and categories remain with zero counts
- **AND** canonical media representations are reused for icons and attachments

### Requirement: Support internal preview visibility

Detailed map retrieval SHALL support PublishedOnly and IncludeDrafts behavior. IncludeDrafts SHALL permit draft games, maps, groups, categories and markers while preserving ownership, ready-style selection and the same response shape. Public HTTP reads SHALL always use PublishedOnly; no client-supplied flag SHALL enable draft reads. No preview HTTP route SHALL be introduced in this change.

#### Scenario: Reuse retrieval for future preview

- **WHEN** internal retrieval requests IncludeDrafts for a draft hierarchy
- **THEN** the detail result includes draft and published content with counts matching all returned markers
- **AND** the corresponding public request returns 404 for a draft root

#### Scenario: Public query cannot enable preview

- **WHEN** a public request supplies includeDrafts=true or visibility=IncludeDrafts
- **THEN** visibility remains PublishedOnly and no additional draft content is returned

### Requirement: Document independent public map reads

OpenAPI SHALL expose GetPublicMap under the exact tag Public Maps with dedicated public map, style, group, category and marker schemas. Canonical media schemas SHALL be reused and no preview visibility parameter SHALL be exposed. Existing map, category, marker and file-delivery contracts SHALL remain unchanged.

#### Scenario: Discover the complete response

- **WHEN** a consumer inspects GetPublicMap in OpenAPI
- **THEN** the complete category tree and marker array are documented without processing metadata
- **AND** the operation uses string gameSlug and mapSlug path parameters
