using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Papermail.Web.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Papermail.Web.Services;
using Papermail.Data.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Configures web services for the application, including controllers, OData, and authentication.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration containing settings.</param>
    /// <param name="environment">The hosting environment of the application.</param>
    public static void AddAuthentication(this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ArgumentNullException.ThrowIfNull(environment);

        var identitySettings = services.Configure<OpenIdSettings>(configuration, "OpenId");

        services.AddAuthentication(environment, identitySettings);
    }

    /// <summary>
    /// Configures authentication and token validation services for the application.
    /// Supports multiple authentication providers such as JWT, Facebook, Google, Microsoft, and Apple.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="environment">The hosting environment of the application, used to determine strict validation.</param>
    /// <param name="settings">The identity settings containing configuration for tokens and external providers.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAuthentication(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        OpenIdSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var strictValidation = environment.IsProduction() || environment.IsStaging();

        var authBuilder = services.AddAuthentication(options =>
        {
            // Set Cookie as default for browser-based auth (sign-in, sign-out)
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            // Set OpenID Connect as the default challenge for API and Page requests
            // This ensures API requests without auth get 401 instead of redirects
            // Note: Individual endpoints should specify [Authorize(AuthenticationSchemes = "OpenIdConnect,Cookies")]
            // to support both authentication methods
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                // 1. Authority: This tells the app where to find the B2C metadata
                options.Authority = settings.Authority;
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                if (!string.IsNullOrWhiteSpace(settings.MetadataAddress))
                {
                    options.MetadataAddress = settings.MetadataAddress;
                }

                // In Development, relax backchannel certificate validation to allow local dev certs
                if (environment.IsDevelopment())
                {
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                }

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;

                options.ResponseType = "code"; // Use Authorization Code flow (standard for security)
                options.SaveTokens = true;     // Saves the tokens in the cookie so you can read them later

                // 3. Scopes: Define the scopes you want to request
                foreach (var scope in settings.Scopes)
                {
                    options.Scope.Add(scope);
                }

                //options.GetClaimsFromUserInfoEndpoint = true;

                // 4. Token Validation
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name" // Maps the "name" claim to User.Identity.Name
                };

                // Handle claim mapping when tokens are received
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // Log all claims for debugging
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenIdConnectEvents>>();
                            logger.LogInformation("OIDC Claims received: {Claims}",
                                string.Join(", ", identity.Claims.Select(c => $"{c.Type}={c.Value}")));

                            // Map 'sub' claim to both NameIdentifier and 'sid' for compatibility
                            var subClaim = identity.FindFirst("sub");
                            if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                                identity.AddClaim(new Claim("sid", subClaim.Value));
                            }

                            // Ensure standard claim types are mapped
                            var emailClaim = identity.FindFirst("email")
                                ?? identity.FindFirst(ClaimTypes.Email)
                                ?? identity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

                            if (emailClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.Email))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Email, emailClaim.Value));
                                // Also add simplified "email" claim for compatibility
                                if (!identity.HasClaim(c => c.Type == "email"))
                                {
                                    identity.AddClaim(new Claim("email", emailClaim.Value));
                                }
                            }

                            var nameClaim = identity.FindFirst("name");
                            if (nameClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.Name))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
                            }

                            var givenNameClaim = identity.FindFirst("given_name");
                            if (givenNameClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.GivenName))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.GivenName, givenNameClaim.Value));
                            }

                            var familyNameClaim = identity.FindFirst("family_name");
                            if (familyNameClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.Surname))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Surname, familyNameClaim.Value));
                            }

                            // Create or update account in database
                            var accountService = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
                            var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                            
                            try
                            {
                                await accountService.EnsureAccountAsync(context.Principal, (account) =>
                                {
                                    var expiresAt = context.Properties?.Items.TryGetValue(".Token.expires_at", out var expiresAtValue) == true
                                        ? expiresAtValue
                                        : null;
                                    var refreshToken = context.Properties?.Items.TryGetValue(".Token.refresh_token", out var refreshTokenValue) == true
                                        ? refreshTokenValue
                                        : null;
                                    var accessToken = context.Properties?.Items.TryGetValue(".Token.access_token", out var accessTokenValue) == true
                                        ? accessTokenValue
                                        : null;

                                    account.AccessToken = string.IsNullOrWhiteSpace(accessToken) 
                                        ? string.Empty 
                                        : tokenService.ProtectToken(accessToken);
                                    account.RefreshToken = string.IsNullOrWhiteSpace(refreshToken)
                                        ? string.Empty
                                        : tokenService.ProtectToken(refreshToken);
                                    account.ExpiresAt = string.IsNullOrWhiteSpace(expiresAt)
                                        ? DateTimeOffset.UtcNow.AddHours(1)
                                        : DateTimeOffset.Parse(expiresAt);
                                    account.Scopes = context.Properties?.Items.TryGetValue(".Token.scope", out var scopeValue) == true
                                        ? scopeValue.Split(' ')
                                        : [];
                                    account.DisplayName = identity.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
                                }, createIfNotExists: true);
                                
                                logger.LogInformation("Account ensured for user {UserId}", context.Principal.FindFirst("sub")?.Value);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to ensure account during OIDC token validation");
                            }
                        }
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Add IOptions<TSettings> to the DI container
    /// </summary>
    /// <typeparam name="TSettings"></typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration containing settings.</param>
    /// <param name="sectionName">The name of the configuration section to read settings from</param>
    /// <returns></returns>
    public static TSettings Configure<TSettings>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TSettings : class
    {
        // Add the TSettings section
        // to the configuration
        // and bind it to the TSettings class
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers
        var section = configuration.GetSection(sectionName);
        var settings = section.Get<TSettings>()!;

        // Add IOptions<TSettings> to the DI container
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/options
        services.Configure<TSettings>(section);

        return settings;
    }
}