import Provider from 'oidc-provider';
import http from 'http';

// Environment variable configuration
const PORT = process.env.PORT || 8080;
const ISSUER = process.env.ISSUER || `http://localhost:${PORT}`;
const EXTERNAL_HOST_NAME = process.env.EXTERNAL_HOST_NAME || 'oidc.papermail.local';

// Client configuration from environment variables
const CLIENT_ID = process.env.CLIENT_ID || 'a92e2069-b509-450b-a9e8-d73cead8735a';
const CLIENT_SECRET = process.env.CLIENT_SECRET || 'YTkyZTIwNjktYjUwOS00NTBiLWE5ZTgtZDczY2VhZDg3MzVh';
const REDIRECT_URIS = process.env.REDIRECT_URIS 
  ? process.env.REDIRECT_URIS.split(',') 
  : ['https://papermail.local/oauth/callback'];
const POST_LOGOUT_REDIRECT_URIS = process.env.POST_LOGOUT_REDIRECT_URIS
  ? process.env.POST_LOGOUT_REDIRECT_URIS.split(',')
  : ['https://papermail.local/auth/logout/callback'];

// Cookie keys for session security
const COOKIE_KEYS = process.env.COOKIE_KEYS
  ? process.env.COOKIE_KEYS.split(',')
  : ['59761AB67FB5BA23BDD917B2F2E409394A848F50FA9CEB08860C73495339266E'];

// Parse users from environment variable or use defaults
const USERS = process.env.USERS ? JSON.parse(process.env.USERS) : [
  {
    id: '945dc90b-02ac-43e5-a4ab-2db744dce149',
    email: 'admin@papermail.local',
    email_verified: true,
    name: 'Administrator Account',
    nickname: 'admin',
    given_name: 'Administrator',
    family_name: 'Account',
    password: 'P@ssw0rd',
    groups: ['Everyone', 'Administrators']
  },
  {
    id: 'fa6c7a8c-05f2-4041-96c4-3dbad71a3fd2',
    email: 'user@papermail.local',
    email_verified: true,
    name: 'User Account',
    nickname: 'user',
    given_name: 'User',
    family_name: 'Account',
    password: 'P@ssw0rd',
    groups: ['Everyone', 'Users']
  }
];

// In-memory account storage
class Account {
  constructor(id, profile) {
    this.accountId = id;
    this.profile = profile;
  }

  async claims(use, scope) {
    console.log('Account.claims called:', { use, scope: Array.from(scope || []) });
    const claims = {
      sub: this.accountId,
      sid: this.accountId
    };

    // scope is a Set or array-like object
    const scopeSet = scope instanceof Set ? scope : new Set(scope.split ? scope.split(' ') : scope);
    console.log('Scope set:', Array.from(scopeSet));

    // Add email claims for both id_token and userinfo
    if (scopeSet.has('email')) {
      claims.email = this.profile.email;
      claims.email_verified = this.profile.email_verified;
      console.log('Added email claims:', claims.email);
    }

    // Add profile claims for both id_token and userinfo
    if (scopeSet.has('profile')) {
      claims.name = this.profile.name;
      claims.nickname = this.profile.nickname;
      claims.given_name = this.profile.given_name;
      claims.family_name = this.profile.family_name;
      claims.groups = this.profile.groups;
      if (this.profile.picture) {
        claims.picture = this.profile.picture;
      }
      console.log('Added profile claims');
    }

    console.log('Final claims for', use, ':', Object.keys(claims));
    return claims;
  }

  static async findAccount(ctx, id) {
    const user = USERS.find(u => u.id === id);
    if (!user) return undefined;
    return new Account(id, user);
  }

  static async authenticate(email, password) {
    const user = USERS.find(u => u.email === email && u.password === password);
    if (!user) return undefined;
    return new Account(user.id, user);
  }
}

// OIDC Provider configuration
const configuration = {
  clients: [{
    client_id: CLIENT_ID,
    client_secret: CLIENT_SECRET,
    grant_types: ['authorization_code', 'refresh_token'],
    redirect_uris: REDIRECT_URIS,
    post_logout_redirect_uris: POST_LOGOUT_REDIRECT_URIS,
    response_types: ['code'],
    token_endpoint_auth_method: 'client_secret_post',
    introspection_endpoint_auth_method: 'client_secret_basic'
  }],
  cookies: {
    keys: COOKIE_KEYS,
  },
  claims: {
    openid: ['sub', 'sid'],
    email: ['email', 'email_verified'],
    profile: ['name', 'nickname', 'given_name', 'family_name', 'groups', 'picture']
  },
  scopes: ['openid', 'offline_access', 'email', 'profile'],
  // Include email and profile claims in id_token (not just userinfo endpoint)
  conformIdTokenClaims: false,
  // Policy-based refresh token issuance per upstream docs
  issueRefreshToken: async (ctx, client, code) => {
    console.log('issueRefreshToken check:', {
      grantAllowed: client.grantTypeAllowed('refresh_token'),
      codeScopes: Array.from(code.scopes),
      hasOfflineAccess: code.scopes.has('offline_access')
    });
    if (!client.grantTypeAllowed('refresh_token')) {
      return false;
    }
    
    // Check the code's scopes first (standard behavior)
    if (code.scopes.has('offline_access')) {
      return true;
    }
    
    // Fallback: check if the Grant has offline_access
    // This handles cases where offline_access was stripped from the authorization code
    // but was granted during the interaction
    if (code.grantId) {
      const grant = await ctx.oidc.provider.Grant.find(code.grantId);
      if (grant && grant.getOIDCScope().includes('offline_access')) {
        console.log('Issuing refresh token based on Grant scope (offline_access in grant but not in code)');
        return true;
      }
    }
    
    // Web public clients fallback
    return client.applicationType === 'web' && client.clientAuthMethod === 'none';
  },
  features: {
    devInteractions: { enabled: false },
    introspection: { 
      enabled: true,
      // Include all claims in introspection response
      allowedPolicy: async (ctx, client, token) => {
        return true;
      },
    },
    revocation: { enabled: true },
  },
  findAccount: Account.findAccount,
  ttl: {
    AccessToken: 3600,
    AuthorizationCode: 600,
    IdToken: 3600,
    RefreshToken: 86400 * 30,
  },
  // Custom interaction handling
  interactions: {
    url(ctx, interaction) {
      return `/interaction/${interaction.uid}`;
    },
  },
  // Proxy configuration - trust proxy headers
  proxy: true,
  // Customize introspection response to include email for mail server
  extraTokenClaims: async (ctx, token) => {
    const user = USERS.find(u => u.id === token.accountId);
    if (user) {
      return {
        email: user.email,
        username: user.email,
      };
    }
    return {};
  },
};

const oidc = new Provider(ISSUER, configuration);

// Trust proxy headers (required when behind nginx/reverse proxy)
oidc.proxy = true;

// Log all requests for debugging
const originalJson = oidc.app.context.constructor.prototype.json;
oidc.app.context.constructor.prototype.json = function(obj) {
  // Intercept introspection responses to add email field
  if (this.path === '/token/introspection' && obj && obj.active && obj.sub) {
    const user = USERS.find(u => u.id === obj.sub);
    if (user) {
      obj.email = user.email;
      obj.username = user.email;
      console.log('Introspection response modified:', { active: obj.active, username: obj.username, email: obj.email, sub: obj.sub });
    }
  }
  // Log token endpoint responses for development to validate refresh issuance
  if (this.path === '/token' && obj && typeof obj === 'object') {
    const hasRefresh = !!obj.refresh_token;
    const scopeInfo = obj.scope || '(no scope)';
    console.log('Token response:', { hasRefresh, scope: scopeInfo });
  }
  return originalJson.call(this, obj);
};

oidc.use(async (ctx, next) => {
  console.log(`${ctx.method} ${ctx.path}`);
  if (ctx.path === '/auth' && ctx.query.scope) {
    console.log('Authorization request with scope:', ctx.query.scope);
  }
  await next();
  if (ctx.path === '/token' && ctx.body && typeof ctx.body === 'object') {
    const hasRefresh = Object.prototype.hasOwnProperty.call(ctx.body, 'refresh_token');
    const scopeInfo = ctx.body.scope || '(no scope)';
    console.log('Token response (middleware):', { hasRefresh, scope: scopeInfo });
  }
  if (ctx.path === '/token/introspection' && ctx.body && typeof ctx.body === 'object') {
    console.log('Introspection response (middleware):', JSON.stringify(ctx.body));
  }
});

// Simple login page
oidc.use(async (ctx, next) => {
  if (ctx.path.startsWith('/interaction/')) {
    const uid = ctx.path.split('/')[2];
    
    if (ctx.method === 'GET') {
      // Show login form
      ctx.type = 'html';
      ctx.body = `
        <!DOCTYPE html>
        <html>
        <head>
          <title>Sign In - PaperMail</title>
          <style>
            body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
            .container { max-width: 400px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            h1 { color: #333; margin-bottom: 20px; }
            input { width: 100%; padding: 10px; margin: 10px 0; border: 1px solid #ddd; border-radius: 4px; box-sizing: border-box; }
            button { width: 100%; padding: 12px; background: #007bff; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }
            button:hover { background: #0056b3; }
            .error { color: red; margin: 10px 0; }
          </style>
        </head>
        <body>
          <div class="container">
            <h1>Sign In</h1>
            ${ctx.query.error ? `<div class="error">Invalid email or password</div>` : ''}
            <form method="post" action="/interaction/${uid}">
              <input type="email" name="email" placeholder="Email" required autofocus />
              <input type="password" name="password" placeholder="Password" required />
              <button type="submit">Sign In</button>
            </form>
          </div>
        </body>
        </html>
      `;
    } else if (ctx.method === 'POST') {
      // Handle login
      const body = await parseBody(ctx);
      console.log('Login attempt for:', body.email);
      const account = await Account.authenticate(body.email, body.password);
      
      if (!account) {
        console.log('Authentication failed for:', body.email);
        ctx.redirect(`/interaction/${uid}?error=1`);
        return;
      }

      console.log('Authentication successful for:', body.email);
      const details = await oidc.interactionDetails(ctx.req, ctx.res);
      console.log('Full interaction details:', JSON.stringify(details, null, 2));
      console.log('Prompt name:', details.prompt.name);
      
      const result = {
        login: {
          accountId: account.accountId,
        },
      };

      // Auto-grant all requested scopes and claims - load existing or create new
      let grant;
      const existingGrantId = ctx.oidc?.session?.grantIdFor(details.params.client_id);
      if (existingGrantId) {
        console.log('Loading existing grant:', existingGrantId);
        grant = await oidc.Grant.find(existingGrantId);
      }
      
      if (!grant) {
        console.log('Creating new grant');
        grant = new oidc.Grant({
          accountId: account.accountId,
          clientId: details.params.client_id,
        });
      }

      // Grant ALL scopes requested in the ORIGINAL authorization request
      // We need to get this from somewhere other than details.params.scope
      // because offline_access is stripped from params before interaction
      // For now, always grant the full set of scopes the client is allowed to request
      const requestedScope = 'openid profile email offline_access';
      console.log('Granting scopes:', requestedScope);
      requestedScope.split(/\s+/).filter(Boolean).forEach(s => {
        if (!grant.getOIDCScope().includes(s)) {
          grant.addOIDCScope(s);
        }
      });

      const grantId = await grant.save();

      result.consent = {
        grantId,
      };

      console.log('Finishing interaction with result:', JSON.stringify(result, null, 2));
      await oidc.interactionFinished(ctx.req, ctx.res, result, { mergeWithLastSubmission: false });
    }
  } else {
    await next();
  }
});

// Helper to parse POST body
async function parseBody(ctx) {
  return new Promise((resolve) => {
    let body = '';
    ctx.req.on('data', chunk => body += chunk);
    ctx.req.on('end', () => {
      const params = new URLSearchParams(body);
      resolve(Object.fromEntries(params));
    });
  });
}

// Add custom introspection endpoint for mail server (no client auth required)
oidc.use(async (ctx, next) => {
  if (ctx.path === '/mail/introspect' && ctx.method === 'POST') {
    const body = await parseBody(ctx);
    const token = body.token;
    
    console.log('Mail introspection request for token:', token ? token.substring(0, 20) + '...' : 'null');
    
    if (!token) {
      console.log('Mail introspection: no token provided');
      ctx.body = { active: false };
      return;
    }

    try {
      // Try to find the access token
      const AccessToken = oidc.AccessToken;
      const accessToken = await AccessToken.find(token);
      
      if (!accessToken) {
        console.log('Mail introspection: token not found');
        ctx.body = { active: false };
        return;
      }

      if (accessToken.isExpired) {
        console.log('Mail introspection: token expired');
        ctx.body = { active: false };
        return;
      }

      // Get user info
      const user = USERS.find(u => u.id === accessToken.accountId);
      if (!user) {
        console.log('Mail introspection: user not found for accountId:', accessToken.accountId);
        ctx.body = { active: false };
        return;
      }

      console.log('Mail introspection: success for user:', user.email);
      ctx.body = {
        active: true,
        sub: accessToken.accountId,
        username: user.email,
        email: user.email,
        scope: accessToken.scope,
        exp: accessToken.exp,
        iat: accessToken.iat
      };
    } catch (err) {
      console.error('Mail introspection error:', err);
      ctx.body = { active: false };
    }
  } else {
    await next();
  }
});

// Start the server
const server = http.createServer(oidc.callback());

server.listen(PORT, () => {
  console.log(`OIDC Provider listening on port ${PORT}`);
  console.log(`Issuer: ${ISSUER}`);
  console.log(`External hostname: ${EXTERNAL_HOST_NAME}`);
  console.log(`Configured users: ${USERS.length}`);
});

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('SIGTERM received, closing server...');
  server.close(() => {
    console.log('Server closed');
    process.exit(0);
  });
});
