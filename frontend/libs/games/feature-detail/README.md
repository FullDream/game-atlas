# Public game details

Web-scoped feature lazy-loaded at `/games/:slug`. Reads public game details through PublicGamesQueryOptions and renders responsive hero metadata and published map summaries with marker/category counts.

Cached catalog fields are temporary placeholders; missing description/maps use skeletons until complete detail arrives. Handles direct SSR reads, image fallbacks, empty maps, unavailable games and retryable errors. Request activity contributes to shared header progress through uiPageLoading. Map previews, names and arrow actions link to `/games/:slug/maps/:mapSlug`. This componentless route reserves the destination without rendering a map viewer yet.

Uses existing Spartan/shared primitives. Frontend verification follows the repository's deferred unit/E2E test policy: affected lint and consuming application production builds, with manual runtime checks.
