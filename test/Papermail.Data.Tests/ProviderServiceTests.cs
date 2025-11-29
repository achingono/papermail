using Microsoft.EntityFrameworkCore;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;
using Papermail.Data;
using Papermail.Data.Services;

namespace Papermail.Data.Tests;

public class ProviderServiceTests
{
    private readonly DataContext _context;
    private readonly ProviderService _service;

    public ProviderServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DataContext(options);
        _service = new ProviderService(_context);
    }

    [Fact]
    public async Task GetByDomainAsync_WhenDomainExists_ReturnsProvider()
    {
        // Arrange
        var provider = new Provider { Name = "TestProvider" };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var domain = new Domain { Name = "test.com", ProviderId = provider.Id };
        _context.Set<Domain>().Add(domain);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByDomainAsync("test.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(provider.Id, result.Id);
        Assert.Equal("TestProvider", result.Name);
    }

    [Fact]
    public async Task GetByDomainAsync_WhenDomainNotExists_ReturnsNull()
    {
        // Act
        var result = await _service.GetByDomainAsync("nonexistent.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MapDomainAsync_WhenDomainNotExists_CreatesDomainMapping()
    {
        // Arrange
        var provider = new Provider { Name = "TestProvider" };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MapDomainAsync(provider, "newdomain.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newdomain.com", result.Name);
        Assert.Equal(provider.Id, result.ProviderId);

        var savedDomain = await _context.Set<Domain>().FirstOrDefaultAsync(d => d.Name == "newdomain.com");
        Assert.NotNull(savedDomain);
    }

    [Fact]
    public async Task MapDomainAsync_WhenDomainExists_ReturnsExistingMapping()
    {
        // Arrange
        var provider = new Provider { Name = "TestProvider" };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var existingDomain = new Domain { Name = "existing.com", ProviderId = provider.Id };
        _context.Set<Domain>().Add(existingDomain);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.MapDomainAsync(provider, "existing.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingDomain.Id, result.Id);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_WithNewDomain_CreatesProviderAndMapping()
    {
        // Arrange
        var imap = new ImapSettings { Host = "imap.test.com", Port = 993, UseSsl = true };
        var smtp = new SmtpSettings { Host = "smtp.test.com", Port = 587, UseTls = true };

        // Act
        var result = await _service.GetOrCreateProviderAsync("newdomain.com", null, imap, smtp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newdomain.com", result.Name);
        Assert.Equal("imap.test.com", result.Imap.Host);
        Assert.Equal("smtp.test.com", result.Smtp.Host);

        var domainMapping = await _context.Set<Domain>().FirstOrDefaultAsync(d => d.Name == "newdomain.com");
        Assert.NotNull(domainMapping);
        Assert.Equal(result.Id, domainMapping.ProviderId);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_WithIdp_UsesIdpAsName()
    {
        // Arrange
        var imap = new ImapSettings { Host = "imap.test.com", Port = 993 };
        var smtp = new SmtpSettings { Host = "smtp.test.com", Port = 587 };

        // Act
        var result = await _service.GetOrCreateProviderAsync("test.com", "GoogleIDP", imap, smtp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("GoogleIDP", result.Name);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_WithExistingProvider_UpdatesSettings()
    {
        // Arrange
        var provider = new Provider
        {
            Name = "ExistingProvider",
            Imap = new ImapSettings { Host = "old-imap.com", Port = 143 },
            Smtp = new SmtpSettings { Host = "old-smtp.com", Port = 25 }
        };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var newImap = new ImapSettings { Host = "new-imap.com", Port = 993, UseSsl = true };
        var newSmtp = new SmtpSettings { Host = "new-smtp.com", Port = 587, UseTls = true };

        // Act
        var result = await _service.GetOrCreateProviderAsync("test.com", "ExistingProvider", newImap, newSmtp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(provider.Id, result.Id);
        Assert.Equal("new-imap.com", result.Imap.Host);
        Assert.Equal(993, result.Imap.Port);
        Assert.True(result.Imap.UseSsl);
        Assert.Equal("new-smtp.com", result.Smtp.Host);
        Assert.Equal(587, result.Smtp.Port);
        Assert.True(result.Smtp.UseTls);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_WithExistingDomain_ReusesProvider()
    {
        // Arrange
        var provider = new Provider { Name = "TestProvider" };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var domain = new Domain { Name = "test.com", ProviderId = provider.Id };
        _context.Set<Domain>().Add(domain);
        await _context.SaveChangesAsync();

        var imap = new ImapSettings { Host = "imap.test.com", Port = 993 };
        var smtp = new SmtpSettings { Host = "smtp.test.com", Port = 587 };

        // Act
        var result = await _service.GetOrCreateProviderAsync("test.com", null, imap, smtp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(provider.Id, result.Id);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_MultipleDomainsToSameProvider_SharesSettings()
    {
        // Arrange
        var imap = new ImapSettings { Host = "imap.outlook.com", Port = 993, UseSsl = true };
        var smtp = new SmtpSettings { Host = "smtp.outlook.com", Port = 587, UseTls = true };

        // Act - Create provider for hotmail.com
        var provider1 = await _service.GetOrCreateProviderAsync("hotmail.com", "Outlook", imap, smtp);
        
        // Act - Map live.com to the same provider
        var provider2 = await _service.GetOrCreateProviderAsync("live.com", "Outlook", imap, smtp);

        // Assert
        Assert.NotNull(provider1);
        Assert.NotNull(provider2);
        Assert.Equal(provider1.Id, provider2.Id); // Same provider
        Assert.Equal("Outlook", provider1.Name);
        Assert.Equal("Outlook", provider2.Name);

        // Verify both domains are mapped
        var hotmailDomain = await _context.Set<Domain>().FirstOrDefaultAsync(d => d.Name == "hotmail.com");
        var liveDomain = await _context.Set<Domain>().FirstOrDefaultAsync(d => d.Name == "live.com");

        Assert.NotNull(hotmailDomain);
        Assert.NotNull(liveDomain);
        Assert.Equal(provider1.Id, hotmailDomain.ProviderId);
        Assert.Equal(provider1.Id, liveDomain.ProviderId);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_WithNullIdp_UsesDomainAsName()
    {
        // Arrange
        var imap = new ImapSettings();
        var smtp = new SmtpSettings();

        // Act
        var result = await _service.GetOrCreateProviderAsync("example.com", null, imap, smtp);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("example.com", result.Name);
    }

    [Fact]
    public async Task GetOrCreateProviderAsync_EnsuresDomainMappingForExistingProvider()
    {
        // Arrange
        var provider = new Provider { Name = "Office365" };
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        var imap = new ImapSettings();
        var smtp = new SmtpSettings();

        // Act
        var result = await _service.GetOrCreateProviderAsync("office365.com", "Office365", imap, smtp);

        // Assert
        var domainMapping = await _context.Set<Domain>().FirstOrDefaultAsync(d => d.Name == "office365.com");
        Assert.NotNull(domainMapping);
        Assert.Equal(provider.Id, domainMapping.ProviderId);
    }
}
