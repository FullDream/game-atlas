# Proposal

## Why

Public game links currently open a placeholder, while the web catalog still reads management contracts. Visitors need published game details and map summaries with immediate reuse of cached catalog content and consistent loading feedback.

## What Changes

- Replace the game placeholder with a responsive detail page based on the supplied reference: breadcrumbs, banner, cover, description, game counts and map cards.
- Add public marker and category counts to each public map summary and display both in map cards.
- Include the exact `Public Games` tag in Games Orval generation and introduce separate public query options and cache keys; migrate the web catalog to these contracts.
- Default all web queries to infinite stale time, while fetching complete details when only a catalog placeholder exists.
- Share the Spartan page progress component and declarative loading registration across admin and web, preserving admin header bindings.
- Out of scope: public interactive map viewer, admin API behavior changes, authentication, new artwork and frontend unit/E2E test work.

## Capabilities

### New Capabilities

- `page-loading`: Shared routing progress and declarative page loading registration for application shells.

### Modified Capabilities

- `public-games`: Include visible marker/category counts in public map summaries.
- `frontend-api-clients`: Generate public Games transport and provide isolated public query options with partial list placeholders.
- `web-games`: Read the published catalog and render game details and map cards.
- `web-shell`: Replace placeholder routing and configure infinite freshness with SSR hydration.

## Impact

Affected areas include Application Games public reads, backend public-read tests, shared Games data access, the web catalog and game detail feature, both application headers, shared UI, and owning documentation. The HTTP change adds fields to public summaries without changing management contracts. Existing Spartan primitives are available; generate additional primitives only if implementation actually needs them. Preserve the substantial existing working-tree changes.

Implementation also includes explicitly approved cleanup of existing shared-ui color-picker lint errors to unblock full affected-project verification.
