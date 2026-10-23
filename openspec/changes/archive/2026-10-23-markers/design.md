# Design

## Context

See proposal.md for motivation and the markers/categories deltas for behavior. The .NET 10 solution already uses four feature-oriented layers, directly persisted domain entities, IApplicationDbContext with EF entity sets, IApplicationTransaction, shared FluentValidation/Result orchestration, typed OutcomeResult responses, and API-owned JSON/OpenAPI configuration. CategoryGroups and Categories demonstrate short SQLite mutation transactions, direct game deletion, database cascades, and failure-injection tests.

Marker storage does not exist. CategoryMappingExtensions currently hardcodes zero in both entity and query mappings; create and update handlers use the entity mapping while list uses the query projection. Position introduces the first nested mutable request object. Media introduces the first persisted inheritance hierarchy and discriminated public union. The significant model, serialization, and migration changes justify this design artifact.

## Goals / Non-Goals

**Goals:**

- Keep Marker as the atomic mutation boundary with a finite immutable Position and protected media collection.
- Persist the agreed domain hierarchy directly while keeping JSON and EF metadata in their consuming outer layers.
- Implement the exact nested-object/array distinction of JSON Merge Patch and preserve meaningful no-ops.
- Compute accurate category counts and load complete marker lists without queries proportional to the number of rows.

**Non-Goals:**

- No repository layer, new module projects, generic aggregate framework, spatial database, or second provider.
- No public child identities or per-attachment commands; internal media identity does not imply a separate aggregate.
- No changes to the frontend or to game map management, and no storage/provider APIs for linked files.

## Decisions

### 1. Model Marker as the aggregate root

Add Domain/Markers/Marker with UUID v7 Id, CategoryId, Name, nullable Description, Position, UTC CreatedAt/UpdatedAt, and a private media collection exposed for reading. Category remains a separate aggregate referenced by ID; deriving game ownership through Category and CategoryGroup avoids an additional mutable GameId column. Application queries enforce route game scope through those relationships. Do not require Game.MapId or build a Map entity as part of this feature.

Domain factories and mutation methods return Result. Position and each concrete media subtype validate their own invariants; Marker validates a complete proposed state before assigning scalar values or replacing children. Keep domain and operation errors in separate nearby static classes. Application checks category existence/ownership and coordinates persistence, while shared validators check request presence, string limits, subtype fields, and indexed errors such as media[1].width. No handler-specific exception-based validation is introduced.

### 2. Use an immutable Position value object

Position is an immutable record with X and Y doubles, value equality, a private construction path, and a Result-returning factory rejecting non-finite values. It has no identity and no latitude/longitude restrictions. Configure it as a required EF Core complex property in MarkerConfiguration, explicitly mapping its read-only members to PositionX and PositionY columns in Markers. Include an early SQLite materialization test for the factory-backed immutable shape against the pinned EF version; use explicit backing-field/materialization configuration if required without exposing public mutation or adding a separate Position table.

API creation position uses a dedicated request object with nullable double fields so missing/null differs from valid zero. Update position uses a separate object with Optional<double?> X/Y inside Optional<UpdateMarkerPositionRequest?> Position. Create requires both coordinates; PATCH merges only supplied members with the stored pair and creates a validated complete Position. Optional wrappers belong to request/use-case orchestration, not to Position itself. Preserve the same nesting in PositionDto and map in extension files.

Reference: [EF Core complex types](https://learn.microsoft.com/en-us/ef/core/modeling/complex-types). Use the existing .NET/EF 10 features; examples on that page marked EF 11 are not a reason to upgrade.

### 3. Implement JSON Merge Patch semantics deliberately

Reuse WithMergePatch for both accepted media types and their charset handling. This convention only checks media types: nested merge behavior must be implemented by request binding, Optional values, and handler orchestration. Do not assume an automatic document-merging engine already exists.

For updates, all five mutable fields are optional. An empty root object fails the existing at-least-one-field rule. A supplied empty position object is a valid no-op; missing nested coordinates preserve existing values. Explicit null position/x/y is a domain validation failure. A description null clears the optional value. A supplied media array is replacement input: each element is complete, without merging by index or internal identity. Null media is invalid and [] clears the collection.

Unknown/read-only root PATCH fields, unknown position members, unknown or mismatched media fields, and missing/unknown discriminators are binding failures (400). Missing/null required subtype values are represented with nullable input fields and validated as 422; avoid C# required-member binding that would turn these into 400. Null media elements receive validation errors. Apply the entire state only after all validations and category checks succeed.

Reference: [RFC 7396, section 2](https://www.rfc-editor.org/rfc/rfc7396.html#section-2). Object-valued members recurse; arrays replace as a whole. Domain immutability does not change the HTTP merge contract.

### 4. Use a small media child hierarchy and TPH

Create abstract MarkerMedia with concrete ImageMedia, YouTubeVideoMedia, and VideoMedia. The children have internally generated IDs, marker ownership, and an internal sequence position. IDs support relational entity tracking but are neither returned nor accepted by the API. There are no independent media handlers, repositories, or endpoints. Subtype factories enforce their distinct data rules; collection changes occur through Marker.

Configure a normal entity hierarchy, not owned/complex-type inheritance. Use one MarkerMedia table and a shadow string discriminator with values image, youtubeVideo, and video. Configure a required Marker FK with cascade delete and an index on (MarkerId, Order). Domain methods assign sequence positions from the supplied array; Order is not a public mutable field. Derived-only columns are nullable across other discriminator rows, with subtype validity enforced in Domain and validation. The CLR domain hierarchy does not need a discriminator property solely for EF.

Image fields are Url, Width, Height, and Alt. YouTube fields contain VideoId. Video fields contain Url and nullable Preview. Shared image/video Url columns can be configured explicitly when their mapped types match. No JSON attributes belong on domain entities. Keep EF configuration in Infrastructure and avoid separate persistence models while the agreed domain and relational shapes match.

Reference: [EF Core TPH inheritance](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-hierarchy-and-discriminator-configuration). Compared with JSON storage, TPH satisfies the selected relational discriminator design and cascade behavior; compared with TPT it keeps reads and replacement writes simple.

### 5. Preserve no-ops by comparing media content

Compare the proposed media sequence with existing children by subtype and normalized public values, in order, excluding generated IDs and technical ownership/order properties. An equal sequence preserves the existing instances, internal IDs, timestamps, and write-free no-op behavior. Different content or order replaces the collection with new child instances. Public identity retention across actual replacement is not promised.

Use one explicit transaction for parent checks and all marker/media writes. Load tracked aggregate children for update/delete as needed; replace via the mapped collection with orphan deletion/cascade configured and tested. Deleting old children and inserting replacements must roll back together if any later write or commit step fails. Avoid a unique order index that creates unnecessary insert-before-delete conflicts during replacement; ordering is assigned by aggregate logic.

### 6. Keep request and response polymorphism at the API boundary

Add Api/Markers/Endpoints.cs with registrations and private handlers in GET, POST, PATCH, DELETE order. Use ListMarkers, CreateMarker, UpdateMarker, and DeleteMarker Application slices. Request declarations, response DTOs, and mapping extensions stay in separate files in their consuming layer.

Use an abstract API media request base with JsonPolymorphic/JsonDerivedType metadata and concrete request variants. Map these to typed Application media inputs; HTTP metadata does not enter Application or Domain. Keep Application-owned MarkerDto, PositionDto, and media DTO hierarchy free of JSON annotations. Register response DTO polymorphism through the API-owned System.Text.Json contract model (JsonPolymorphismOptions), so the existing OutcomeResult<ApplicationDto> pattern can remain intact without adding a duplicate complete response hierarchy. Requests and responses use the same three public discriminator strings, with no extra source field.

The current HTTP JSON options already enable AllowOutOfOrderMetadataProperties. Preserve that so type need not appear first in a media object. Reject unknown type values without falling back to the base type. Document media array items as oneOf with discriminator.propertyName=type and three explicit mappings in both request and response schemas. Start with native polymorphism metadata; add a narrow media schema transformer only when the generated .NET 10 document needs correction to achieve that exact contract. Do not broadly convert every anyOf into oneOf. Reuse the FluentValidation metadata integration for nested lengths, bounds, and patterns, adding only narrowly tested support if a real gap appears.

References: [ASP.NET Core polymorphic schemas](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0#polymorphic-types) and [System.Text.Json contract model](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism). Schema/runtime alignment needs tests rather than an assumption that CLR inheritance alone guarantees the required OpenAPI output.

### 7. Compute category counts without broad aggregate loading

Add Markers to IApplicationDbContext and ApplicationDbContext. Category list projections use a correlated marker Count by CategoryId or an equivalent grouped SQL projection; do not fetch all markers or issue one count query per category. Update the category mapping extensions to accept/project actual counts. Creating a new category can return a known zero; updating/renaming/reordering/transferring an existing category must query its actual count in the existing transaction. No new marker-count property is persisted on Category.

Marker creation/deletion/transfer automatically changes the next count query through changed rows. Parent timestamps remain unchanged. Existing category API docs and descriptions must stop promising zero. Maintain the existing DTO fields, category validation rules, and count field's read-only behavior. Test category mutation responses with populated categories, not only the list endpoint.

### 8. Load complete marker lists with bounded query count

Check game existence first. Scope markers through category/group game ownership, apply CreatedAt then Id ordering, and use AsNoTracking. Load the ordered media collection with an explicit split query or a bounded second query keyed by the selected marker IDs, and map outside SQL where polymorphic mapping needs it. Do not issue a query per marker, use lazy loading, or call a nontranslatable mapping method inside the SQL predicate. Verify query count stays bounded as marker and media counts increase, and preserve marker ordering and media ordering independently.

No list controls are introduced. The final decision removed categoryId/categoryIds filters entirely, including explicit FromQuery aliases and validation of filter IDs. Full descriptions and media are intentionally returned for now.

### 9. Record conservative validation defaults

The discussion fixed name 1..200, description up to 5000, finite doubles, positive integer image dimensions, empty-default/non-null Alt, optional Preview, and discriminator values. The following smaller details use established conventions or simple initial defaults:

- Url and nonblank Preview are trimmed opaque references of at most 2048 characters, matching existing image-reference limits. Blank Preview normalizes to null; no URL scheme, extension, or remote availability check is added.
- Alt preserves supplied text and defaults to empty only when omitted. No additional maximum alt length or application-level attachment-count cap is introduced without a requirement.
- The initial YouTube identifier contract requires 11 ASCII letters/digits/hyphens/underscores, case-sensitive and without URL parsing. This is local syntax validation, not proof of existence or embeddability. The backend does not generate embed HTML. See the [YouTube embed identifier contract](https://developers.google.com/youtube/player_parameters#Manual_IFrame_Embeds) for client use of video IDs.
- Valid category IDs are checked by scoped lookup; an all-zero ID is a missing resource (404), while omission/null at creation is 422. Existing games need not have MapId set because map management is not implemented here.
- Repeated equal attachments are allowed. Media replacement alone with equal normalized ordered content is a no-op. Other successful mutations update only the marker timestamp.

## Risks / Trade-offs

- [Full marker/media lists can grow large] -> Keep queries bounded and avoid pagination/filter machinery until a real loading requirement changes the agreed contract.
- [MarkerMedia is entity-shaped for TPH despite replacement semantics] -> Keep IDs internal and lifecycle inside Marker; revisiting public per-media editing requires its own contract change.
- [Polymorphic request, serializer, and schema metadata can diverge] -> Test every subtype, unknown/mixed fields, out-of-order type, response round trips, and both OpenAPI media-array schemas against the running app.
- [Position mapping is new to this repo] -> Validate immutable value-object materialization early with the actual EF 10 SQLite provider; preserve factory-controlled domain construction and inward dependencies.
- [Multiple replacement SQL commands can partially execute] -> Test failures after delete/insert and after SaveChanges but before commit, verifying reopened storage and counts.
- [SQLite serializes write transactions] -> Keep writes short and perform no network calls; reuse IApplicationTransaction rather than introducing provider APIs into Application.
- [Cascades physically remove linked records] -> Verify current ownership after marker/category transfers and preserve other games; referenced files remain untouched.

## Migration Plan

1. Add Marker/Position/media mappings and generate the migration through EF CLI in Infrastructure, including designer and snapshot. Previous migration is 20261023114301_Categories. Do not edit historical migrations.
2. Create Markers with CategoryId cascade FK, indexed category membership, PositionX/PositionY doubles, metadata, and UTC timestamps. Create MarkerMedia with its cascade FK, discriminator, typed nullable columns, and ordering index. Do not add a stored category count or media IDs to public DTOs.
3. On disposable SQLite databases, test upgrade with existing games/groups/categories, all subtype round trips, position materialization, no pending model changes, failed replacement rollback, and cascades including ExecuteDeleteAsync game deletion.
4. Apply the migration explicitly through documented EF CLI deployment commands before serving requests. Implementation tests do not authorize applying it to a persistent local/user database; startup migration behavior stays unchanged.
5. Document rollback to 20261023114301_Categories after stopping writes and backing up data. Rollback removes marker/media data and preserves existing games/groups/categories; the prior application version restores its temporary-zero counter behavior.
