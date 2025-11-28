# PaperMail Test Suite

This directory contains comprehensive tests for the PaperMail application, including unit tests, integration tests, and UI tests.

## Test Projects

### 1. Papermail.Core.Tests
Unit tests for the Core layer including:
- Entity validation (AccountValidator, ProviderValidator)
- Domain model tests
- Business rule validation

**Run tests:**
```bash
dotnet test test/Papermail.Core.Tests/Papermail.Core.Tests.csproj
```

### 2. Papermail.Data.Tests
Unit tests for the Data layer including:
- Service layer tests (EmailService, AccountService, TokenService)
- Repository pattern tests
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
