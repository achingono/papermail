using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Papermail.Core.Configuration;
using Papermail.Core.Entities;

namespace Papermail.Data.Services;

public interface IProviderService
{
    Task<Provider> GetOrCreateProviderAsync(string emailDomain, string? idp, ImapSettings imap, SmtpSettings smtp);
    Task<Provider?> GetByDomainAsync(string emailDomain);
    Task<Domain> MapDomainAsync(Provider provider, string domain);
}

public class ProviderService : IProviderService
{
    private readonly DataContext _db;
    public ProviderService(DataContext db) { _db = db; }

    public async Task<Provider?> GetByDomainAsync(string emailDomain)
    {
        var pd = await _db.Set<Domain>().FirstOrDefaultAsync(d => d.Name == emailDomain);
        if (pd == null) return null;
        return await _db.Providers.FirstAsync(p => p.Id == pd.ProviderId);
    }

    public async Task<Domain> MapDomainAsync(Provider provider, string domain)
    {
        var existing = await _db.Set<Domain>().FirstOrDefaultAsync(d => d.Name == domain);
        if (existing != null) return existing;
        var pd = new Domain { Name = domain, ProviderId = provider.Id };
        _db.Add(pd);
        await _db.SaveChangesAsync();
        return pd;
    }

    public async Task<Provider> GetOrCreateProviderAsync(string emailDomain, string? idp, ImapSettings imap, SmtpSettings smtp)
    {
        // Prefer lookup by idp/name when provided
        Provider? existing = null;
        if (!string.IsNullOrWhiteSpace(idp))
        {
            existing = await _db.Providers.FirstOrDefaultAsync(p => p.Name == idp);
        }
        // Fallback to domain mapping
        existing ??= await GetByDomainAsync(emailDomain);
        if (existing != null)
        {
            // Update settings if missing; prefer explicit config
            bool changed = false;
            if (existing.Imap is null) { existing.Imap = new ImapSettings(); changed = true; }
            if (existing.Smtp is null) { existing.Smtp = new SmtpSettings(); changed = true; }
            // overwrite with provided values to keep in sync
            existing.Imap.Host = imap.Host;
            existing.Imap.Port = imap.Port;
            existing.Imap.UseSsl = imap.UseSsl;
            existing.Imap.TrustCertificates = imap.TrustCertificates;
            existing.Smtp.Host = smtp.Host;
            existing.Smtp.Port = smtp.Port;
            existing.Smtp.UseTls = smtp.UseTls;
            existing.Smtp.TrustCertificates = smtp.TrustCertificates;
            changed = true;
            if (changed)
            {
                await _db.SaveChangesAsync();
            }
            // Ensure domain is mapped
            await MapDomainAsync(existing, emailDomain);
            return existing;
        }

        var provider = new Provider
        {
            Name = string.IsNullOrWhiteSpace(idp) ? emailDomain : idp,
            Imap = new ImapSettings
            {
                Host = imap.Host,
                Port = imap.Port,
                UseSsl = imap.UseSsl,
                TrustCertificates = imap.TrustCertificates
            },
            Smtp = new SmtpSettings
            {
                Host = smtp.Host,
                Port = smtp.Port,
                UseTls = smtp.UseTls,
                TrustCertificates = smtp.TrustCertificates
            }
        };

        _db.Providers.Add(provider);
        await _db.SaveChangesAsync();
        // Map initial domain
        await MapDomainAsync(provider, emailDomain);
        return provider;
    }
}
