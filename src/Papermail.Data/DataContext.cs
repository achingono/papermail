using Microsoft.EntityFrameworkCore;
using Papermail.Core.Entities;

namespace Papermail.Data;

/// <summary>
/// Represents the database context for the Papermail application.
/// Provides access to database sets for users, accounts, and providers.
/// </summary>
public class DataContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the database context.</param>
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }
    
    /// <summary>
    /// Gets or sets the database set for email accounts.
    /// </summary>
    public DbSet<Account> Accounts { get; set; }
    
    /// <summary>
    /// Gets or sets the database set for email providers.
    /// </summary>
    public DbSet<Provider> Providers { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships and constraints if needed
        modelBuilder.Entity<Account>()
            .HasOne(a => a.Provider)
            .WithMany(p => p.Accounts)
            .HasForeignKey(a => a.ProviderId);

        // ensure the account email is unique
        modelBuilder.Entity<Account>()
            .HasIndex(a => a.EmailAddress)
            .IsUnique();

        // ensure the provider name is unique
        modelBuilder.Entity<Provider>()
            .HasIndex(p => p.Name)
            .IsUnique();

        // provider domains map many domains to one provider
        modelBuilder.Entity<Domain>()
            .HasIndex(d => d.Name)
            .IsUnique();

        modelBuilder.Entity<Provider>()
            .HasMany(p => p.Domains)
            .WithOne(d => d.Provider)
            .HasForeignKey(d => d.ProviderId);
        
        // Configure owned Provider settings
        modelBuilder.Entity<Provider>()
            .OwnsOne(p => p.Imap)
            .WithOwner();
        
        modelBuilder.Entity<Provider>()
            .OwnsOne(p => p.Smtp)
            .WithOwner();
    }
}