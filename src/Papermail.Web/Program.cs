using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Papermail.Web.Security;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Papermail.Data;
using Papermail.Data.Services;
using Papermail.Web.Services;
using Papermail.Data.Repositories;
using Papermail.Web.Clients;
using Papermail.Data.Clients;
using Papermail.Core.Configuration;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers to trust proxy headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure SMTP settings from configuration
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

// Configure IMAP settings from configuration
builder.Services.Configure<ImapSettings>(builder.Configuration.GetSection("Imap"));

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.TryAddSingleton<IPrincipalAccessor, PrincipalAccessor>();
builder.Services.TryAddScoped<IPrincipal>(provider => provider.GetRequiredService<IPrincipalAccessor>().Principal!);
builder.Services.TryAddScoped<IAccountService, AccountService>();
builder.Services.TryAddScoped<IEmailService, EmailService>();
builder.Services.TryAddScoped<ITokenService, TokenService>();
builder.Services.TryAddScoped<IEmailRepository, EmailRepository>();
builder.Services.TryAddScoped<IImapClient, ImapClient>();
builder.Services.TryAddScoped<ISmtpClient, SmtpClient>();

builder.Services.AddDataProtection();

builder.Services.AddDbContext<DataContext>(options =>
{
    var connectionStringName = "Sql";
    var connectionString = builder.Configuration.GetConnectionString(connectionStringName);

    if (string.IsNullOrWhiteSpace(connectionString) && builder.Environment.EnvironmentName != "Testing")
        throw new InvalidOperationException($"Connection string '{connectionStringName}' is missing.");
    else
        options.UseSqlServer(connectionString,
            options =>
            {
                options.EnableRetryOnFailure();
            });

    if (builder!.Environment.IsDevelopment())
        options.EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true);
});

// Enables Application Insights telemetry collection.
if (!(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")))
{
    builder.Services.AddApplicationInsightsTelemetry();
    builder.Services.AddServiceProfiler();
}
builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddAuthentication(builder.Configuration, builder.Environment);

// Required NuGet package: Microsoft.Extensions.Diagnostics.HealthChecks
builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddSqlServer(builder.Configuration.GetConnectionString("Sql")!,
                    name: "SqlServer")
                .AddDbContextCheck<DataContext>()
                .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "Redis");

var app = builder.Build();

// Configure forwarded headers FIRST - must be before UseHttpsRedirection
app.UseForwardedHeaders();

// Note: UseHttpsRedirection comes after UseForwardedHeaders
// so it can see the X-Forwarded-Proto header
app.UseHttpsRedirection();
app.UseStaticFiles();

// Note: UseAuthentication must come before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();  // Handles /oauth/callback implicitly
app.MapHealthChecks("/healthz");

if (!app.Environment.IsProduction())
{
    using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
    {
        try
        {
            var context = scope!.ServiceProvider.GetRequiredService<DataContext>();
            //var seeder = scope.ServiceProvider.GetService<IDbContextSeeder>();

            await context.Database.EnsureCreatedAsync();
            if (context.Database.IsRelational())
            {
                await context.Database.MigrateAsync();
            }

            //Task.Run(() => seeder?.SeedAsync()).Wait();
        }
        catch (Exception ex)
        {
            // Log the exception but don't crash the application
            var logger = scope!.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database migration failed, but application will continue");
        }
    }
}
app.Run();
