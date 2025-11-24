# PaperMail - Email Client for E-Ink Devices

## Project Overview

PaperMail is a web-based email client specifically designed for paper tablets and E-Ink devices such as the Kindle Scribe, Kobo, and similar devices with built-in browsers. The application prioritizes simplicity, performance, and compatibility with the unique constraints of E-Ink displays and older browser engines.

## Vision

To provide a seamless, distraction-free email experience optimized for the calm, focused reading experience that E-Ink devices naturally afford, without the bloat and complexity of modern web applications.

## Target Audience

- **Primary**: Users of paper tablets with built-in browsers (Kindle Scribe, Kobo Elipsa, reMarkable, etc.)
- **Secondary**: Users seeking a minimal, low-bandwidth email interface
- **Use Cases**:
  - Reading and responding to emails on E-Ink devices
  - Low-bandwidth environments
  - Distraction-free email management
  - Accessibility-focused email access

## Core Principles

### 1. E-Ink Optimized

- Minimal screen refreshes to prevent E-Ink ghosting
- High contrast, grayscale-friendly design
- Static content generation wherever possible
- Reduced animations and transitions

### 2. Simplicity First

- Server-side rendering for maximum compatibility
- Progressive enhancement approach
- Graceful degradation for older browsers
- Minimal JavaScript execution

### 3. Performance

- Fast page loads even on slow connections
- Efficient IMAP/SMTP protocol usage
- Minimal client-side processing
- Optimized asset delivery

### 4. Privacy & Security

- OAuth 2.0 as primary authentication method
- Secure credential storage
- No tracking or analytics
- Minimal external dependencies

## Key Constraints

### E-Ink Display Limitations

- Slow refresh rates (300–1000ms)
- Limited color palette (typically grayscale)
- Variable browser engine capabilities
- Reduced JavaScript performance

### Browser Engine Constraints

- Older WebKit or Chromium versions
- Limited CSS3 support
- Reduced JavaScript API availability
- Unpredictable feature support

## Project Goals

1. **Functional Email Client**: Support core email operations (read, compose, send, organize)
2. **OAuth Integration**: Primary authentication via OAuth 2.0 for Gmail, Outlook, etc.
3. **IMAP/SMTP Support**: Standard protocol support for email operations
4. **Responsive Design**: Optimized layouts for various E-Ink device sizes
5. **Offline Capability**: Minimal offline reading support where feasible
6. **Accessibility**: WCAG 2.1 AA compliance for screen readers

## Success Metrics

- Page load time < 2 seconds on 3G connection
- Time to interactive < 3 seconds
- HTML size < 50KB per page
- CSS bundle < 30KB
- JavaScript bundle < 20KB
- Support for browsers dating back to 2015

## Documentation Structure

- **[Technology Stack](./docs/TECH_STACK.md)**: Detailed technology choices and rationale
- **[Features Specification](./docs/FEATURES.md)**: Complete feature list and requirements
- **[Architecture](./docs/ARCHITECTURE.md)**: System design and folder structure
- **[Authentication](./docs/AUTHENTICATION.md)**: OAuth and security implementation
- **[UI/UX Guidelines](./docs/UI_GUIDELINES.md)**: E-Ink specific design patterns
- **[API Reference](./docs/API.md)**: Internal API documentation (future)
- **[Deployment](./docs/DEPLOYMENT.md)**: Hosting and deployment guide (future)

## Getting Started

See the [Architecture](./docs/ARCHITECTURE.md) document for folder structure and development setup instructions.

## License

[To be determined]

## Contributing

[To be determined]
