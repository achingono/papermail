# Architecture & Folder Structure

## Overview

PaperMail follows a clean, layered architecture with clear separation of concerns, optimized for server-side rendering and maintainability.

## Architecture Principles

### 1. Layered Architecture

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│    (Razor Pages, Views, Components)     │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│         Application Layer               │
│      (Services, View Models, DTOs)      │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│         Domain Layer                    │
│    (Email Domain Logic, Interfaces)     │
└─────────────────────────────────────────┘
                   ↓
┌─────────────────────────────────────────┐
│      Infrastructure Layer               │
│  (IMAP/SMTP, OAuth, Storage, Caching)   │
└─────────────────────────────────────────┘
```

### 2. Design Patterns

- **Repository Pattern**: Abstract email storage (IMAP) access
- **Service Layer**: Business logic separation
- **Dependency Injection**: Loose coupling, testability
- **View Models**: Presentation-specific models
- **Factory Pattern**: Creating email clients per account
- **Strategy Pattern**: Different authentication strategies

### 3. Key Architectural Decisions

- **Server-Side Rendering**: All HTML generated server-side
- **Stateless Pages**: Minimal session state
- **HTTP Caching**: Aggressive caching strategy
- **Progressive Enhancement**: JavaScript as optional enhancement
- **Security First**: OAuth, HTTPS, CSP, sanitization throughout

## Source Code Structure

```
src/
├── Papermail.Web/                      # Main ASP.NET Core web application
│   ├── Pages/                          # Razor Pages (UI routes)
│   │   ├── Index.cshtml               # Landing page
│   │   ├── Index.cshtml.cs            # Landing page model
│   │   ├── Inbox.cshtml               # Inbox view
│   │   └── Shared/                    # Shared layout components
│   │       ├── _Layout.cshtml         # Main layout template
│   │       ├── _Header.cshtml         # Header partial
│   │       ├── _Aside.cshtml          # Sidebar partial
│   │       └── _List.cshtml           # Email list partial
│   │
│   ├── Controllers/                   # API Controllers
│   │   └── AuthController.cs         # Authentication controller
│
│   ├── Clients/                       # Email protocol clients
│   │   ├── ImapClient.cs             # IMAP implementation
│   │   └── SmtpClient.cs             # SMTP implementation
│   │
│   ├── wwwroot/                       # Static assets
│   │   └── css/                       # Stylesheets
│   │       └── styles.css            # Main stylesheet
│   │
│   ├── Models/                        # View models and DTOs
│   │   ├── ViewModels/               # Page-specific view models
│   │   │   ├── InboxViewModel.cs    # Inbox page data
│   │   │   ├── EmailViewModel.cs    # Single email display
│   │   │   ├── ComposeViewModel.cs  # Email composition
│   │   │   └── SettingsViewModel.cs # Settings data
│   │   └── DTOs/                     # Data transfer objects
│   │       ├── EmailListItemDto.cs  # Email list item
│   │       ├── EmailDetailDto.cs    # Full email details
│   │       └── AttachmentDto.cs     # Attachment info
│   │
│   ├── Middleware/                    # Custom middleware
│   │   ├── SecurityHeadersMiddleware.cs  # Security headers (CSP, etc.)
│   │   ├── ErrorHandlingMiddleware.cs    # Global error handling
│   │   └── PerformanceMonitoringMiddleware.cs  # Performance logging
│   │
│   ├── Filters/                       # Action filters
│   │   ├── RequireAuthenticationAttribute.cs  # Auth requirement
│   │   └── ValidateModelAttribute.cs          # Model validation
│   │
│   ├── TagHelpers/                    # Custom tag helpers
│   │   ├── EmailAddressTagHelper.cs  # Format email addresses
│   │   ├── DateTimeTagHelper.cs      # Format dates
│   │   └── FileSizeTagHelper.cs      # Format file sizes
│   │
│   ├── Extensions/                    # Extension methods
│   │   ├── ServiceCollectionExtensions.cs  # DI setup
│   │   └── StringExtensions.cs             # String utilities
│   │
│   ├── appsettings.json              # Application configuration
│   ├── appsettings.Development.json  # Development config
│   ├── Program.cs                     # Application entry point
│   └── tailwind.config.js            # TailwindCSS configuration
│
├── PaperMail.Core/                    # Core domain logic
│   ├── Entities/                      # Domain entities
│   │   ├── Email.cs                  # Email entity
│   │   ├── Attachment.cs             # Attachment entity
│   │   ├── EmailAddress.cs           # Email address value object
│   │   ├── Folder.cs                 # Folder entity
│   │   └── Account.cs                # Email account entity
│   │
│   ├── Interfaces/                    # Core interfaces
│   │   ├── IEmailRepository.cs       # Email data access
│   │   ├── IEmailService.cs          # Email operations
│   │   ├── IAuthenticationService.cs # Authentication
│   │   ├── IFolderService.cs         # Folder operations
│   │   └── ISearchService.cs         # Search functionality
│   │
│   ├── Enums/                         # Enumerations
│   │   ├── EmailProvider.cs          # Email providers (Gmail, Outlook, etc.)
│   │   ├── AuthenticationType.cs     # OAuth vs manual
│   │   └── FolderType.cs             # Standard folder types
│   │
│   └── Exceptions/                    # Custom exceptions
│       ├── EmailNotFoundException.cs
│       ├── AuthenticationException.cs
│       └── InvalidEmailException.cs
│
├── PaperMail.Application/             # Application services
│   ├── Services/                      # Business logic services
│   │   ├── EmailService.cs           # Email operations
│   │   ├── FolderService.cs          # Folder management
│   │   ├── SearchService.cs          # Email search
│   │   ├── ComposeService.cs         # Email composition
│   │   └── AttachmentService.cs      # Attachment handling
│   │
│   ├── Factories/                     # Factory classes
│   │   ├── EmailClientFactory.cs     # Create IMAP/SMTP clients
│   │   └── AuthProviderFactory.cs    # Create auth providers
│   │
│   ├── Mappers/                       # Object mapping
│   │   ├── EmailMapper.cs            # Map entities to DTOs/ViewModels
│   │   └── FolderMapper.cs           # Map folder data
│   │
│   └── Validators/                    # Business validation
│       ├── EmailValidator.cs         # Email validation rules
│       └── ComposeValidator.cs       # Composition validation
│
└── Papermail.Tests/                   # Test projects (future)
    ├── UnitTests/                     # Unit tests
    ├── IntegrationTests/              # Integration tests
    └── E2ETests/                      # End-to-end tests
```

## Layer Responsibilities

### Presentation Layer (PaperMail.Web)

**Purpose**: Handle HTTP requests, render HTML, manage user interaction

**Responsibilities**:

- Razor Pages for routing and views
- View Components for reusable UI
- Tag Helpers for custom HTML generation
- Static asset serving
- Client-side validation
- Minimal Alpine.js interactivity

**Technologies**:

- ASP.NET Core Razor Pages
- TailwindCSS
- Alpine.js
- HTML5

**Key Files**:

- `Pages/*.cshtml`: Page routes and views
- `ViewComponents/*.cs`: Reusable components
- `wwwroot/*`: Static assets

### Application Layer (PaperMail.Application)

**Purpose**: Orchestrate business logic, coordinate between layers

**Responsibilities**:

- Service implementations
- Use case orchestration
- Object mapping (entities ↔ DTOs ↔ view models)
- Business rule validation
- Factory pattern implementations

**Technologies**:

- C# / .NET 8
- Dependency Injection

**Key Files**:

- `Services/*.cs`: Business logic services
- `Mappers/*.cs`: Object transformations
- `Validators/*.cs`: Business validation

### Domain Layer (PaperMail.Core)

**Purpose**: Core business domain, framework-agnostic

**Responsibilities**:

- Domain entities
- Business rules
- Domain exceptions
- Core interfaces (contracts)
- Value objects

**Technologies**:

- C# / .NET 8 (pure domain logic, no framework dependencies)

**Key Files**:

- `Entities/*.cs`: Domain models
- `Interfaces/*.cs`: Contracts
- `Exceptions/*.cs`: Domain exceptions

### Infrastructure Layer (PaperMail.Infrastructure)

**Purpose**: External concerns, third-party integrations

**Responsibilities**:

- IMAP/SMTP client implementations (MailKit)
- OAuth provider integrations
- Token storage and encryption
- Caching implementations
- File system access
- HTML sanitization

**Technologies**:

- MailKit/MimeKit
- OAuth libraries
- ASP.NET Core Data Protection

**Key Files**:

- `Email/*.cs`: Email protocol implementations
- `Authentication/*.cs`: OAuth implementations
- `Storage/*.cs`: Data persistence

## Data Flow Examples

### Reading an Email

```
1. User clicks email in inbox
   ↓
2. InboxPage.OnGetAsync(emailId)
   ↓
3. EmailService.GetEmailByIdAsync(emailId)
   ↓
4. ImapEmailRepository.GetByIdAsync(emailId)
   ↓
5. MailKitWrapper queries IMAP server
   ↓
6. Email entity returned
   ↓
7. EmailMapper maps to EmailViewModel
   ↓
8. Razor Page renders HTML
```

### Sending an Email

```
1. User submits compose form
   ↓
2. ComposePage.OnPostAsync(model)
   ↓
3. ComposeValidator validates input
   ↓
4. ComposeService.SendEmailAsync(model)
   ↓
5. SmtpEmailSender sends via SMTP
   ↓
6. MailKitWrapper sends message
   ↓
7. Success/error returned
   ↓
8. Redirect to Sent folder
```

### OAuth Authentication

```
1. User clicks "Sign in with Google"
   ↓
2. Redirect to Google OAuth consent
   ↓
3. User authorizes
   ↓
4. Callback to /Auth/OAuth
   ↓
5. OAuthService.HandleCallback()
   ↓
6. GoogleOAuthProvider exchanges code for token
   ↓
7. TokenStorage encrypts and stores refresh token
   ↓
8. Session established
   ↓
9. Redirect to inbox
```

## Configuration Management

### appsettings.json Structure

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "...",
      "ClientSecret": "...",
      "Scopes": ["https://mail.google.com/"]
    },
    "Microsoft": {
      "ClientId": "...",
      "ClientSecret": "...",
      "Scopes": ["https://outlook.office.com/IMAP.AccessAsUser.All"]
    }
  },
  "Email": {
    "DefaultSyncDays": 30,
    "PageSize": 25,
    "MaxAttachmentSize": 25
  },
  "Security": {
    "SessionTimeout": 60,
    "RequireHttps": true,
    "EnableCSP": true
  },
  "Caching": {
    "EmailListCacheDuration": 5,
    "FolderCacheDuration": 15
  }
}
```

## Dependency Injection Setup

### Program.cs Structure

```csharp
// Add services
builder.Services.AddRazorPages();

// Core services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFolderService, FolderService>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Infrastructure
builder.Services.AddScoped<IEmailRepository, ImapEmailRepository>();
builder.Services.AddSingleton<IEmailClientFactory, EmailClientFactory>();

// Authentication
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<ITokenStorage, TokenStorage>();

// Configure OAuth providers
builder.Services.AddAuthentication()
    .AddGoogle(options => { ... })
    .AddMicrosoftAccount(options => { ... });

// Caching
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// Security
builder.Services.AddAntiforgery();
builder.Services.AddHsts(options => { ... });
```

## Development Workflow

### 1. Feature Development

1. Define interface in `PaperMail.Core/Interfaces/`
2. Implement service in `PaperMail.Application/Services/`
3. Implement infrastructure in `PaperMail.Infrastructure/`
4. Create view models in `PaperMail.Web/Models/`
5. Create Razor Page in `PaperMail.Web/Pages/`
6. Write tests in `PaperMail.Tests/`

### 2. Adding a New Page

1. Create `*.cshtml` and `*.cshtml.cs` in `Pages/`
2. Define view model if needed
3. Use existing services via DI
4. Add navigation links
5. Update site map

### 3. Adding a New Email Provider

1. Create provider class in `Infrastructure/Authentication/OAuthProviders/`
2. Implement `IOAuthProvider` interface
3. Register in `AuthProviderFactory`
4. Add configuration to `appsettings.json`
5. Update UI with provider option

## Build and Deployment

### Build Process

```bash
# Restore dependencies
dotnet restore

# Build TailwindCSS
npx tailwindcss -i ./PaperMail.Web/Styles/input.css -o ./PaperMail.Web/wwwroot/css/site.css --minify

# Build application
dotnet build --configuration Release

# Run tests
dotnet test

# Publish
dotnet publish --configuration Release --output ./publish
```

### Deployment Structure

```
publish/
├── PaperMail.Web.dll
├── appsettings.json
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── images/
└── (other dependencies)
```

## Performance Considerations

### Caching Strategy

- **Memory Cache**: Email lists, folder structures (5-15 min)
- **Session Cache**: Current user data
- **HTTP Cache**: Static assets (1 year), pages (5 min)
- **Output Cache**: Rendered pages (configurable)

### Database Future Consideration

If local caching becomes necessary:

```
PaperMail.Infrastructure/
└── Database/
    ├── Entities/          # EF Core entities
    ├── Migrations/        # Schema migrations
    └── ApplicationDbContext.cs
```

## Testing Strategy

- **Unit Tests**: Services, validators, mappers
- **Integration Tests**: IMAP/SMTP operations, OAuth flows
- **E2E Tests**: Critical user flows (login, read, compose, send)
- **Browser Tests**: Target E-Ink device browsers

## Security Layers

1. **Transport**: HTTPS only
2. **Authentication**: OAuth 2.0 / Session management
3. **Authorization**: User can only access own emails
4. **Input Validation**: Server-side validation on all inputs
5. **Output Encoding**: HTML sanitization, XSS prevention
6. **CSRF Protection**: Anti-forgery tokens
7. **Content Security Policy**: Restrict resource loading
8. **Token Encryption**: Encrypted refresh token storage

## Scalability Considerations

### Single User (Initial)

- In-memory caching
- Session state
- Direct IMAP/SMTP connections

### Multi-User (Future)

- Redis for distributed caching
- Database for user settings
- Connection pooling
- Background jobs for sync
- Load balancing

## Summary

The architecture emphasizes:

- **Simplicity**: Clear separation, easy to understand
- **Testability**: Interface-driven, dependency injection
- **Maintainability**: Organized by concern, logical grouping
- **Performance**: Caching, server-side rendering
- **Security**: Multiple layers of protection
- **Extensibility**: Easy to add providers, features

This structure supports the core goal of a simple, fast, E-Ink-optimized email client while maintaining professional software engineering practices.
