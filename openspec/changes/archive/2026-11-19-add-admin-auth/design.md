# Design

## Context

Admin and web use relative /api requests behind the same HTTPS origin. Program.cs registers static media and tile middleware separately from management endpoints and MapHub. The admin already uses Angular HttpClient with XHR, Orval and a QueryClient with a custom MutationCache.

## Goals / Non-Goals

Use Microsoft's cookie, credential, lockout and CSRF implementations. Keep the session an API concern. The Domain project needs no user entity. Public SSR continues to use public endpoints independently of the admin session.

## Decisions

1. Use IdentityUser, IdentityUserContext and Identity EF Core stores in the existing SQLite database and migration workflow. Preserve current entity mappings and base Identity model configuration. Add IdentityCore, SignInManager and the Identity application cookie scheme as the authentication and challenge default. The small Auth endpoint group owns its handler and uses UserManager and SignInManager directly. Mapping the entire Identity API would expose registration and token flows outside this release.
2. Bootstrap through UserManager.CreateAsync after explicit migration, before serving requests. Read configuration only when the user store is empty. Supply credentials via deployment secrets or local user secrets. Check for an existing user before opening a transaction and recheck the store inside the transaction before inserting. Invalid inputs fail startup with a diagnostic that omits credentials. Rely on Identity password hashing, validation and lockout defaults. Use a fixed window of 30 login requests per minute and PasswordSignInAsync with lockoutOnFailure enabled. Return generic HTTP 401 Problem Details for rejected credentials and HTTP 429 Problem Details when the rate limit is reached.
3. Configure __Host-GameAtlas.Session, Path=/, no Domain, Secure, HttpOnly, SameSite=Strict. Use a nonpersistent eight-hour ticket, sliding expiration disabled, and durable protected Data Protection keys. Use SignOutAsync on logout. HTTP challenges and access denial return 401/403. Management route groups and the editor hub explicitly require authorization. An integration test verifies that every production route declares its access policy. Close the SignalR connection when authentication expires. Allow anonymous public reads and auth bootstrap endpoints explicitly. Keep both static delivery middleware branches reachable before the authorization gate.
4. Use ASP.NET Core IAntiforgery and Angular's existing same-origin XSRF support. Anonymous GET /auth/antiforgery issues a no-store response, a protected HttpOnly antiforgery cookie and a separate readable XSRF-TOKEN request token. Configure X-XSRF-TOKEN as the header. Validate unsafe management REST methods centrally, including JSON, multipart uploads, login and logout, after the authorization check. Acquire a token before submitting login and refresh it when a successful session lookup establishes or restores the authenticated session. Session responses use no-store. SignalR uses its own transport and requires a valid cookie. Reject a supplied Origin that differs from the request origin, except for http://localhost:4201 and https://localhost:4201 in Development.
5. Add admin-only auth/data-access and auth/feature-login with scope:auth. Generate the Auth-tagged client using the repository Orval conventions. Guard the management shell with a TanStack query that fetches GET /auth/session and refreshes the antiforgery token. The query has a five-minute stale time and disables retries. Show session lookup failures on the sign-in page. Build the username and password form with Angular Signal Forms, shared Field and Button components, and Helm primitives. Adapt the referenced interceptor for admin API 401s, coalesce redirects and validate returnUrl as a local admin route, falling back to Games. Clear the admin QueryClient and use a full browser navigation to sign-in on successful logout or an admin API 401. Preserve supported Games or Media destinations on expiry. Treat a logout 401 as an already expired session and show a retryable error for other logout failures. Preserve withXhr and the existing MutationCache. Queries stop retrying on 401/403 and retain bounded retries for transient failures. Keep mutation retries disabled.

Implementation references: [Spartan login-simple](https://github.com/spartan-ng/spartan/tree/main/apps/app/src/app/pages/%28blocks-preview%29/blocks-preview/login-simple), [auth interceptor](https://github.com/FullDream/headless-cms/blob/main/frontend/libs/iam/data-access/src/lib/auth.interceptor.ts), [retry configuration](https://github.com/FullDream/headless-cms/blob/main/frontend/src/app/app.config.ts).

## Risks / Trade-offs

- The same origin is a shared trust boundary. Preserve existing media MIME, nosniff and SVG sandbox headers. Knowing a file or tile URL grants read access even for drafts.
- Login and authenticated requests require a working browser HTTPS path. Document the local proxy setup and verify it without weakening production cookie flags.

## Migration Plan

Back up SQLite, apply the additive EF migration explicitly, configure HTTPS, persistent keys and first-run secrets, then deploy API and admin together. Verify login, protected writes, public reads and assets before removing bootstrap secrets. For rollback, restrict management traffic at the proxy before reverting to an API version that accepts anonymous writes. Keep the additive tables and existing catalog data.

Production mounts the admin build at /admin/ with this Angular base href. The proxy strips /api/ before forwarding API requests, forwards static media and tile paths unchanged and sends remaining page routes to public web.
