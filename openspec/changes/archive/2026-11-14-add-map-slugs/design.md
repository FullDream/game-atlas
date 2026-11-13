# Design

## Context

See [proposal.md](proposal.md) for motivation and scope. `Game` currently stores slug as a string and validates its syntax internally; Application validators repeat the pattern and enforce the 200-character maximum. `GameConfiguration` already has a unique slug index. Maps have game-scoped normalized-name uniqueness, ID-based operations and metadata editing in both the list modal and Settings & Tools. `MapDto` and `GameMapDto` currently omit slug.

The working tree contains user edits adding a string slug property and a factory argument to `Map.cs`, without assigning the property or adapting callers. Implementation must incorporate those edits while preserving unrelated user formatting and behavior. This proposal does not modify that file.

This is a cross-layer model and migration change, so a design artifact is required.

## Goals / Non-Goals

**Goals:** Make valid slug state reusable across games and maps, preserve HTTP validation semantics, and upgrade populated SQLite databases without touching map files or processing state.

**Non-Goals:** No slug-based queries, route binding, public APIs, redirects, historical aliases, extra database provider or slug-generation dependency. IDs remain resource and cache identities. This change does not redesign name validation or existing map presentation behavior.

## Decisions

### Shared immutable domain value

Place `Slug` and its validation errors in a shared Domain value-object namespace, outside Games and Maps. Use a sealed immutable reference value object with a private constructor, ordinal value equality and a `Create(string?)` factory returning `Result<Slug>`. Expose the canonical string through `Value` and `ToString()`. Do not expose construction or implicit string conversion that bypasses validation. A reference type avoids the invalid default state of a value-type wrapper; entity methods must still guard required references.

The factory owns trimming, the 1..200 limit and the existing absolute-anchor pattern `\A[a-z0-9]+(-[a-z0-9]+)*\z`. It rejects uppercase, non-ASCII text and malformed separators without repair. Both entity properties use `Slug`, and domain create/update operations accept validated values. Uniqueness stays outside the value object because its scope belongs to the resource and database.

Alternative: retain strings or separate GameSlug/MapSlug types. Both would duplicate identical invariants. Automatic lowercasing or title-based generation would change existing game behavior and is outside scope.

### Preserve request validation and PATCH presence

API request/response fields remain strings, with `Optional<string?>` for PATCH. Keep nullable raw creation input for name and slug so missing/null values reach the existing Application validation behavior and produce field-specific 422 responses. Nullable input does not make persisted fields optional.

Current commands can retain raw strings at their validation boundary. Application validation uses the shared slug factory rather than a second regex; handlers obtain the validated value object before invoking domain operations and remain defensive when called directly. Preserve game field/error compatibility by mapping common slug failures to the owning resource's field errors where needed. No duplicate validation orchestrator or API-only validator pipeline is introduced.

For updates, resolve omitted slug from the current entity, parse supplied values before mutation, then validate the complete resulting state before assignment. A patch with invalid slug must not partially change name, default or style order. Equal trimmed values preserve timestamps. Renaming never regenerates slug. Existing queries retain Guid parameters; future public queries may accept `Slug` after boundary validation.

Alternative: parse only in endpoint mapping and make every command typed now. That would require replacing current command-validation flow without helping the ID-based queries in this change.

### EF conversion and independent constraints

Implement a reusable `ValueConverter<Slug, string>` in Infrastructure and apply it to both entity properties. Store the entire scalar string rather than an owned object or JSON. Materialization reconstructs a validated value; corrupt stored data must not silently produce an invalid slug. Use max length 200 and required columns. For query comparisons, compare converted properties with a `Slug` parameter, avoiding string operations on `Slug.Value` inside database predicates. DTO mapping unwraps after materialization where necessary.

Retain `Games(Slug)` and `Maps(GameId, NormalizedName)` unique indexes. Add `Maps(GameId, Slug)`. Application checks exclude the target map during updates and provide early 409 results; the unique database index handles competing writers. Extend the existing persistence classifier and centralized map conflict handler to recognize specifically the map slug constraint. Name conflicts and unrelated persistence failures retain their current classification. Do not rely solely on the in-process map mutation lock.

Alternative: application-only uniqueness is insufficient for competing processes. Replacing name uniqueness would change an existing independent requirement.

### Consistent contracts and existing admin surfaces

Add required response string slug to `MapDto` and `GameMapDto` and update every producer, including game listing and detail mapping. Document creation name/slug requirements, PATCH presence/null rules, length and pattern in OpenAPI without exposing the domain value-object schema. Map and game paths remain ID-based, including tiles and SignalR membership.

Regenerate Maps and Games clients through their existing explicit HTTP generation targets. Add a required slug input to the existing map creation dialog and editable slug fields to its editing mode and Settings & Tools. Preserve user drafts, pending/error behavior, changed-field PATCH submission and authoritative cache reconciliation. Do not derive slug when name changes. These are compatibility changes to existing forms, not new navigation or public pages.

### Deterministic backfill

Create a new migration after the current latest migration. Add the map column in a state that allows backfill, populate `map-` plus the lowercase hyphenless existing UUID, then enforce NOT NULL and game-scoped uniqueness. Adapt the SQL to the actual existing SQLite UUID storage representation and verify byte-for-byte agreement with `.ToString("N")`; do not assume GUID byte order or text casing. Remove any transitional empty/default value so it cannot serve as an implicit slug for future inserts. Game rows and their existing slug strings require no rewrite.

This avoids transliteration, title collisions and operator input for old maps. The fallback is only for existing rows; newly created maps always require a supplied slug. Backfill is a schema operation and must not invoke domain updates, advance timestamps, enqueue processing or change files.

### Compatibility discovered during client regeneration

The user approved these compatibility repairs after regeneration exposed existing schema drift. Preserve null in the schema when unwrapping `Optional<Nullable<T>>`, including Category icon/placement GUIDs; validators may still reject null for required fields. Adapt the Games listing imports to generated ListGamesSortBy/ListGamesSortDirection names without changing sorting behavior. Regenerate affected clients and verify existing category reset and game sorting contracts as well as the admin build. These repairs preserve documented behavior rather than introduce new resource features.

## Risks / Trade-offs

- Required creation slug breaks old map clients -> deploy updated forms and API together; document the contract change.
- SQLite table rebuilding can affect foreign keys and dependent rows -> verify migration against a populated database with styles, content, defaults and processing metadata before deployment.
- Converted scalar queries may fail translation if they access wrapper members -> use value-object equality and verify against the actual SQLite provider.
- Shared errors can change existing game validation details -> retain resource field mapping and focused game HTTP regression checks.
- Existing user edits are incomplete -> adapt factory callers during implementation without reverting those edits or broad formatting changes.
- Rolling back removes editable map slug metadata -> back up the database first; restore the backup if those values must be retained.

## Migration Plan

1. Back up the database and retain the existing file store. Coordinate a maintenance window for this breaking creation-contract change.
2. Explicitly apply the new migration using EF CLI; do not migrate at startup or rewrite migration history.
3. Verify deterministic backfill, constraints and preservation of dependent data, timestamps and files. Deploy compatible backend and admin clients together.
4. Verify create, metadata PATCH, duplicate conflict and game summaries through ID-based endpoints.
5. For rollback, stop the updated application and restore the prior application/client versions together with a database backup, or explicitly downgrade the new migration after exporting any map slugs that must survive. Dropping the slug column loses those values but must preserve the existing map ownership hierarchy and files.
