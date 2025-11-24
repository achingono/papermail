## Docker Development Stack

An end-to-end local stack is provided via `docker-compose.yml` including:

* `mailserver`: SMTP/IMAP server (docker-mailserver) exposed on ports 25, 587, 993
* `oidc`: Simple OIDC Provider (qlik/simple-oidc-provider) for local OAuth2 / OpenID Connect
* `papermail-web`: The PaperMail ASP.NET Core application (built from source)

### Quick Start

```bash
docker compose pull
docker compose build papermail-web
docker compose up -d
docker compose ps
```

Visit the app at: <http://localhost:8080>
OIDC discovery endpoint (for debugging): <http://localhost:8081/.well-known/openid-configuration>

### Test User / Client

Configured client:

* Client Id: `papermail-web`
* Client Secret: `papermail-secret`
* Redirect URI: `http://localhost:8080/oauth/callback`

Configured user:

* Username (sub): `user1`
* Email: `user1@example.test`
* Password: `Password123!`

### Adding a Mail Account

Create a mailbox inside the mailserver container (example):

```bash
docker exec -ti mailserver setup email add user1@example.test Password123!
```

### Sending a Test Email

From host (requires `swaks` or `telnet`):

```bash
swaks --to user1@example.test --from tester@example.test --server localhost:587 --header "Subject: Docker Test" --body "Hello from docker stack" --auth-user user1@example.test --auth-password Password123!
```

### Environment Mapping

The app uses `ASPNETCORE_ENVIRONMENT=Docker` which loads `appsettings.Docker.json` providing container hostnames.

### Rebuilding

```bash
docker compose build --no-cache papermail-web
docker compose restart papermail-web
```

### Tear Down

```bash
docker compose down -v
```

### Notes

* OIDC provider config is inline via environment variables; adjust scopes or clients in `docker-compose.yml`.
* For production, replace simple-oidc-provider with a hardened identity platform and configure TLS for mailserver.
* Current IMAP access flow still uses OAuth access token, but the local mailserver expects username/password; future iteration may introduce a credential bridging layer or switch to basic authentication for local dev.

## End-to-End Tests (Playwright)

Playwright tests live in `test/PaperMail.EndToEnd.Tests`.

### Prerequisites

1. Docker stack running (`docker compose up -d`)
2. Browsers installed for Playwright.

Install browsers after first build:
 
```bash
dotnet build
bash test/PaperMail.EndToEnd.Tests/bin/Debug/net8.0/playwright.sh install
```

### Running Tests

```bash
PAPERMAIL_BASE_URL=http://localhost:8080 dotnet test test/PaperMail.EndToEnd.Tests
```

### Included Scenarios

* OAuth login and Inbox load
* Compose page form presence
* Keyboard shortcuts: `c` (compose), `i` (inbox)

### Adjusting Credentials

Update OIDC user/password in `docker-compose.yml` USERS env and modify `PlaywrightFixture.LoginAsync` if needed.

# PaperMail - Email Client for E-Ink Devices

## Project Overview

PaperMail is a web-based email client specifically designed for paper tablets and E-Ink devices such as the Kindle Scribe, Kobo, and similar devices with built-in browsers. The application prioritizes simplicity, performance, and compatibility with the unique constraints of E-Ink displays and older browser engines.

## Vision

To provide a seamless, distraction-free email experience optimized for the calm, focused reading experience that E-Ink devices naturally afford, without the bloat and complexity of modern web applications.

## Target Audience

* **Primary**: Users of paper tablets with built-in browsers (Kindle Scribe, Kobo Elipsa, reMarkable, etc.)
* **Secondary**: Users seeking a minimal, low-bandwidth email interface
* **Use Cases**:
  * Reading and responding to emails on E-Ink devices
  ## Local Development & Docker Stack

  The local stack (defined in `docker-compose.yml`) now runs behind an NGINX reverse proxy with HTTPS and custom development domains. Services:

  | Service  | Purpose | Internal Port | Public Domain / URL |
  |----------|---------|---------------|---------------------|
  | `proxy`  | TLS termination, virtual host routing | 80/443 | https://papermail.local / https://oidc.papermail.local |
  | `client` | PaperMail ASP.NET Core app | 8080 | proxied at https://papermail.local |
  | `oidc`   | OIDC provider (qlik/simple-oidc-provider) | 8080 | proxied at https://oidc.papermail.local |
  | `mail`   | SMTP/IMAP (docker-mailserver) | 25,465,587,993 | direct (no proxy) |

  ### Prerequisites

  * .NET 8 SDK
  * Docker / Docker Compose v2
  * OpenSSL (for certificate generation)
  * Host OS trust store access (to trust local root CA) 

  ### 1. Hostname Resolution
  Add entries to `/etc/hosts` (macOS/Linux):
  ```
  127.0.0.1 papermail.local oidc.papermail.local
  ```

  ### 2. Generate & Trust Certificates
  Run the provided script (creates a local root CA and wildcard cert):
  ```bash
  ./docker/scripts/generate-certificate.sh
  ```
  Files are written to `${HOME}/.aspnet/https`. To trust the root CA:
  ```bash
  # Linux (Debian/Ubuntu)
* **[Architecture](./docs/ARCHITECTURE.md)**: System design and folder structure
* **[Authentication](./docs/AUTHENTICATION.md)**: OAuth and security implementation

  # macOS (add to login keychain & trust manually or use security CLI)
  open ~/.aspnet/https/papermail-local-ca.crt
  ```
  If you cannot trust the CA, browsers will show a self‑signed certificate warning which you can bypass for development.

  ### 3. Start the Stack
  ```bash
  docker compose pull
  docker compose build client
  docker compose up -d
  docker compose ps
  ```
  Access the app at: `https://papermail.local`  (first load may prompt for cert trust)

  OIDC discovery (debug): `https://oidc.papermail.local/.well-known/openid-configuration`

  ### 4. Configured OIDC Client & Users
  Client:
  * Client Id: `papermail-web`
  * Client Secret: `papermail-secret`
  * Redirect URI: `https://papermail.local/oauth/callback`
  * Scopes: `openid profile email offline_access`

  Default OIDC users (see `USERS` env in compose):
  | sub   | email                              | password (DEFAULT_PASSWORD) |
  |-------|------------------------------------|-----------------------------|
  | admin | admin@papermail.local              | P@ssw0rd                    |
  | user  | user@papermail.local               | P@ssw0rd                    |

  Change `DEFAULT_PASSWORD` when exporting an environment variable before `docker compose up` for improved safety.

  ### 5. Mail Server OAuth2 Integration
  `docker-mailserver` is configured for OAuth introspection (`ENABLE_OAUTH2=1`). The variable `OAUTH2_INTROSPECTION_URL` points to the OIDC provider. Registered OAuth accounts: `user1@papermail.local,user2@papermail.local` via `DMS_AUTH_OAUTH2_ACCOUNTS`.

  ### 6. Adding / Managing Mailboxes
  ```bash
  docker exec -ti mail setup email add user1@papermail.local P@ssw0rd
  docker exec -ti mail setup email list
  ```

  ### 7. Sending a Test Email
  ```bash
  swaks --to user1@papermail.local \
    --from tester@papermail.local \
    --server localhost:587 \
    --header "Subject: Local Dev" \
    --body "Hello from local stack" \
    --auth-user user1@papermail.local \
    --auth-password P@ssw0rd
  ```

  ### 8. Environment Variables Summary
  Key variables (see compose file):
  * `CLIENT_DOMAIN` / `OIDC_DOMAIN` – public dev hostnames
  * `CERTIFICATE_PATH` – path for mounted TLS cert/key (defaults to `~/.aspnet/https`)
  * `DEFAULT_PASSWORD` – password injected for OIDC test users
  * `PROXY_HTTP_PORT` / `PROXY_HTTPS_PORT` – override mapped ports if needed

  ### 9. Rebuild & Restart
  ```bash
  docker compose build --no-cache client
  docker compose restart client
  ```

  ### 10. Tear Down
  ```bash
  docker compose down -v
  ```

  ### 11. Playwright E2E Tests (HTTPS)
  Install browsers after first build:
  ```bash
  dotnet build
  bash test/PaperMail.EndToEnd.Tests/bin/Debug/net8.0/playwright.sh install chromium
  ```
  Run tests (use HTTPS base URL):
  ```bash
  PAPERMAIL_BASE_URL=https://papermail.local dotnet test test/PaperMail.EndToEnd.Tests
  ```
  If you encounter certificate errors in Playwright, either trust the CA or add a context option to ignore HTTPS errors (temporary):
  ```csharp
  await playwright.Chromium.LaunchAsync(new() { Headless = true });
  // context = await browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
  ```

  ### 12. Data Protection Note
  `Microsoft.AspNetCore.DataProtection` was **downgraded to 8.0.22** due to decryption failures with 10.0.0 in tests. If upgrading again, validate `TokenStorage` round‑trip with unit tests.

  ### 13. Proxy Templates
  Reusable nginx snippets:
  * `docker/nginx/snippets/ssl.conf` – TLS config
  * `docker/nginx/snippets/proxy-headers.conf` – forwarded headers
  * `docker/nginx/snippets/healthz.conf` – `/healthz` endpoint

  Virtual host templates:
  * `docker/nginx/templates/default.conf.template` – PaperMail app
  * `docker/nginx/templates/oidc.conf.template` – OIDC provider
  * `docker/nginx/templates/redirect.conf.template` – HTTP→HTTPS redirect

  For deeper details see `docs/LOCAL_DEVELOPMENT.md` (added in this update).
* **[UI/UX Guidelines](./docs/UI_GUIDELINES.md)**: E-Ink specific design patterns
* **[API Reference](./docs/API.md)**: Internal API documentation (future)
* **[Deployment](./docs/DEPLOYMENT.md)**: Hosting and deployment guide (future)

## Getting Started

See the [Architecture](./docs/ARCHITECTURE.md) document for folder structure and development setup instructions.

## License

[To be determined]

## Contributing

[To be determined]
