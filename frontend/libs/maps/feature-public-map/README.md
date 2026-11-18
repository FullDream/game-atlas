# Public map viewer

The web application composes this feature at `/games/:slug/maps/:mapSlug`.
Its route resolver reads public game details through `PublicGamesQueryOptions`
and the existing QueryClient cache, then supplies the transport-independent
`PublicMapGame` input. Maps does not import Games data access.

The filter header displays the game name, cover and optional banner. The banner
fills the header up to the drawer surface edges, with rounded top corners and a theme-aware gradient to preserve text contrast. On mobile, the banner extends behind the decorative handle; the drawer retains its native drag behavior.
The current map opens a Spartan dropdown containing links to published maps;
map entries show the source-image preview with the same MediaThumbnail component
as the style selector, a fallback map icon when absent, and a radio checkmark for
the current route. The trigger keeps its chevron aligned at the trailing edge.
The control is disabled and its chevron hidden when at most one map is available.
If game metadata cannot be loaded, map loading and the existing slug fallback
remain available.

The viewer reads marker completion and display preferences through
`MapClientDb.query`. Visibility and expansion are enabled for groups without a
saved configuration. Group visibility, group expansion, category visibility
and the Hide completed switch persist in IndexedDB.
