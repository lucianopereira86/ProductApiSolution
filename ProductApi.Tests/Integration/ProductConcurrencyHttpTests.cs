using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
                await dbContext.SaveChangesAsync();
                originalRowVersion = product.RowVersion.ToArray();
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

        [Fact]
        public async Task UpdateProduct_WhenProductDoesNotExist_ShouldReturn404()
        {
            // Arrange
            string token;
            int nonExistingProductId = -1;

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);
            }

            var updateProductRequest = new UpdateProductRequest(
                nonExistingProductId,
                "original product",
                100,
                10,
                "updated product",
                Convert.ToBase64String(new byte[8]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{nonExistingProductId}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;
            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProduct_WhenRouteIdDiffersFromBodyId_ShouldReturn400()
        {
            // Arrange
            string token;

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);
            }

            var updateProductRequest = new UpdateProductRequest(
                10,
                "original product",
                100,
                10,
                "updated product",
                Convert.ToBase64String(new byte[8]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/1";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;
            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"Route ID and body ID must match.\"", errorMessage);
        }

        [Fact]
        public async Task UpdateProduct_WithInvalidBase64RowVersion_ShouldReturn400()
        {
            // Arrange
            string token;
            var originalProduct = new Product("user-123", "product 1", 10, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(originalProduct);
                await dbContext.SaveChangesAsync();

                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);
            }

            var updateProductRequest = new UpdateProductRequest(
                originalProduct.Id,
                "original product",
                100,
                10,
                "updated product",
                RowVersion: "invalid-base64!");

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{originalProduct.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
            Assert.Equal("Row version is not a valid Base64 string.", problem.Detail);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.SingleAsync(s => s.Id == originalProduct.Id);
                Assert.Equal(originalProduct.Name, persistedProduct.Name);
                Assert.Equal(originalProduct.Price, persistedProduct.Price);
                Assert.Equal(originalProduct.Stock, persistedProduct.Stock);
                Assert.Equal(originalProduct.Description, persistedProduct.Description);
                Assert.True(originalProduct.RowVersion.SequenceEqual(persistedProduct.RowVersion));
            }
        }

        [Fact]
        public async Task UpdateProduct_WithInvalidRowVersionLength_ShouldReturn400()
        {
            // Arrange
            string token;
            var originalProduct = new Product("user-123", "product 1", 10, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(originalProduct);
                await dbContext.SaveChangesAsync();

                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);
            }

            var updateProductRequest = new UpdateProductRequest(
                originalProduct.Id,
                "original product",
                100,
                10,
                "updated product",
                RowVersion: Convert.ToBase64String(new byte[4]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{originalProduct.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
            Assert.Equal("RowVersion must contain exactly 8 bytes.", problem.Detail);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.SingleAsync(s => s.Id == originalProduct.Id);
                Assert.Equal(originalProduct.Name, persistedProduct.Name);
                Assert.Equal(originalProduct.Price, persistedProduct.Price);
                Assert.Equal(originalProduct.Stock, persistedProduct.Stock);
                Assert.Equal(originalProduct.Description, persistedProduct.Description);
                Assert.True(originalProduct.RowVersion.SequenceEqual(persistedProduct.RowVersion));
            }
        }

        [Fact]
        public async Task UpdateProduct_WithNegativePrice_ShouldReturn400()
        {
            // Arrange
            string token;
            var originalProduct = new Product("user-123", "product 1", 10, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(originalProduct);
                await dbContext.SaveChangesAsync();

                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);
            }

            var updateProductRequest = new UpdateProductRequest(
                originalProduct.Id,
                "original product",
                -1,
                10,
                "updated product",
                RowVersion: Convert.ToBase64String(new byte[8]));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{originalProduct.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
            Assert.Contains("Price must be greater than zero.", problem.Detail);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.SingleAsync(s => s.Id == originalProduct.Id);
                Assert.Equal(originalProduct.Name, persistedProduct.Name);
                Assert.Equal(originalProduct.Price, persistedProduct.Price);
                Assert.Equal(originalProduct.Stock, persistedProduct.Stock);
                Assert.Equal(originalProduct.Description, persistedProduct.Description);
                Assert.True(originalProduct.RowVersion.SequenceEqual(persistedProduct.RowVersion));
            }
        }
    }
}
