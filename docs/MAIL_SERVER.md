# Mail Server Configuration

## Overview

PaperMail can integrate with self-hosted mail servers for development and testing. This document covers the configuration and troubleshooting of mail server OAuth2 authentication.

## Docker Mail Server Setup

### Components

- **docker-mailserver**: Complete mail server solution (Postfix + Dovecot)
- **Dovecot**: IMAP server with OAuth2 support via `auth-oauth2` passdb
- **Postfix**: SMTP server with SASL authentication
- **OIDC Provider**: OAuth2/OIDC provider for token issuance and introspection

### Basic Configuration

```yaml
services:
  mail:
    image: docker.io/mailserver/docker-mailserver:latest
    hostname: mail
    domainname: papermail.local
    environment:
      - PERMIT_DOCKER=network
      - SSL_TYPE=self-signed
      - ENABLE_OAUTH2=1
      - OAUTH2_INTROSPECTION_URL=http://oidc:8080/token/introspection
      - OAUTH2_INTROSPECTION_MODE=post
      - OAUTH2_CLIENT_ID=your-client-id
      - OAUTH2_CLIENT_SECRET=your-client-secret
      - OAUTH2_USERNAME_ATTRIBUTE=email
    ports:
      - "25:25"    # SMTP
      - "587:587"  # Submission (STARTTLS)
      - "993:993"  # IMAPS
      - "143:143"  # IMAP
    volumes:
      - maildata:/var/mail
      - mailstate:/var/mail-state
      - ./docker/mailserver/config/:/tmp/docker-mailserver/
    networks:
      papermail-net:
        aliases:
          - mail.papermail.local
```

## STARTTLS Configuration

### Certificate Setup

For development with self-signed certificates:

1. **Generate Certificate**:
```bash
openssl req -x509 -nodes -days 365 -newkey rsa:4096 \
  -keyout papermail.local.key \
  -out papermail.local.crt \
  -subj "/CN=*.papermail.local" \
  -addext "subjectAltName=DNS:papermail.local,DNS:*.papermail.local"
```

2. **Mount in Docker**:
```yaml
volumes:
  - ./certs/papermail.local.crt:/tmp/docker-mailserver/ssl/mail.papermail.local-cert.pem:ro
  - ./certs/papermail.local.key:/tmp/docker-mailserver/ssl/mail.papermail.local-key.pem:ro
```

3. **Trust in Application**:
```yaml
# In client service
volumes:
  - ./certs/papermail.local.crt:/usr/local/share/ca-certificates/papermail.local.crt:ro
entrypoint: ["sh", "-c", "update-ca-certificates && dotnet PaperMail.Web.dll"]
```

### SMTP Ports

- **Port 25**: Unencrypted SMTP (for server-to-server)
- **Port 587**: SMTP with STARTTLS (recommended for clients)
- **Port 465**: SMTPS (deprecated, use 587)

### Environment-Aware Certificate Validation

```csharp
public class SmtpWrapper
{
    private readonly IHostEnvironment _environment;
    private readonly bool _isDevelopment;
    
    public SmtpWrapper(SmtpSettings settings, IHostEnvironment environment)
    {
        _settings = settings;
        _environment = environment;
        _isDevelopment = environment.IsDevelopment();
    }
    
    public async Task SendEmailAsync(MimeMessage message)
    {
        using var client = new SmtpClient();
        
        // Only bypass certificate validation in development
        if (_isDevelopment)
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
        
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        // ... rest of implementation
    }
}
```

**Production**: Remove the certificate validation bypass and use CA-signed certificates.

## OAuth2 Authentication Flow

### 1. XOAUTH2 SASL Mechanism

When a client connects to IMAP/SMTP with XOAUTH2:

```
Client → Mail Server: AUTH XOAUTH2
Client → Mail Server: base64(user=user@example.com^Aauth=Bearer <access_token>^A^A)
Mail Server → Dovecot: Validate token via OAuth2 passdb
Dovecot → OIDC Provider: POST /token/introspection
```

### 2. Token Introspection

Dovecot sends introspection request:

```http
POST /token/introspection HTTP/1.1
Host: oidc:8080
Content-Type: application/x-www-form-urlencoded

token=<access_token>&client_id=<client_id>&client_secret=<client_secret>
```

### 3. Expected Response

**RFC-Compliant Response**:
```json
{
  "active": true,
  "token_type": "Bearer",
  "sub": "945dc90b-02ac-43e5-a4ab-2db744dce149",
  "email": "user@example.com",
  "client_id": "a92e2069-b509-450b-a9e8-d73cead8735a",
  "exp": 1764087384,
  "iat": 1764083784,
  "iss": "https://oidc.example.com",
  "scope": "openid profile email"
}
```

**Critical Fields**:
- `active`: Must be `true`
- `token_type`: **Must be `"Bearer"`** (not `"access_token"`)
- `sub` or `email`: User identifier (depends on `username_attribute`)

### 4. Dovecot Validation

Dovecot validates:
1. HTTP 200 response
2. `active` field is `true`
3. `token_type` field is `"Bearer"`
4. Username field (`email` or `sub`) exists and matches

If validation fails, authentication is rejected.

## Dovecot OAuth2 Configuration

### Environment Variable Method

Docker-mailserver supports `OAUTH2_*` environment variables that are automatically mapped to `dovecot-oauth2.conf.ext`:

```bash
OAUTH2_INTROSPECTION_URL → introspection_url
OAUTH2_INTROSPECTION_MODE → introspection_mode
OAUTH2_CLIENT_ID → client_id
OAUTH2_CLIENT_SECRET → client_secret
OAUTH2_USERNAME_ATTRIBUTE → username_attribute
```

### Manual Configuration File

Create `docker/mailserver/config/dovecot-oauth2.conf.ext`:

```conf
introspection_url = http://oidc:8080/token/introspection
introspection_mode = post
client_id = a92e2069-b509-450b-a9e8-d73cead8735a
client_secret = YTkyZTIwNjktYjUwOS00NTBiLWE5ZTgtZDczY2VhZDg3MzVh
username_attribute = email
```

Mount this file or copy it to the container's `/etc/dovecot/dovecot-oauth2.conf.ext`.

### Introspection Modes

- `auth`: GET request with Bearer authentication header
- `get`: GET request with token in query string
- `post`: POST request with token in form body (recommended)
- `local`: Local validation without external call

### Username Attribute

- `email`: Use the `email` field from introspection response
- `sub`: Use the `sub` (subject) field
- Custom field name from provider's response

## Troubleshooting

### Enable Debug Logging

1. **Add to dovecot-oauth2.conf.ext**:
```conf
debug = yes
rawlog_dir = /tmp/oauth2
```

2. **Create log directory**:
```bash
docker exec mail mkdir -p /tmp/oauth2
docker exec mail chmod 777 /tmp/oauth2
```

3. **Reload Dovecot**:
```bash
docker exec mail dovecot reload
```

4. **Check raw logs**:
```bash
docker exec mail find /tmp/oauth2 -type f -exec cat {} \;
```

### Common Errors

#### 1. "Expected Bearer token, got 'access_token'"

**Cause**: OIDC provider returns `"token_type": "access_token"` instead of `"Bearer"`

**Solution**:
- Use a production-grade OIDC provider (Keycloak, Auth0, Okta)
- OR implement an introspection proxy to normalize responses
- OR keep password authentication as fallback

#### 2. "Introspection failed: No username returned"

**Cause**: The field specified in `username_attribute` doesn't exist in response

**Solution**:
- Check introspection response JSON
- Change `username_attribute` to match available field (`sub`, `email`, etc.)
- Ensure OIDC provider includes user claims in introspection

#### 3. "405 Method Not Allowed"

**Cause**: `introspection_mode` set to wrong HTTP method

**Solution**:
- Change to `introspection_mode = post`
- Verify OIDC provider supports POST to introspection endpoint

#### 4. "400 Bad Request"

**Cause**: Missing or invalid client credentials

**Solution**:
- Verify `client_id` and `client_secret` are correct
- Ensure provider requires client authentication for introspection
- Check request format matches provider expectations

### Inspect Mail Server Logs

```bash
# OAuth2-specific errors
docker compose logs mail | grep -i oauth2

# SASL authentication attempts
docker compose logs mail | grep SASL

# General authentication errors
docker compose logs mail | grep "auth:"
```

### Test Introspection Manually

```bash
# Get an access token from your OIDC provider
ACCESS_TOKEN="your-access-token-here"

# Test introspection endpoint
curl -X POST http://localhost:8080/token/introspection \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "token=$ACCESS_TOKEN&client_id=your-client-id&client_secret=your-client-secret"
```

Verify the response contains:
- `"active": true`
- `"token_type": "Bearer"`
- User identifier field (`email` or `sub`)

## OIDC Provider Compatibility

### Compatible Providers

✅ **Keycloak**
- Full RFC 7662 compliance
- Returns `token_type: "Bearer"`
- Configurable introspection endpoint

✅ **Auth0**
- Supports OAuth2 introspection
- Returns standard response format

✅ **Okta**
- Enterprise-grade OIDC/OAuth2
- Full introspection support

✅ **Azure AD B2C**
- Microsoft identity platform
- OAuth2 compliant

### Incompatible Providers

❌ **qlik/simple-oidc-provider**
- Returns `token_type: "access_token"` (non-standard)
- Suitable for development/testing only
- Requires password fallback for actual mail authentication

### Migration Path

To move from development to production:

1. Replace `qlik/simple-oidc-provider` with Keycloak or Auth0
2. Update `OAUTH2_INTROSPECTION_URL` to production endpoint
3. Configure production OIDC provider with proper scopes
4. Test introspection response format
5. Remove password authentication fallback (optional)

## Network Configuration

### Docker Network Resolution

Use network aliases for FQDN resolution matching certificates:

```yaml
networks:
  papermail-net:
    driver: bridge

services:
  mail:
    networks:
      papermail-net:
        aliases:
          - mail.papermail.local
```

Application can now use `mail.papermail.local` which matches the certificate SAN.

### DNS vs Host Resolution

❌ **Avoid extra_hosts** (static, doesn't update on container restart):
```yaml
extra_hosts:
  - "mail.papermail.local:172.19.0.2"
```

✅ **Use network aliases** (dynamic, DNS-based):
```yaml
networks:
  papermail-net:
    aliases:
      - mail.papermail.local
```

## Security Considerations

### Development

- Self-signed certificates acceptable
- Certificate validation bypass in code (environment-aware)
- Local OIDC provider
- Relaxed security headers

### Production

- CA-signed TLS certificates
- No certificate validation bypass
- Production OIDC provider (Keycloak, Auth0, etc.)
- HSTS enforcement
- Firewall rules restricting mail server access
- Regular security updates

## Example: Complete Working Configuration

```yaml
services:
  mail:
    hostname: mail
    domainname: papermail.local
    image: docker.io/mailserver/docker-mailserver:latest
    environment:
      - PERMIT_DOCKER=network
      - SSL_TYPE=self-signed
      - ENABLE_OAUTH2=1
      - OAUTH2_INTROSPECTION_URL=https://keycloak.example.com/realms/myrealm/protocol/openid-connect/token/introspect
      - OAUTH2_INTROSPECTION_MODE=post
      - OAUTH2_CLIENT_ID=papermail-client
      - OAUTH2_CLIENT_SECRET=secret-key-here
      - OAUTH2_USERNAME_ATTRIBUTE=email
    ports:
      - "587:587"
      - "993:993"
    volumes:
      - maildata:/var/mail
      - ./certs/server.crt:/tmp/docker-mailserver/ssl/mail.papermail.local-cert.pem:ro
      - ./certs/server.key:/tmp/docker-mailserver/ssl/mail.papermail.local-key.pem:ro
    networks:
      papermail-net:
        aliases:
          - mail.papermail.local

  client:
    build: .
    environment:
      - SMTP__HOST=mail.papermail.local
      - SMTP__PORT=587
      - SMTP__USETLS=true
      - SMTP__USERNAME=user@papermail.local
      - IMAP__HOST=mail.papermail.local
      - IMAP__PORT=993
      - IMAP__USESSL=true
    volumes:
      - ./certs/server.crt:/usr/local/share/ca-certificates/papermail.local.crt:ro
    entrypoint: ["sh", "-c", "update-ca-certificates && dotnet PaperMail.Web.dll"]
    networks:
      - papermail-net
```

## Further Reading

- [Dovecot OAuth2 Documentation](https://doc.dovecot.org/configuration_manual/authentication/oauth2/)
- [RFC 7662: OAuth 2.0 Token Introspection](https://datatracker.ietf.org/doc/html/rfc7662)
- [RFC 7628: SASL OAuth Bearer Mechanism](https://datatracker.ietf.org/doc/html/rfc7628)
- [docker-mailserver Documentation](https://docker-mailserver.github.io/docker-mailserver/)
