# games-feature-catalog

Lazy-loaded home page for the web application (`type:feature`, `scope:web`).
Cards are informational until game routes exist. There are no placeholder links.
The component consumes `GamesCatalog` resource signals directly, including its
loading, error, retry and empty states. Layout uses Tailwind and Spartan theme tokens.

Run `npm exec nx lint games-feature-catalog` and
`npm exec nx test games-feature-catalog` from the workspace root.

This non-buildable library uses the existing native Angular/Vitest builder with
the web application's development build as its compilation harness. Its own
spec tsconfig and project root select only this library's tests; no extra test
framework or buildable-library packaging is required.
