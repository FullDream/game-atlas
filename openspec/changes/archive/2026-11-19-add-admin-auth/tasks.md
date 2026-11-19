# Tasks

## 1. Provision Identity

- [x] 1.1 Register role-free Identity EF stores in the existing database. Generate and inspect the additive migration, then apply it to fresh and populated SQLite databases and verify existing catalog data.
- [x] 1.2 Bootstrap the administrator via UserManager and deployment secrets. Cover first start, invalid or missing credentials, repeated initialization and changed or absent secrets after provisioning with focused backend tests.
- [x] 1.3 Document explicit migration, bootstrap secrets, HTTPS forwarding and durable Data Protection keys in backend/deployment guidance. Verify a restart retains the account and a valid session.

## 2. Protect administrative APIs

- [x] 2.1 Implement Auth-tagged antiforgery, login, session and logout endpoints with Identity cookies, rate limiting and lockout. Verify cookie flags, expiry, generic login errors, logout and CSRF rejection with real Identity integration tests.
- [x] 2.2 Declare route access explicitly and validate unsafe REST requests centrally. Update GameApiFactory and existing management callers to authenticate and send antiforgery tokens. Verify representative reads, JSON writes, multipart uploads and editor hub negotiation with and without a session.
- [x] 2.3 Exercise guest public API reads, direct media and tile URLs, including draft assets. Verify existing file responses and publish filtering. Update API conventions and OpenAPI operation security metadata to distinguish anonymous operations from protected operations.

## 3. Integrate the admin SPA

- [x] 3.1 Add auth/data-access and auth/feature-login with admin platform tags and scope:auth. Configure the Auth-tagged Orval client, update frontend architecture guidance and verify generation and Nx boundary lint.
- [x] 3.2 Integrate the cached session query, guard, CSRF refresh, the 401 interceptor, full-page login redirect and TanStack retry policy. Manually verify one redirect for simultaneous 401s, zero retries on 401/403, inline handling of 403 and recoverable backend connection errors.
- [x] 3.3 Compose the Signal Forms login page with shared UI and Spartan primitives, and add a shell logout confirmation with retryable error feedback. Verify keyboard submission, invalid credentials, return to the requested editor, rejection of external return URLs, and clearing private data on expiry/logout. Run frontend formatting, affected lint and production builds under the current verification policy.

## 4. Verify the deployment flow

- [x] 4.1 Build the .NET solution and run the backend integration suite. Using the production /admin/, /api/ and public / mounts, verify deep links and asset paths, then sign in, edit, upload, connect the editor hub, log out and confirm subsequent management access is denied while guest maps and assets still load.
