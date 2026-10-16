using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace ProductApi.Tests.Integration;

public class ProductPersistenceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProductPersistenceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_ShouldApplyMigrations()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
    }

    [Fact]
    public async Task AddProduct_ShouldPersistInDatabase()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        int productId;
        var defaultProduct = new
        {
            CreatedBy = "integration-user-123",
            Name = "Integration Test Product",
            Price = 150,
            Stock = 10,
            Description = "Created by integration test",
            RowVersion = new byte[8]
        };

        // Etapa A: inserir o produto
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = new Product(
                defaultProduct.CreatedBy,
                defaultProduct.Name,
                defaultProduct.Price,
                defaultProduct.Stock,
                defaultProduct.Description);
            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        }
        // Etapa B: recuperar o produto com outro DbContext
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act
            var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(f => f.Id == productId);

            // Assert
            Assert.NotNull(product);
            Assert.Equal(defaultProduct.Name, product.Name);
            Assert.Equal(defaultProduct.Price, product.Price);
            Assert.Equal(defaultProduct.Stock, product.Stock);
            Assert.Equal(defaultProduct.Description, product.Description);
            Assert.NotNull(product.RowVersion);
            Assert.Equal(defaultProduct.RowVersion.Length, product.RowVersion.Length);
        }
    }

    [Fact]
    public async Task ConcurrentUpdate_ShouldThrowDbUpdateConcurrencyException()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        int productId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var product = new Product(
                "user-123",
                "Concurrent Product",
                100,
                10);

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        }

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        using var scopeC = _factory.Services.CreateScope();

        var dbContextA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbContextB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

        var productA = await dbContextA.Products.FirstAsync(f => f.Id == productId);
        var productB = await dbContextB.Products.FirstAsync(f => f.Id == productId);

        // Act
        productA.ChangePrice(200);
        await dbContextA.SaveChangesAsync();

        productB.ChangePrice(300);

        // Assert
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async() => await dbContextB.SaveChangesAsync());

        var dbContextC = scopeC.ServiceProvider.GetRequiredService<AppDbContext>();
        var productC = await dbContextC.Products.FirstAsync(f => f.Id == productId);

        Assert.Equal(productA.Price, productC.Price);
    }
}