# Docker Environment UI Tests

This directory contains end-to-end UI tests that validate the complete docker-hosted PaperMail environment.

## Overview

These tests validate the full stack running in docker-compose:
- **Nginx Proxy**: HTTPS reverse proxy (ports 80/443)
- **PaperMail Web Client**: ASP.NET Core application
- **OIDC Provider**: Authentication service (simple-oidc-provider)
- **Mail Server**: docker-mailserver with IMAP/SMTP

## Test Categories

### DockerEnvironmentTests
Validates that all docker services are properly configured and accessible:
- Proxy service accessibility
- OIDC service configuration
- Client service routing
- Mail server ports (SMTP 587, IMAP 993)
- Service health checks
- OIDC discovery endpoints

### AuthenticationFlowTests
Tests OAuth 2.0 authentication flow with the OIDC provider:
- Login with valid credentials (admin and regular user)
- Login with invalid credentials (error handling)
- OAuth callback handling
- Logout functionality
- Unauthenticated access redirection

### EmailWorkflowUITests
End-to-end tests for email operations through the web UI:
- Compose and send email
- Load and display inbox
- Form validation
- Keyboard shortcuts (e.g., 'c' for compose)
- Multiple recipients
- Navigation between pages

## Prerequisites

### Required Software
- Docker and Docker Compose
- .NET 8.0 SDK
- Playwright browsers installed

### Install Playwright
```bash
pwsh bin/Debug/net8.0/playwright.ps1 install
```

### SSL Certificates
The tests expect SSL certificates at:
- `~/.aspnet/https/papermail.local.crt`
- `~/.aspnet/https/papermail.local.key`

Generate certificates using:
```bash
cd docker/scripts
./generate-certificate.sh
```

### Hosts File Configuration
Add these entries to `/etc/hosts`:
```
127.0.0.1 papermail.local
127.0.0.1 oidc.papermail.local
127.0.0.1 mail.papermail.local
```

## Running the Tests

### Option 1: Using the Automation Script
The easiest way to run the tests:

```bash
cd test/PaperMail.EndToEnd.Tests/Docker
./run-ui-tests.sh
```

The script will:
1. Build all docker images
2. Start all services
3. Wait for services to be ready
4. Run the UI tests
5. Clean up containers

### Option 2: Manual Execution

#### 1. Start Docker Environment
```bash
cd /path/to/papermail
docker compose up -d
```

#### 2. Wait for Services to Start
Wait 30-60 seconds for all services to be ready, or check logs:
```bash
docker compose logs -f
```

#### 3. Remove Skip Attributes
Edit the test files and remove the `Skip` parameter from the `[Fact]` attributes:
```csharp
// Before
[Fact(Skip = "Requires docker-compose environment")]

// After
[Fact]
```

#### 4. Run Tests
```bash
cd test/PaperMail.EndToEnd.Tests
dotnet test --filter "FullyQualifiedName~Docker"
```

#### 5. Stop Docker Environment
```bash
docker compose down -v
```

## Test Configuration

### Docker Environment
The `DockerEnvironmentFixture` automatically:
- Builds docker images with latest code
- Starts all services (proxy, client, mail, oidc)
- Waits for services to be ready (HTTP health checks, TCP port checks)
- Provides test credentials and URLs
- Cleans up containers on disposal

### URLs
- **Application**: https://papermail.local
- **OIDC Provider**: https://oidc.papermail.local

### Test Users
From `docker/oidc/users.json`:
- **Admin**: admin@papermail.local / P@ssw0rd
- **User**: user@papermail.local / P@ssw0rd

### Mail Server
- **SMTP**: localhost:587
- **IMAP**: localhost:993
- **Credentials**: Same as OIDC users

## Troubleshooting

### Tests Timeout During Service Startup
Increase timeout in `DockerEnvironmentFixture.WaitForServicesAsync()`:
```csharp
WaitForHttpServiceAsync(BaseUrl, "PaperMail Web Application", timeoutSeconds: 120)
```

### Certificate Errors
Ensure certificates exist and are readable:
```bash
ls -la ~/.aspnet/https/papermail.local.*
```

### OIDC Login Form Not Found
The OIDC provider UI might have different selectors. Update `AuthenticationFlowTests.LoginAsync()` with correct selectors:
```csharp
await page.FillAsync("input[name=username]", email);  // Adjust selector
```

### Ports Already in Use
Stop conflicting services or change ports in `docker-compose.yml`:
```yaml
ports:
  - "8080:80"   # Change from 80 to 8080
  - "8443:443"  # Change from 443 to 8443
```

### Docker Build Failures
Check docker build logs:
```bash
docker compose build --progress=plain
```

### Playwright Browser Not Installed
Install Playwright browsers:
```bash
cd test/PaperMail.EndToEnd.Tests
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

## CI/CD Integration

To run in CI/CD pipeline:

1. **Setup Phase**:
   ```yaml
   - name: Generate certificates
     run: cd docker/scripts && ./generate-certificate.sh
   
   - name: Configure hosts
     run: |
       echo "127.0.0.1 papermail.local" | sudo tee -a /etc/hosts
       echo "127.0.0.1 oidc.papermail.local" | sudo tee -a /etc/hosts
   ```

2. **Test Phase**:
   ```yaml
   - name: Run UI tests
     run: cd test/PaperMail.EndToEnd.Tests/Docker && ./run-ui-tests.sh
   ```

3. **Cleanup**:
   ```yaml
   - name: Cleanup
     if: always()
     run: docker compose down -v
   ```

## Test Output

Successful test run output:
```
Starting docker-compose environment for UI tests...
docker compose build completed successfully
docker compose up -d completed successfully
Waiting for PaperMail Web Application at https://papermail.local...
PaperMail Web Application is ready
Waiting for OIDC Provider at https://oidc.papermail.local...
OIDC Provider is ready
Waiting for SMTP Server on localhost:587...
SMTP Server is ready
Waiting for IMAP Server on localhost:993...
IMAP Server is ready
Docker environment ready for UI tests

Starting test execution...
Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20
```

## Contributing

When adding new UI tests:
1. Add tests to appropriate test class or create new class
2. Use `[Fact(Skip = "Requires docker-compose environment")]` attribute
3. Inherit test class from `DockerEnvironmentCollection`
4. Use fixture credentials and URLs
5. Handle HTTPS with `IgnoreHTTPSErrors = true`
6. Add appropriate waits for dynamic content
7. Update this README with new test descriptions

