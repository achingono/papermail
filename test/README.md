# Papermail Test Suite

This directory contains comprehensive tests for the Papermail application across multiple layers.

## Test Projects

### 1. Papermail.Core.Tests (Unit Tests)
**Coverage: 100%** | **Tests: 49**

Unit tests for core domain entities, validators, and configuration classes.

```bash
dotnet test test/Papermail.Core.Tests/
```

**Test Files:**
- Entity tests: `AccountTests.cs`, `EmailTests.cs`, `AttachmentTests.cs`, `EmailAddressTests.cs`, `ProviderTests.cs`
- Validation: `AccountValidatorTests.cs`, `ProviderValidatorTests.cs`
- Configuration: `ImapSettingsTests.cs`, `SmtpSettingsTests.cs`
- Repository: `RepositoryTests.cs`

### 2. Papermail.Data.Tests (Unit Tests)
**Coverage: 96.3%** | **Tests: 66**

Unit tests for data layer services, repositories, mappers, and extensions.

```bash
dotnet test test/Papermail.Data.Tests/
```

**Test Files:**
- Services: `EmailServiceTests.cs`, `AccountServiceTests.cs`
- Repository: `EmailRepositoryTests.cs`
- Mappers: `EmailMapperTests.cs`
- Extensions: `ClaimsPrincipalExtensionsTests.cs`

### 3. Papermail.Web.Tests (Unit Tests)
**Coverage: 7.2%*** | **Tests: 13**

Unit tests for web layer services and controllers.

```bash
dotnet test test/Papermail.Web.Tests/
```

**Test Files:**
- Services: `TokenServiceTests.cs`, `PrincipalAccessorTests.cs`
- Controllers: `AuthControllerTests.cs`

*Note: Low coverage is expected as Razor Pages and IMAP/SMTP clients require integration tests.

### 4. Papermail.Integration.Tests (Integration Tests)
**Tests: 2 test classes, 9 tests**

Integration tests that verify the application works with real dependencies using the Docker environment.

#### Running Integration Tests

**Prerequisites:**
1. Docker and Docker Compose installed
2. Docker environment running

**Start the environment:**
```bash
# From repository root
docker-compose up -d
```

**Run integration tests:**
```bash
# Enable Docker-dependent tests (use export to pass to child processes)
export RUN_DOCKER_TESTS=true

# Run all integration tests
dotnet test test/Papermail.Integration.Tests/

# Or run specific test classes
dotnet test test/Papermail.Integration.Tests/ --filter "FullyQualifiedName~DockerEnvironmentTests"
dotnet test test/Papermail.Integration.Tests/ --filter "FullyQualifiedName~EmailEndToEndTests"

# Single command (shell-specific, won't persist):
RUN_DOCKER_TESTS=true dotnet test test/Papermail.Integration.Tests/
```

**Test Classes:**
- `DockerEnvironmentTests.cs` - Docker service connectivity tests (6 tests)
- `EmailEndToEndTests.cs` - SMTP/IMAP end-to-end email flow tests (3 tests)

**What gets tested:**
- Web application accessibility and health checks
- OIDC provider configuration  
- SMTP server (port 587) send functionality
- IMAP server (port 143) receive functionality
- SQL Server connectivity
- Complete email send-and-receive workflow

**Environment Gating:**
Tests are automatically skipped when `RUN_DOCKER_TESTS` is not set to prevent failures in environments without Docker.

### 5. Papermail.UI.Tests (End-to-End UI Tests)
**Framework: Playwright + NUnit** | **Tests: 2 test classes, 10 tests**

End-to-end browser automation tests using Playwright.

#### Running UI Tests

**Prerequisites:**
1. Docker environment running (see Integration Tests section)
2. Playwright browsers installed

**Install Playwright browsers:**
```bash
pwsh bin/Debug/net8.0/playwright.ps1 install
# or
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

**Run UI tests:**
```bash
# Enable UI tests (use export to pass to child processes)
export RUN_UI_TESTS=true

# Make sure Docker environment is running
docker-compose up -d

# Wait for services to be ready
sleep 30

# Run UI tests
dotnet test test/Papermail.UI.Tests/

# Single command (shell-specific, won't persist):
RUN_UI_TESTS=true dotnet test test/Papermail.UI.Tests/
```

**Test Classes:**
- `EmailWorkflowTests.cs` - Core email workflows (inbox, compose, send, navigation)
- `AuthenticationFlowTests.cs` - Authentication and security flows

**What gets tested:**
- User login and logout flows
- Inbox page rendering and navigation
- Email composition and sending
- Multi-user simultaneous sessions
- Authentication error handling
- Session management

## Running All Tests

### Unit Tests Only (No Docker Required)
```bash
dotnet test --filter "FullyQualifiedName!~Integration.Tests&FullyQualifiedName!~UI.Tests"
```

### All Tests Including Integration and UI
```bash
# Start Docker environment
docker-compose up -d

# Wait for services to be ready
sleep 30

# Enable both test types (use export to pass to child processes)
export RUN_DOCKER_TESTS=true
export RUN_UI_TESTS=true

# Run all tests
dotnet test

# Or with single command (won't persist env vars):
RUN_DOCKER_TESTS=true RUN_UI_TESTS=true dotnet test
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator \
  -reports:"test/*/TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/report" \
  -reporttypes:"HtmlInline_AzurePipelines"

# View report
open TestResults/report/index.html
```

## Test Configuration

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `RUN_DOCKER_TESTS` | Enable integration tests requiring Docker | `false` |
| `RUN_UI_TESTS` | Enable Playwright UI tests | `false` |
| `SQL_PASSWORD` | SQL Server password for Docker | Generated |

### Docker Environment URLs

When running Docker environment tests:
- Web App: `https://papermail.local`
- OIDC Provider: `https://oidc.papermail.local`
- SMTP: `localhost:587`
- IMAP: `localhost:143`
- SQL Server: `localhost:1433`

### Test Credentials

Default test users (configured in docker-compose.yml):

**Admin User:**
- Email: `admin@papermail.local`
- Password: `P@ssw0rd`

**Regular User:**
- Email: `user@papermail.local`
- Password: `P@ssw0rd`

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run Unit Tests
        run: dotnet test --filter "FullyQualifiedName!~Integration.Tests&FullyQualifiedName!~UI.Tests" --collect:"XPlat Code Coverage"
      - name: Upload Coverage
        uses: codecov/codecov-action@v3

  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Start Docker Environment
        run: docker-compose up -d
      - name: Wait for Services
        run: sleep 30
      - name: Run Integration Tests
        env:
          RUN_DOCKER_TESTS: true
        run: dotnet test test/Papermail.Integration.Tests/

  ui-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Install Playwright
        run: pwsh test/Papermail.UI.Tests/bin/Debug/net8.0/playwright.ps1 install --with-deps
      - name: Start Docker Environment
        run: docker-compose up -d
      - name: Wait for Services
        run: sleep 30
      - name: Run UI Tests
        env:
          RUN_UI_TESTS: true
        run: dotnet test test/Papermail.UI.Tests/
```

## Troubleshooting

### Integration Tests Failing

1. **Check Docker is running:**
   ```bash
   docker ps
   ```

2. **Check all services are healthy:**
   ```bash
   docker-compose ps
   ```

3. **Check service logs:**
   ```bash
   docker-compose logs web
   docker-compose logs mail
   docker-compose logs oidc
   ```

4. **Restart environment:**
   ```bash
   docker-compose down
   docker-compose up -d
   ```

### UI Tests Failing

1. **SSL Certificate Issues:**
   - Ensure certificates are generated: `./docker/scripts/generate-certificate.sh`
   - Check certificate location matches docker-compose.yml

2. **Playwright Browser Issues:**
   ```bash
   playwright install --with-deps
   ```

3. **Timeout Issues:**
   - Increase wait times in tests
   - Check service logs for errors
   - Verify network connectivity

### Coverage Reports Not Generating

```bash
# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Verify it's in PATH
reportgenerator --version
```

## Best Practices

1. **Unit Tests**: Should run fast and without external dependencies
2. **Integration Tests**: Gate with environment variable to avoid CI failures
3. **UI Tests**: Run in headless mode for CI, headed for debugging
4. **Test Isolation**: Each test should be independent and clean up after itself
5. **Test Data**: Use unique identifiers (GUIDs) to avoid conflicts
6. **Async/Await**: Always await async operations properly
7. **Assertions**: Use descriptive assertion messages

## Coverage Goals

- **Core Layer**: ≥ 80% (Currently: 100% ✅)
- **Data Layer**: ≥ 80% (Currently: 96.3% ✅)
- **Web Layer**: ≥ 50% for unit-testable components
- **Overall**: ≥ 70% line coverage

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [ASP.NET Core Integration Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Coverlet Coverage](https://github.com/coverlet-coverage/coverlet)

- DTO mapping validation
- Mocked dependencies using Moq

**Run tests:**
```bash
dotnet test test/Papermail.Data.Tests/Papermail.Data.Tests.csproj
```

### 3. Papermail.Web.Tests
Unit tests for the Web layer including:
- Razor Page models (Inbox, Sent, Drafts, Compose)
- Controller tests
- Authentication and authorization
- Form validation

**Run tests:**
```bash
dotnet test test/Papermail.Web.Tests/Papermail.Web.Tests.csproj
```

### 4. Papermail.Integration.Tests
Integration tests using Testcontainers:
- IMAP/SMTP email operations with real docker-mailserver
- OAuth2 authentication flows
- End-to-end email sending and receiving
- Database integration tests

**Prerequisites:**
- Docker must be running
- Sufficient resources for testcontainers

**Run tests:**
```bash
dotnet test test/Papermail.Integration.Tests/Papermail.Integration.Tests.csproj
```

### 5. Papermail.UI.Tests
End-to-end UI tests using Playwright:
- Login flow testing
- Inbox navigation
- Email composition and sending
- Folder navigation (Inbox, Sent, Drafts)
- Form validation

**Prerequisites:**
- Install Playwright browsers:
```bash
cd test/Papermail.UI.Tests
pwsh bin/Debug/net8.0/playwright.ps1 install
# or on Linux/Mac:
playwright install
```

- Application must be running at https://papermail.local

**Run tests:**
```bash
dotnet test test/Papermail.UI.Tests/Papermail.UI.Tests.csproj
```

## Running All Tests

To run all test projects:
```bash
dotnet test
```

To run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Organization

Tests follow the Arrange-Act-Assert (AAA) pattern:
- **Arrange**: Set up test data and dependencies
- **Act**: Execute the code being tested
- **Assert**: Verify the expected outcome

## Continuous Integration

Tests are designed to run in CI/CD pipelines. For GitHub Actions or Azure Pipelines:

```yaml
- name: Run Tests
  run: dotnet test --configuration Release --logger trx --results-directory TestResults
  
- name: Publish Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults
```

## Writing New Tests

### Unit Tests
- Use xUnit attributes: `[Fact]`, `[Theory]`, `[InlineData]`
- Mock dependencies using Moq
- Test one logical unit per test method
- Use descriptive test names: `MethodName_Scenario_ExpectedOutcome`

### Integration Tests
- Use Testcontainers for dependencies
- Clean up resources in test cleanup/disposal
- Test real integrations, not mocked

### UI Tests
- Use Playwright's built-in waits and assertions
- Test user workflows, not implementation details
- Keep tests independent and idempotent

## Code Coverage Goals

- **Unit Tests**: Aim for >80% coverage
- **Integration Tests**: Cover critical paths
- **UI Tests**: Cover main user workflows

## Troubleshooting

### Common Issues

**Playwright tests fail:**
- Ensure browsers are installed: `playwright install`
- Check that app is running at https://papermail.local
- Verify SSL certificate is trusted

**Integration tests timeout:**
- Ensure Docker is running
- Check Docker has sufficient resources
- Verify network connectivity

**Unit tests fail:**
- Check for missing dependencies: `dotnet restore`
- Verify project references are correct

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
