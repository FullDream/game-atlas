## ADDED Requirements

### Requirement: Consume canonical public schema names

Existing frontend API clients SHALL use public OpenAPI schema names without DTO suffixes or parent-prefixed concrete media variants. Handwritten imports, exports, and type annotations SHALL migrate to those generated names without legacy aliases. Generation SHALL continue to read the running backend document over HTTP.

#### Scenario: Regenerate existing domain clients

- **WHEN** Games and Media clients are regenerated from the updated backend document
- **THEN** they expose canonical resource types and discriminated media unions
- **AND** video previews reference the shared nullable image type
- **AND** handwritten consumers compile with updated imports and names
- **AND** normal frontend builds still use committed generated files without fetching OpenAPI
