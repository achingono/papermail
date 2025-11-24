# Technology Stack

## Overview

PaperMail uses a carefully selected technology stack optimized for server-side rendering, minimal JavaScript execution, and maximum compatibility with E-Ink device browsers.

## Core Technologies

### 1. ASP.NET Core (Server-Side Framework)

**Version**: ASP.NET Core 8.0+

**Role**: Primary server-side framework for rendering HTML and handling business logic

**Rationale**:

- **Mature MVC/Razor Pages**: Proven server-side rendering capabilities
- **Performance**: Excellent performance characteristics for server-rendered applications
- **Cross-platform**: Runs on Linux, Windows, macOS
- **Built-in DI**: Dependency injection out of the box
- **Security**: Robust authentication and authorization middleware
- **OAuth Support**: Well-established OAuth 2.0 libraries
- **IMAP/SMTP Libraries**: Rich ecosystem for email protocol support

**Key Features Used**:

- **Razor Pages**: For simple, page-focused development
- **Tag Helpers**: Clean HTML generation
- **View Components**: Reusable UI components
- **Middleware Pipeline**: Request/response processing
- **Built-in Caching**: Output caching for performance
- **Session Management**: Maintaining user state

### 2. TailwindCSS (Styling Framework)

**Version**: TailwindCSS 3.x

**Role**: Utility-first CSS framework for responsive layouts

**Rationale**:

- **Small Bundle Size**: Purges unused CSS, resulting in minimal file sizes
- **No JavaScript Required**: Pure CSS solution
- **Utility-First**: Rapid development without context switching
- **Customizable**: Easy to configure for E-Ink constraints (grayscale palette)
- **Responsive Design**: Mobile-first, works well for various device sizes
- **Print-Friendly**: Can optimize for E-Ink display characteristics

**Configuration Optimizations**:

- Custom grayscale color palette
- Disabled animations and transitions
- Simplified typography scale
- High contrast defaults
- Minimal shadow and gradient utilities

### 3. HTML5 (Foundation)

**Role**: Semantic markup foundation

**Rationale**:

- **Semantic Elements**: Clear structure (`<article>`, `<nav>`, `<main>`, etc.)
- **Accessibility**: Built-in ARIA support
- **Progressive Enhancement**: Works without CSS or JavaScript
- **Universal Support**: Supported by all E-Ink device browsers
- **Form Controls**: Native form elements for better compatibility
- **No Polyfills**: Stick to well-supported features only

**Best Practices**:

- Semantic HTML5 elements
- Proper heading hierarchy
- Form labels and fieldsets
- Alternative text for images
- ARIA attributes where needed
- Valid, well-formed markup

### 4. Alpine.js (Minimal JavaScript Framework)

**Version**: Alpine.js 3.x

**Role**: Lightweight JavaScript for minimal client-side interactivity

**Rationale**:

- **Tiny Size**: ~15KB minified/gzipped
- **Declarative Syntax**: HTML-first approach, similar to Vue.js
- **No Build Step**: Works directly in the browser
- **Progressive Enhancement**: Enhances server-rendered HTML
- **Reactive**: Data binding without virtual DOM overhead
- **Perfect for Sprinkles**: Not a SPA framework, designed for enhancement

**Use Cases in PaperMail**:

- Dropdown menus and modals
- Form validation feedback
- Email composition interface enhancements
- Folder/label selection
- Read/unread toggle
- Minimal AJAX operations (e.g., mark as read)

**Usage Guidelines**:

- Use sparingly, only where absolutely necessary
- Prefer server-side rendering over client-side manipulation
- Ensure all functionality works without JavaScript (progressive enhancement)
- Keep Alpine.js directives simple and readable

## Supporting Libraries

### Email Protocol Libraries

#### MailKit

**Role**: IMAP/SMTP client library

**Rationale**:

- Industry-standard .NET library
- Full IMAP4 and SMTP support
- OAuth 2.0 authentication
- Robust and well-maintained
- Excellent documentation

#### MimeKit

**Role**: MIME message parsing and creation

**Rationale**:

- Companion to MailKit
- Complete RFC compliance
- HTML and plain text handling
- Attachment support
- Email header parsing

### Authentication Libraries

#### Microsoft.AspNetCore.Authentication.OAuth

**Role**: OAuth 2.0 authentication provider

**Rationale**:

- Built into ASP.NET Core
- Supports multiple OAuth providers
- PKCE support for enhanced security
- Token management

#### Provider-Specific Libraries:

- **Google.Apis.Auth**: Google OAuth integration
- **Microsoft.Identity.Client**: Microsoft OAuth integration

### Security Libraries

#### ASP.NET Core Data Protection

**Role**: Secure credential storage

**Rationale**:

- Encrypted token storage
- Key management
- Built-in to ASP.NET Core

### Optional Enhancement Libraries

#### Markdig

**Role**: Markdown to HTML conversion (for plain text email enhancement)

**Rationale**:

- Fast and extensible
- Useful for formatting plain text emails

#### HtmlSanitizer

**Role**: HTML email sanitization

**Rationale**:

- Prevent XSS attacks
- Clean untrusted HTML content
- Allow safe subset of HTML in emails

## Build Tools

### .NET SDK

- **Version**: .NET 8.0+
- **Purpose**: Building, testing, and running the application

### TailwindCSS CLI

- **Purpose**: CSS processing and purging
- **Integration**: npm/npx for build pipeline

### Optional Tools

- **LibMan**: Client-side library management (alternative to npm)
- **WebOptimizer**: Asset bundling and minification

## Database (Future Consideration)

While the initial version focuses on direct IMAP/SMTP access, future versions may include:

- **SQLite**: Local caching of emails and metadata
- **PostgreSQL**: Multi-user deployment option

## Deployment Targets

- **Kestrel**: Built-in ASP.NET Core web server
- **Linux**: Primary deployment target
- **Docker**: Containerized deployment
- **Cloud Platforms**: Azure, AWS, Google Cloud
- **Self-Hosted**: VPS or home server

## Browser Support Target

### Minimum Browser Versions

- **Chromium**: Version 60+ (2017)
- **WebKit**: Safari 11+ (2017)
- **Firefox**: Version 55+ (2017)

### Feature Support Requirements

- HTML5 semantic elements
- CSS Grid and Flexbox
- ES6 basics (let/const, arrow functions, promises)
- Fetch API (with fallback)

### Testing Strategy

- Test on actual E-Ink devices (Kindle, Kobo)
- Simulate older browsers using BrowserStack
- Progressive enhancement testing (disable JavaScript)
- Accessibility testing with screen readers

## Why Not React/Vue/Angular?

These frameworks were considered but rejected because:

- **Build Complexity**: Require complex build pipelines
- **Bundle Size**: Much larger JavaScript bundles
- **SPA Overhead**: Virtual DOM and reconciliation overhead
- **Server Rendering Complexity**: SSR adds significant complexity
- **Unnecessary**: Server rendering is simpler and more appropriate
- **E-Ink Performance**: Heavy JavaScript impacts E-Ink device performance

## Why Not HTMX?

While HTMX is excellent, Alpine.js was chosen because:

- **Complementary**: Alpine.js pairs better with Razor Pages
- **Local State**: Better for managing component-level state
- **Form Enhancement**: Better for complex form interactions
- **Lighter Touch**: Less opinionated about server responses

Both could potentially be used together in future iterations.

## Development Principles

1. **HTML First**: Start with semantic HTML, add CSS, then minimal JavaScript
2. **Progressive Enhancement**: Core functionality works without JavaScript
3. **Performance Budget**: Strict limits on bundle sizes
4. **Zero Dependencies**: Minimize client-side dependencies
5. **Standard Protocols**: Use standard HTTP/HTML patterns
6. **Cache Aggressively**: Leverage HTTP caching and CDNs
7. **Test on Target Devices**: Regular testing on actual E-Ink devices

## Technology Decision Checklist

Before adding any new dependency, ask:

- [ ] Does it serve a critical function?
- [ ] Is there a simpler alternative?
- [ ] What is the bundle size impact?
- [ ] Does it work on older browsers?
- [ ] Does it require a build step?
- [ ] Is it actively maintained?
- [ ] Can we implement it ourselves more simply?

## Performance Targets

- **HTML Response**: < 50KB
- **Total CSS**: < 30KB (after minification and gzip)
- **Total JavaScript**: < 20KB (after minification and gzip)
- **Page Load Time**: < 2s on 3G
- **Time to Interactive**: < 3s
- **Lighthouse Score**: > 90 for Performance, Accessibility, Best Practices
