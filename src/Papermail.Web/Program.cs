using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Papermail.Web.Security;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using Papermail.Data;

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

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.TryAddSingleton<IPrincipalAccessor, PrincipalAccessor>();
builder.Services.TryAddScoped<IPrincipal>(provider => provider.GetRequiredService<IPrincipalAccessor>().Principal!);
//builder.Services.TryAddSingleton<IHostNameAccessor, HostNameAccessor>();
builder.Services.AddDbContext<Context>(options =>
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
builder.Services.AddScoped<IUserLocator, UserLocator>();

// Enables Application Insights telemetry collection.
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddServiceProfiler();
builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddAuthentication(builder.Configuration, builder.Environment);

// Required NuGet package: Microsoft.Extensions.Diagnostics.HealthChecks
builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddSqlServer(builder.Configuration.GetConnectionString("Sql")!,
                    name: "SqlServer")
                .AddDbContextCheck<Context>()
                .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "Redis");

var app = builder.Build();

// Configure forwarded headers FIRST - must be before UseHttpsRedirection
app.UseForwardedHeaders();

// Note: UseHttpsRedirection comes after UseForwardedHeaders
// so it can see the X-Forwarded-Proto header
app.UseHttpsRedirection();

// Note: UseAuthentication must come before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();  // Handles /oauth/callback implicitly

app.Run();
