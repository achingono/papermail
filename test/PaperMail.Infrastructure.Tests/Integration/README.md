# Integration Tests

This directory contains integration tests that use the actual mail server from `docker-compose.yml`.

## Overview

The integration tests verify MailKitWrapper and SmtpWrapper against a real IMAP/SMTP server, testing:

- **IMAP Operations**: Fetch emails, save drafts, mark as read, delete
- **SMTP Operations**: Send emails with various configurations
- **Authentication**: Password-based authentication with fallback from OAuth
- **Error Handling**: Invalid credentials, missing emails

## Architecture

### DockerComposeFixture

The `DockerComposeFixture` manages the lifecycle of the mail server:

1. Starts the `mail` service from docker-compose before tests run
2. Waits for the mail server to be ready (max 30 seconds)
3. Provides connection details to tests
4. Stops and cleans up containers after tests complete

### Test Collections

Tests are organized in the `DockerCompose` collection to share the fixture across all integration tests, ensuring the mail server starts once for all tests.

## Running Integration Tests

### Prerequisites

- Docker and Docker Compose installed
- Generated SSL certificates (see root README.md)

### Run All Tests (Including Integration)

```bash
# From repository root
dotnet test
```

Integration tests are **skipped by default** to avoid requiring docker-compose for regular test runs.

### Run Integration Tests Only

To enable integration tests, remove the `Skip` attribute or use test filtering:

```bash
# Run all tests in the Integration namespace
dotnet test --filter "FullyQualifiedName~Integration"
```

### Manual Setup

If you want to run integration tests manually:

1. Start the mail server:
   ```bash
   docker compose up -d mail
   ```

2. Wait for the server to be ready (~10-30 seconds)

3. Remove `Skip` attributes from test methods in:
   - `Integration/MailKitWrapperIntegrationTests.cs`
   - `Integration/SmtpWrapperIntegrationTests.cs`

4. Run tests:
   ```bash
   dotnet test
   ```

5. Clean up:
   ```bash
   docker compose down -v
   ```

## Configuration

The integration tests use these settings from docker-compose:

- **IMAP Host**: localhost
- **IMAP Port**: 143 (plain IMAP, no SSL)
- **SMTP Host**: localhost
- **SMTP Port**: 587 (STARTTLS)
- **Test User**: admin@papermail.local
- **Test Password**: P@ssw0rd

## Test Coverage

Integration tests cover scenarios that are difficult to mock:

- Real IMAP folder operations
- Real SMTP email sending
- Actual authentication mechanisms
- Network connectivity issues
- Mail server response handling

These tests complement the unit tests by verifying the integration with actual mail protocols.

## Troubleshooting

### Mail server not starting

Check docker logs:
```bash
docker compose logs mail
```

### Connection refused errors

Ensure the mail server is fully started (it can take 10-30 seconds after container starts).

### Certificate errors

Integration tests use self-signed certificates. The tests are configured to accept these in development mode.
