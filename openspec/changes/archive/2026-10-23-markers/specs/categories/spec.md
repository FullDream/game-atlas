# Categories Spec Delta

## MODIFIED Requirements

### Requirement: Return a read-only marker count
Every category representation SHALL include integer markersCount equal to the number of currently persisted markers belonging to that category. Clients SHALL NOT control the count. Counts SHALL be computed from marker records without a persisted category counter. Categories without markers SHALL return 0. The existing category representation shape SHALL remain unchanged.

#### Scenario: Count across operations
- **WHEN** a category is created, listed, updated, or transferred
- **THEN** its representation includes markersCount equal to its current number of markers, including 0 when none exist
- **AND** PATCH attempts to change that field return HTTP 400

#### Scenario: Create delete and transfer markers
- **WHEN** a marker is created or deleted
- **THEN** the next category representation reflects the resulting count
- **AND** transferring a marker within its game decreases the source category's count and increases the destination's count, while same-category no-ops do not change either count

#### Scenario: Retain counts on category changes and failures
- **WHEN** a populated category is renamed, reordered, or transferred to another group of the same game
- **THEN** its response retains the actual marker count and its marker ownership remains unchanged
- **AND** a failed marker mutation leaves counts unchanged without writing category metadata or timestamps

#### Scenario: Preserve unrelated counts during cascade deletion
- **WHEN** a category, group, or game is deleted with its markers
- **THEN** the related marker rows are physically removed and surviving categories report their actual remaining counts
- **AND** unrelated categories and their timestamps remain unchanged
