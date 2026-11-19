# Admin session data access

Generate the Auth-tagged Angular client with `npm exec nx -- run auth-data-access:generate-api`. Admin supplies the same-origin `/api` base URL. The guard checks the server session and refreshes the antiforgery token before rendering protected routes.
