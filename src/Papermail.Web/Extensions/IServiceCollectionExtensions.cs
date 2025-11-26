using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using Papermail.Web.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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

        var fileSettings = services.Configure<FileSettings>(configuration, "Files");
        var identitySettings = services.Configure<IdentitySettings>(configuration, "Identity");

        services.AddAuthentication(environment, identitySettings, fileSettings);
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
        IdentitySettings settings,
        FileSettings fileSettings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var strictValidation = environment.IsProduction() || environment.IsStaging();

        services.AddCors();
        var authBuilder = services.AddAuthentication(options =>
        {
            // Set Cookie as default for browser-based auth (sign-in, sign-out)
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            // Set JWT Bearer as the default challenge for API requests
            // This ensures API requests without auth get 401 instead of redirects
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            // Note: Individual endpoints should specify [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
            // to support both authentication methods
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = strictValidation;
            x.TokenValidationParameters.ValidateIssuer = strictValidation;
            x.TokenValidationParameters.ValidateAudience = strictValidation;
            x.TokenValidationParameters.ValidateLifetime = strictValidation;
            x.TokenValidationParameters.ValidateIssuerSigningKey = strictValidation;
            x.TokenValidationParameters.ValidIssuer = settings.Token.Issuer;
            x.TokenValidationParameters.ValidAudience = settings.Token.Audience;
            x.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(settings.Token.SecretKey));
            // x.TokenValidationParameters.SignatureValidator = (token, parameters) =>
            // {
            //     JsonWebToken jwtToken = new JsonWebToken(token);
            //     return jwtToken;
            // };
            x.SaveToken = true;

            // Add detailed logging for JWT authentication failures
            x.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    logger.LogInformation("JWT OnMessageReceived: Authorization Header = '{AuthHeader}'",
                        string.IsNullOrEmpty(authHeader) ? "MISSING" : authHeader);
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT Authentication failed: {Exception}", context.Exception.Message);
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    logger.LogDebug("JWT Token: {Token}", string.IsNullOrEmpty(authHeader) ? "None" : authHeader);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogInformation("JWT Token validated successfully for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT Challenge triggered. Error: {Error}, ErrorDescription: {ErrorDescription}, AuthFailure: {AuthFailure}",
                        context.Error, context.ErrorDescription, context.AuthenticateFailure?.Message ?? "None");

                    // Don't allow cookie redirect for requests with Authorization header or Accept: application/json
                    if (context.Request.Headers.ContainsKey("Authorization") ||
                        context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true))
                    {
                        // Suppress the default challenge behavior (no redirect)
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers.Append("WWW-Authenticate", "Bearer");
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            // Configure cookie settings to prevent correlation failures
            options.Cookie.Name = "Famorize.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;

            // Configure login and logout paths
            options.LoginPath = "/login";
            options.LogoutPath = "/signout";
            options.AccessDeniedPath = "/access-denied";

            // Customize events to prevent redirects for API requests
            options.Events.OnRedirectToLogin = context =>
            {
                // If this is an API request (has Accept: application/json or Authorization header),
                // return 401 instead of redirecting
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true) ||
                    context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                // Otherwise, redirect to login for browser requests
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                // If this is an API request, return 403 instead of redirecting
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.Headers.Accept.Any(h => h?.Contains("application/json") == true) ||
                    context.Request.Headers.ContainsKey("Authorization"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                // Otherwise, redirect to access denied page for browser requests
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        if (!environment.IsEnvironment("Testing"))
        {
            authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = settings.OpenId.Authority;
                options.ClientId = settings.OpenId.ClientId;
                options.ClientSecret = settings.OpenId.ClientSecret;
                options.RequireHttpsMetadata = settings.OpenId.RequireHttpsMetadata;

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;

                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.GetClaimsFromUserInfoEndpoint = true;

                // Handle claim mapping when tokens are received
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
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
                        }

                        return Task.CompletedTask;
                    }
                };
            })
            .AddFacebook(fb =>
            {
                fb.AppId = settings.Facebook.AppId;
                fb.AppSecret = settings.Facebook.AppSecret;
                fb.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                fb.SaveTokens = true;
                fb.Events = new OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        // Log authentication failures for debugging
                        System.Diagnostics.Debug.WriteLine($"Facebook authentication failed: {context.Failure?.Message}");
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            })
            .AddGoogle(g =>
            {
                g.ClientId = settings.Google.ClientId;
                g.ClientSecret = settings.Google.ClientSecret;
                g.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                g.SaveTokens = true;
                g.Events = new OAuthEvents
                {
                    OnRemoteFailure = context =>
                    {
                        // Log authentication failures for debugging
                        System.Diagnostics.Debug.WriteLine($"Google authentication failed: {context.Failure?.Message}");
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            })
            .AddMicrosoftAccount(ms =>
            {
                ms.ClientId = settings.Microsoft.ClientId;
                ms.ClientSecret = settings.Microsoft.ClientSecret;
                ms.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                ms.SaveTokens = true;

                // Ensure proper callback URL handling
                ms.CallbackPath = "/signin-microsoft";

                // Configure correlation cookie settings to prevent correlation failures
                ms.CorrelationCookie.Name = "Microsoft.AspNetCore.Correlation";
                ms.CorrelationCookie.HttpOnly = true;
                ms.CorrelationCookie.SameSite = SameSiteMode.Lax;
                ms.CorrelationCookie.SecurePolicy = environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;

                ms.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Log successful token creation for debugging
                        System.Diagnostics.Debug.WriteLine($"Microsoft authentication successful for user: {context.Principal?.Identity?.Name}");
                        await Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        // Log authentication failures for debugging
                        System.Diagnostics.Debug.WriteLine($"Microsoft authentication failed: {context.Failure?.Message}");
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            })
            .AddApple(a =>
            {
                a.ClientId = settings.Apple.ClientId;
                a.KeyId = settings.Apple.KeyId;
                a.TeamId = settings.Apple.TeamId;
                a.UsePrivateKey(keyId =>
                    new FileProviders.PhysicalFileProvider(fileSettings.RootPath)
                                    .GetFileInfo($"AuthKey_{keyId}.p8")
                );
                a.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                a.SaveTokens = true;
                a.Events = new AspNet.Security.OAuth.Apple.AppleAuthenticationEvents
                {
                    OnRemoteFailure = context =>
                    {
                        // Log authentication failures for debugging
                        System.Diagnostics.Debug.WriteLine($"Apple authentication failed: {context.Failure?.Message}");
                        context.Response.Redirect("/");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });
        }

        if (string.IsNullOrWhiteSpace(settings.AuthenticationType))
            settings.AuthenticationType = JwtBearerDefaults.AuthenticationScheme;

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