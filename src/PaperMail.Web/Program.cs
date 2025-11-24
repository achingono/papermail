using Microsoft.AspNetCore.DataProtection;
using PaperMail.Application.Services;
using PaperMail.Core.Interfaces;
using PaperMail.Infrastructure.Authentication;
using PaperMail.Infrastructure.Configuration;
using PaperMail.Infrastructure.Email;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();

// Configure settings from appsettings.json
builder.Services.Configure<ImapSettings>(builder.Configuration.GetSection("Imap"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<OAuthSettings>(builder.Configuration.GetSection("OAuth"));

// Register infrastructure services
builder.Services.AddSingleton<ITokenStorage, TokenStorage>();
builder.Services.AddSingleton<IMailKitWrapper, MailKitWrapper>();
builder.Services.AddScoped<IEmailRepository, ImapEmailRepository>();

// Register application services
builder.Services.AddScoped<IEmailService, EmailService>();

// Add data protection for token encryption
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("PaperMail");

// Add distributed memory cache for token storage (use Redis in production)
builder.Services.AddDistributedMemoryCache();

// Configure session for user state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
