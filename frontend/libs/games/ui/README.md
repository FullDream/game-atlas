# games-ui

Admin presentation shared by Games features (`type:ui`, `scope:games`, `platform:admin`). Import from `@game-atlas/games/ui`.

- `GameFields` renders the basic-information form from caller-owned Signal Forms fields and emits cover/banner/status actions. Its `skeleton` mode follows the same two-column field, cover and banner layout without requiring form data.
- `GameForm`, `createGameFormModel` and `gameFormSchema` define the form contract, initial values and validation independently of generated API models.

This library owns no queries, mutations, dialogs or shared form state. It depends only on shared UI/util libraries; feature libraries adapt server data and own submission. Description, optional banner, release date and developer are editable fields. Description formatting buttons remain disabled. Release date uses the owned Spartan Date Picker with month/year navigation, automatic close on selection and built-in clearing. Its input accepts and displays dd.MM.yyyy. The picker binds directly to the Signal Forms Date-or-null field; features serialize local calendar dates to YYYY-MM-DD for the API, without UTC conversion.

Verify with Nx lint and the consuming admin production build. Frontend unit/E2E work remains deferred.
