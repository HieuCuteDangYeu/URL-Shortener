using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UrlShortener.UserService.Infrastructure.Persistence;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=users;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
        return new UserDbContext(optionsBuilder.Options);
    }
}