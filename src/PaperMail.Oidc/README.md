# PaperMail OIDC Provider

A custom OpenID Connect (OIDC) provider for PaperMail, built with `oidc-provider`.

## Features

- Full OAuth 2.0 and OpenID Connect support
- Token introspection endpoint for mail server authentication
- Configurable via environment variables
- Simple built-in login interface
- Support for authorization code flow with PKCE
- Refresh token support

## Environment Variables

### Required Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `PORT` | Server port | `8080` |
| `ISSUER` | OIDC issuer URL (internal) | `http://localhost:8080` |
| `EXTERNAL_HOST_NAME` | External hostname for display | `oidc.papermail.local` |

### Client Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `CLIENT_ID` | OAuth client ID | `a92e2069-b509-450b-a9e8-d73cead8735a` |
| `CLIENT_SECRET` | OAuth client secret | `YTkyZTIwNjktYjUwOS00NTBiLWE5ZTgtZDczY2VhZDg3MzVh` |
| `REDIRECT_URIS` | Comma-separated redirect URIs | `https://papermail.local/oauth/callback` |
| `POST_LOGOUT_REDIRECT_URIS` | Comma-separated post-logout redirect URIs | `https://papermail.local/auth/logout/callback` |

### Security

| Variable | Description | Default |
|----------|-------------|---------|
| `COOKIE_KEYS` | Comma-separated cookie signing keys | Random key |

### User Management

| Variable | Description | Default |
|----------|-------------|---------|
| `USERS` | JSON array of user objects (see below) | Default admin and user accounts |

## User Configuration

Users can be configured via the `USERS` environment variable. Each user should have:

```json
{
  "id": "unique-user-id",
  "email": "user@example.com",
  "email_verified": true,
  "name": "Full Name",
  "nickname": "username",
  "given_name": "First",
  "family_name": "Last",
  "password": "plaintext-password",
  "groups": ["group1", "group2"]
}
```

### Default Users

1. **Admin Account**
   - Email: `admin@papermail.local`
   - Password: `P@ssw0rd`
   - Groups: Everyone, Administrators

2. **User Account**
   - Email: `user@papermail.local`
   - Password: `P@ssw0rd`
   - Groups: Everyone, Users

## Endpoints

- `GET /.well-known/openid-configuration` - OIDC discovery
- `GET /auth` - Authorization endpoint
- `POST /token` - Token endpoint
- `POST /token/introspection` - Token introspection
- `POST /token/revocation` - Token revocation
- `GET /interaction/:uid` - Login page
- `POST /interaction/:uid` - Login form submission

## Development

**Requirements:** Node.js 22 or later (required by oidc-provider v9)

```bash
# Install dependencies
npm install

# Run in development mode
npm run dev

# Start production server
npm start
```

## Docker

```bash
# Build image
docker build -t papermail-oidc .

# Run container
docker run -p 8080:8080 \
  -e CLIENT_ID=your-client-id \
  -e CLIENT_SECRET=your-client-secret \
  -e REDIRECT_URIS=https://yourapp.com/callback \
  papermail-oidc
```

## Token TTLs

- Access Token: 1 hour
- ID Token: 1 hour
- Authorization Code: 10 minutes
- Refresh Token: 30 days

## Security Notes

⚠️ **For Development Only**: This implementation includes:
- Plaintext password storage
- Simple built-in authentication
- No rate limiting
- Basic session management

For production use, consider:
- Using a proper user database
- Implementing bcrypt/argon2 password hashing
- Adding rate limiting and brute force protection
- Using Redis for session storage
- Implementing account recovery flows
- Adding multi-factor authentication
