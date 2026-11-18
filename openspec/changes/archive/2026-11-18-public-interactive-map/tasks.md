## 1. Public data and marker rendering

- [x] 1.1 Generate the Public Maps API client and expose the detail query through `PublicMapsQueryOptions`.
- [x] 1.2 Support explicit marker opacity for icons and fallback pins while preserving admin draft and pending presentation.
- [x] 1.3 Persist completed markers and display preferences with Dexie and expose reactive category progress and visibility.
- [x] 1.4 Dim completed markers to 0.35 opacity and filter them out when hide-completed is enabled.

## 2. Route and navigation

- [x] 2.1 Route published game and map slugs to the public viewer with loading, retry and back-to-game states.
- [x] 2.2 Fit the map below the persistent layout header and keep theme selection in that header.
- [x] 2.3 Resolve concrete style thumbnails and select the default ready style with a first-ready fallback.
- [x] 2.4 Place the filter action before style selection and provide map switching and home/game/map breadcrumbs in the filter panel.

## 3. Category filters

- [x] 3.1 Present filters in a left desktop drawer or a mobile bottom drawer using shared controls and English interface copy.
- [x] 3.2 Provide searchable groups and categories with counts and visibility toggles.
- [x] 3.3 Provide Show all, Hide all and Hide completed controls backed by local preferences.

## 4. Marker inspection

- [x] 4.1 Anchor the desktop popover to the marker and provide full description, media carousel and quick completion.
- [x] 4.2 Support expansion into the right drawer and returning to the popup.
- [x] 4.3 Display the mobile inspector at 50dvh with scrollable content and a completion checkbox in its header.
- [x] 4.4 Update details when the selected marker changes and use the measured drawer dimensions to reveal it without changing zoom.
