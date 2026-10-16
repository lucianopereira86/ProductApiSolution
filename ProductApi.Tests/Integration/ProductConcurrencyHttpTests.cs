using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProductApi.Tests.Integration
{
    public class ProductConcurrencyHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductConcurrencyHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }
        [Fact]
        public async Task UpdateProduct_WithOutdatedRowVersion_ShouldReturn409()
        {
            // Arrange
            byte[] rowVersion;
            string token;
            const decimal originalPrice = 10m;
            const decimal updatedPrice = 20m;
            const decimal attemptedPrice = 30m;
            var product = new Product("user-123", "product 1", originalPrice, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                rowVersion = product.RowVersion.ToArray();

                product.ChangePrice(updatedPrice);
                await dbContext.SaveChangesAsync();
            }

            var updateProductRequest = new UpdateProductRequest(
                product.Id,
                product.Name,
                attemptedPrice,
                product.Stock,
                product.Description,
                Convert.ToBase64String(rowVersion));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{product.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;
            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.AsNoTracking().SingleAsync(f => f.Id == product.Id);
                Assert.Equal(updatedPrice, persistedProduct.Price);
            }
        }

        [Fact]
        public async Task UpdateProduct_ByNonOwner_ShouldReturn403()
        {
            // Arrange
            string token;
            const decimal originalPrice = 10m;
            const decimal attemptedPrice = 20m;
            var product = new Product("user-123", "product 1", originalPrice, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-456",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();
            }

            var updateProductRequest = new UpdateProductRequest(
                product.Id,
                product.Name,
                attemptedPrice,
                product.Stock,
                product.Description,
                Convert.ToBase64String(product.RowVersion));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{product.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;
            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.AsNoTracking().SingleAsync(f => f.Id == product.Id);
                Assert.Equal(originalPrice, persistedProduct.Price);
            }
        }

        [Fact]
        public async Task UpdateProduct_WithoutToken_ShouldReturn401()
        {
            // Arrange
            var updateProductRequest = new UpdateProductRequest(
                1,
                "product test 1",
                100,
                10,
                "product test 1 descriptions",
                Convert.ToBase64String(new byte[8]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = "/mediator/products/1";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProduct_WithoutWritePermission_ShouldReturn403()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();

            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var token = tokenService.GenerateToken(
                 userId: "user-123",
                 username: "integration-test",
                 role: "editor",
                 permissions: ["Products.Read"]);
            var updateProductRequest = new UpdateProductRequest(
                1,
                "product test 1",
                100,
                10,
                "product test 1 descriptions",
                Convert.ToBase64String(new byte[8]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = "/mediator/products/1";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProduct_ByOwner_ShouldReturn204()
        {
            // Arrange
            string token;
            byte[] originalRowVersion = [];
            const decimal originalPrice = 10m;
            var product = new Product("user-123", "product 1", originalPrice, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(product); 
                originalRowVersion = product.RowVersion.ToArray();
                await dbContext.SaveChangesAsync();
            }

            var updateProductRequest = new UpdateProductRequest(
                product.Id,
                $"{product.Name} UPDATED",
                product.Price * 2,
                product.Stock * 2,
                $"{product.Description} UPDATED",
                Convert.ToBase64String(product.RowVersion));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{product.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;
            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.AsNoTracking().SingleAsync(f => f.Id == product.Id);
                Assert.Equal(updateProductRequest.Name, persistedProduct.Name);
                Assert.Equal(updateProductRequest.Price, persistedProduct.Price);
                Assert.Equal(updateProductRequest.Stock, persistedProduct.Stock);
                Assert.Equal(updateProductRequest.Description, persistedProduct.Description);
                Assert.Equal(product.CreatedBy, persistedProduct.CreatedBy);
                Assert.False(originalRowVersion.SequenceEqual(persistedProduct.RowVersion));
            }
        }
    }
}
