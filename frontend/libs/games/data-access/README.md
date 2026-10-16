# games-data-access

Shared catalog models and native Angular `httpResource` data access
(`type:data-access`, `scope:shared`). Import `GamesCatalog` and `Game` from
`@game-atlas/games/data-access`.

`GamesCatalog.games` reads `/content/games.json`. Its resource owns loading,
success and error state; use `reload()` to retry. Parsing rejects malformed
records, duplicate identifiers/slugs, invalid UUID v4 identifiers and negative
or fractional map counts. Cover URLs are optional local absolute paths.

The published file lives at `apps/web/public/content/games.json`. IDs are stable,
lowercase UUID v4 strings, independent of the public slug. Preserve IDs when
updating content. New browser-side entities must use `crypto.randomUUID()`;
catalog display never generates or replaces IDs.

Only the web app's server configuration imports the JSON at build time. Its
prerender-only HTTP backend serves the file through Angular's normal interceptor
chain, so the HTTP transfer cache includes it in the prerendered HTML. Browser
hydration reuses that response; subsequent browser requests use the static file.
Rebuild the site when publishing content so the HTML and JSON stay consistent.
No production Node.js process or backend API is needed.

Run `npm exec nx lint games-data-access` and `npm exec nx test games-data-access`.
The non-buildable library uses the existing Angular/Vitest test builder with
the web development build as the compilation harness, scoped to its own specs.
