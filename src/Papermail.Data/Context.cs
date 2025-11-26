using Microsoft.EntityFrameworkCore;
using Papermail.Core.Entities;

namespace Papermail.Data;

/// <summary>
/// Represents the database context for the Papermail application.
/// Provides access to database sets for users, accounts, and providers.
/// </summary>
public class Context : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Context"/> class.
    /// </summary>
    /// <param name="options">The options to configure the database context.</param>
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }
    
    /// <summary>
    /// Gets or sets the database set for users.
    /// </summary>
    public DbSet<User> Users { get; set; }
    
    /// <summary>
    /// Gets or sets the database set for email accounts.
    /// </summary>
    public DbSet<Account> Accounts { get; set; }
    
    /// <summary>
    /// Gets or sets the database set for email providers.
    /// </summary>
    public DbSet<Provider> Providers { get; set; }
}