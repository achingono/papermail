using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Papermail.Data;

/// <summary>
/// Factory for creating DataContext instances at design time for EF Core tools.
/// </summary>
public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        
        // Try to get connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__SQL") 
            ?? "Server=localhost,1433;Database=Papermail;User Id=sa;Password=YourPassword;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);

        return new DataContext(optionsBuilder.Options);
    }
}
