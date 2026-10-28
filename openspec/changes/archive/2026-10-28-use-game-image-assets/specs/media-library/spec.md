# Media library delta

## MODIFIED Requirements

### Requirement: Delete only unused assets immediately

DELETE /media/assets/{id} SHALL return 409 while a game image, category icon, marker attachment, style, or video preview references it. Otherwise it SHALL delete the physical file immediately and remove its record before returning empty 204. Missing files SHALL be tolerated; repeated deletion SHALL return 404. Serialized relationship checks and restrictive FKs SHALL prevent dangling references. No active-reader leases, cleanup intents, deferred deletion, startup cleanup, or retry jobs SHALL be introduced.

#### Scenario: Reject persisted uses

- **WHEN** any current resource or preview references an asset
- **THEN** deletion returns 409 without detachment or byte changes
- **AND** uses across all games count even when original bytes are missing

#### Scenario: Delete an unused original

- **WHEN** the last relationship is removed and the client deletes the asset
- **THEN** the original is deleted synchronously and 204 follows record removal
- **AND** an IO failure returns safe 500 while retaining metadata, without scheduling cleanup

#### Scenario: Accept independent storage and metadata failure boundaries

- **WHEN** a DB failure follows physical deletion or a crash interrupts the operation
- **THEN** metadata may reference a missing file or an orphan may remain
- **AND** no automatic reconciliation infrastructure is added

#### Scenario: Protect a game cover

- **WHEN** an image is selected by at least one game, including when its original file is missing
- **THEN** deleting the asset returns 409 and preserves both the asset metadata and game references

#### Scenario: Release a game cover

- **WHEN** every game using an image has been deleted or assigned another image
- **THEN** the former cover remains in the library and can be explicitly deleted only if no other resource references it

#### Scenario: Race image assignment and deletion

- **WHEN** a game image is assigned or replaced concurrently with deletion of that asset
- **THEN** the operations cannot commit a game referencing a deleted asset
- **AND** deletion refused for a committed game reference returns 409 before removing bytes
