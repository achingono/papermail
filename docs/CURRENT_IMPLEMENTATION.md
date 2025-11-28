# Current Implementation Status

## Project Overview

Papermail is an email client optimized for E-Ink devices, currently in active development on the `features/azure-ad` branch.

## Project Structure

The solution consists of three main projects:

### 1. Papermail.Core

**Purpose**: Domain models and core business logic

**Key Components**:
- **Entities**: `Account`, `Email`, `EmailAddress`, `Attachment`, `Provider`
- **Interfaces**: `IEntity`, `IRepository<T>`
- **Configuration**: `ImapSettings`, `SmtpSettings`
- **Validation**: `AccountValidator`, `ProviderValidator` (FluentValidation)

**Notable Design Decisions**:
- Email and Attachment are immutable value objects
- EmailAddress uses factory pattern for validation
- Entities use proper XML documentation

### 2. Papermail.Data

**Purpose**: Data access layer and business services

**Key Components**:
- **Services**:
  - `AccountService`: User account management
  - `EmailService`: Email operations orchestration
  - `ITokenService`: Token management interface

- **Repositories**:
  - `EmailRepository`: Coordinates IMAP/SMTP operations

- **Clients** (Interfaces):
  - `IImapClient`: IMAP protocol operations
  - `ISmtpClient`: SMTP protocol operations

- **Mappers**:
  - `EmailMapper`: Entity ↔ DTO transformations

- **Models** (DTOs):
  - `EmailItemModel`: List view representation
  - `EmailModel`: Detail view representation
  - `AttachmentModel`: Attachment metadata
  - `DraftModel`: Email composition

- **Extensions**:
  - `ClaimsPrincipalExtensions`: Extract user identity info

- **DataContext**: Entity Framework Core DbContext for Accounts and Providers

### 3. Papermail.Web

**Purpose**: ASP.NET Core web application (presentation layer)

**Key Components**:
- **Controllers**:
  - `AuthController`: Handles OpenID Connect authentication flow

- **Clients** (Implementations):
  - `ImapClient`: MailKit-based IMAP implementation with OAuth2
  - `SmtpClient`: MailKit-based SMTP implementation with OAuth2

- **Services**:
  - `TokenService`: Manages encrypted token storage using Data Protection

- **Security**:
  - `IPrincipalAccessor` / `PrincipalAccessor`: HttpContext user access

- **Configuration**:
  - `OpenIdSettings`: OpenID Connect configuration

- **Extensions**:
  - `IServiceCollectionExtensions`: DI setup and authentication configuration

- **Pages**:
  - `Index.cshtml`: Landing page
  - `Inbox.cshtml`: Email inbox view
  - Shared partials: `_Layout`, `_Header`, `_Aside`, `_List`

## Authentication Implementation

### Current Status: OpenID Connect with Azure AD

The application uses **OpenID Connect** for authentication via Azure AD (Microsoft Entra ID).

**Key Features**:
- OAuth2 Authorization Code flow
- Claims mapping (sub, email, name, etc.)
- Token validation and refresh
- Secure token storage with ASP.NET Core Data Protection
- Cookie-based session management

**Authentication Flow**:
1. User clicks sign-in → redirected to Azure AD
2. User authenticates and consents
3. Callback to `/auth/{scheme}` endpoint
4. Tokens exchanged and encrypted
5. Account created/updated in database
6. User redirected to inbox

**Configuration**: See `appsettings.json` → `OpenId` section

## Email Protocol Integration

### IMAP Implementation

**Library**: MailKit

**Authentication**: OAuth2 (XOAUTH2 SASL mechanism)

**Key Features**:
- Fetch emails with pagination
- Message lookup by deterministic GUID (MD5 hash of Message-ID)
- Batch FETCH operations for efficiency
- Mark as read/unread
- Delete messages
- Save drafts

**Performance Optimizations**:
- Batch header fetching (100 messages at a time)
- Only fetch full messages when needed
- Use IMAP FETCH with MessageSummaryItems.Envelope

### SMTP Implementation

**Library**: MailKit

**Authentication**: OAuth2 (XOAUTH2 SASL mechanism)

**Key Features**:
- Send emails with OAuth2
- Plain text and HTML body support
- Attachment support (via MimeMessage)

## Data Storage

### Current Implementation

**Database**: SQL Server (via Entity Framework Core)

**Entities Stored**:
- `Account`: User email accounts with OAuth tokens
- `Provider`: Email providers (Gmail, Outlook, etc.)

**Token Storage**:
- Access tokens: Stored encrypted in database
- Refresh tokens: Stored encrypted using Data Protection API
- Encryption purpose: "RefreshTokens"

### Email Storage Strategy

**Current Approach**: Direct IMAP access (no local caching)

- Emails fetched on-demand from IMAP server
- No persistent email storage
- Deterministic GUIDs for email identification

**Future Enhancement**: Local email caching for offline access

## Security Features

### Implemented

- ✅ OpenID Connect authentication
- ✅ OAuth2 token encryption (Data Protection API)
- ✅ HTTPS enforcement
- ✅ Secure cookie settings (HttpOnly, Secure, SameSite)
- ✅ Claims-based authorization
- ✅ Connection string encryption
- ✅ SQL Server with retry on failure

### Planned

- ⏳ Content Security Policy headers
- ⏳ HTML email sanitization
- ⏳ CSRF protection for forms
- ⏳ Rate limiting
- ⏳ Session timeout configuration

## Technology Stack

### Backend

- **Framework**: ASP.NET Core 8.0
- **Language**: C# 12
- **ORM**: Entity Framework Core
- **Database**: SQL Server
- **Email Protocol**: MailKit/MimeKit
- **Validation**: FluentValidation
- **Security**: ASP.NET Core Data Protection
- **Caching**: Redis (configured but not fully implemented)
- **Monitoring**: Application Insights (production only)

### Frontend

- **Rendering**: Razor Pages (server-side)
- **CSS**: Custom styles (site.css)
- **JavaScript**: Minimal (Alpine.js planned but not yet integrated)

## Configuration

### Required Settings

**Connection Strings**:
- `Sql`: SQL Server connection string
- `Redis`: Redis connection string

**OpenId Section**:
- `Authority`: Azure AD authority URL
- `ClientId`: Application (client) ID
- `ClientSecret`: Client secret
- `RequireHttpsMetadata`: true (production)
- `Scopes`: Array of requested scopes

### Environment Support

- **Development**: Detailed errors, sensitive data logging
- **Production**: Application Insights, Service Profiler
- **Testing**: Specific test environment handling

## Deployment

### Current Setup

**Platform**: Docker container

**Database Migration**: Auto-migrate on startup (non-production only)

**Health Checks**:
- Self check
- SQL Server check
- DbContext check
- Redis check

**Endpoints**:
- `/healthz`: Health check endpoint
- `/auth/{scheme}`: Authentication callback
- Pages: Mapped via Razor Pages convention

## Key Design Patterns

1. **Repository Pattern**: `IEmailRepository` abstracts email access
2. **Service Layer**: Business logic separated from data access
3. **Factory Pattern**: `EmailAddress.Create()`, `Email.Create()`
4. **Dependency Injection**: All services registered in DI container
5. **DTO Pattern**: Separate models for domain and presentation
6. **Value Objects**: Immutable Email, Attachment, EmailAddress

## Testing Status

**Current**: No automated tests implemented

**Planned**:
- Unit tests for services and mappers
- Integration tests for IMAP/SMTP clients
- E2E tests for critical user flows

## Known Limitations

1. **Email Storage**: No local caching, always fetches from server
2. **Attachments**: Download only, no preview
3. **Search**: Not implemented yet
4. **Folders**: Limited folder support
5. **Offline**: Requires internet connection
6. **Multi-Account**: Single account per user currently
7. **Conversation Threading**: Not implemented
8. **Filters/Rules**: Not implemented

## Next Steps

Based on the current implementation, suggested priorities:

### High Priority
1. Complete inbox page implementation
2. Implement email reading view
3. Add email composition functionality
4. Implement folder navigation
5. Add proper error handling and user feedback

### Medium Priority
6. Implement local email caching
7. Add attachment handling
8. Implement search functionality
9. Add email filters and rules
10. Improve performance with Redis caching

### Low Priority
11. Multi-account support
12. Conversation threading
13. Contact management
14. Advanced search
15. Email templates

## Documentation Status

All C# files now have comprehensive XML documentation including:
- Class-level summaries
- Method descriptions
- Parameter documentation
- Return value documentation
- Exception documentation where applicable

## Commit History

Latest commits on `features/azure-ad` branch:
- Added comprehensive XML documentation to all C# classes, interfaces, and methods
- (Prior commits related to Azure AD authentication implementation)
