using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ProductApi.Tests.Integration
{
    public class CustomWebApplicationFactory: WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                services.AddDbContext<AppDbContext>(
                    options => options.UseSqlServer(
                        "Server=(localdb)\\MSSQLLocalDB;" +
                        "Database=ProductApi_IntegrationTests;" +
                        "Trusted_Connection=True;" +
                        "TrustServerCertificate=True;"));
            });           
        }

        public async Task InitializeDatabaseAsync()
        {
            using var scope = Services.CreateScope();

            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var databaseName =
                dbContext.Database.GetDbConnection().Database;

            if (databaseName != "ProductApi_IntegrationTests")
            {
                throw new InvalidOperationException(
                    "Unsafe database configuration for integration tests.");
            }

            await dbContext.Database.MigrateAsync();
        }
    }
}
