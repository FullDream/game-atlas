# Proposal

## Why

The backend media library is available, but administration still exposes a placeholder. Operators need a complete browser for organizing and uploading reusable assets before attaching them to content.

## What Changes

- Replace the Media placeholder with a responsive folder/file grid, breadcrumbs, local search and URL-backed pagination.
- Add single-file upload cards with transfer progress, processing state, retry and background continuity while navigating folders.
- Add a right-side details drawer, compact cards with icon context menus and folder drag/drop, folder creation, move dialogs, image selection for video covers and confirmed deletion.
- Supply folder breadcrumbs to the sticky shell header through the shared CDK portal integration; collapse long paths through Spartan overflow. Keep create/upload actions beside search, using icon-only buttons on mobile. A disabled-in-root Up button preserves toolbar geometry. Keep pagination fixed below independently scrolling cards. Center theme controls in the sidebar footer, aligned with pagination, and cycle modes through one button when collapsed.
- Add geometry-preserving listing skeletons, retain results during same-folder filter/pagination changes and keep known-parent navigation available during pending or failed requests. Fit mobile breadcrumbs to their actual labels, show header loading without layout shifts and distinguish eligible/active drag feedback from ordinary clicks.
- Protect unsaved edits during in-app navigation without intercepting browser reload or tab closure.
- Keep GET /media on PaginatedResult<MediaItemDto> and add GET /media/folders/{id} for independently cached current-folder metadata and ancestors. Raise the list limit to 120 with a default of 24.
- Add shared Media Orval transport and handwritten TanStack query/mutation integration, preferring safe cache updates over refetches.
- Make rename and move optimistic with cache snapshots, server-response reconciliation, rollback and error toasts. Keep confirmations/guards as Observables and move templates longer than three lines into HTML files.
- Out of scope: public web, content attachment screens, batch operations, automatic covers, upload persistence across reloads, authentication and E2E suites.

## Capabilities

### New Capabilities

- `admin-media-library`: Administration browsing, upload, editing and organization workflows.

### Modified Capabilities

- `media-library`: Retrieve folder context and ancestor breadcrumbs separately; retain ordinary pagination for lists.
- `admin-shell`: Replace the Media foundation destination with a working feature while retaining Games as a foundation page.

## Impact

Backend Application pagination and Media listing, API/OpenAPI and associated tests; frontend admin routing/providers, new media data-access and feature libraries, committed generated clients and reusable UI as needed. No database schema change is expected. The listing uses the shared pagination contract; a separate folder-detail endpoint supplies navigation context. Frontend dependency boundaries and theme conventions remain in force.
