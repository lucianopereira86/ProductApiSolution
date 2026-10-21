using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProductApi.Tests.Integration
{
    public class ProductValidationHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductValidationHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }
        [Fact]
        public async Task UpdateProduct_WithInvalidFields_ShouldReturnValidationProblemDetails()
        {
            // Arrange
            byte[] rowVersion;
            string token;
            const decimal originalPrice = 10m;
            var originalProduct = new Product("user-123", "product 1", originalPrice, 1);

            using (var scope = _factory.Services.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
                token = tokenService.GenerateToken(
                    userId: "user-123",
                    username: "integration-test",
                    role: "editor",
                    permissions: ["Products.Read", "Products.Write"]);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Products.Add(originalProduct);
                await dbContext.SaveChangesAsync();

                rowVersion = originalProduct.RowVersion.ToArray();
            }

            var updateProductRequest = new UpdateProductRequest(
                originalProduct.Id,
                string.Empty,
                -10,
                -1,
                null,
                Convert.ToBase64String(rowVersion));

            var httpContent = JsonContent.Create(updateProductRequest);
            string endpoint = $"/mediator/products/{originalProduct.Id}";
            using var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = httpContent;

            // Act
            using var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problems = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

            Assert.NotNull(problems);
            Assert.Equal("One or more validation errors occurred.", problems.Title);
            Assert.Equal(endpoint, problems.Instance);
            Assert.Equal(3, problems.Errors.Count);
            Assert.True(problems.Errors.ContainsKey("Name"));
            Assert.True(problems.Errors.ContainsKey("Price"));
            Assert.True(problems.Errors.ContainsKey("Stock"));
            Assert.True(problems.Errors["Name"].Length > 0);
            Assert.True(problems.Errors["Price"].Length > 0);
            Assert.True(problems.Errors["Stock"].Length > 0);
            Assert.Contains("Name is required", problems.Errors["Name"][0]);
            Assert.Contains("Price must be greater than zero", problems.Errors["Price"][0]);
            Assert.Contains("Stock must not be negative", problems.Errors["Stock"][0]);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var persistedProduct = await dbContext.Products.AsNoTracking().SingleAsync(s => s.Id == originalProduct.Id);
                Assert.Equal(originalProduct.Name, persistedProduct.Name);
                Assert.Equal(originalProduct.Price, persistedProduct.Price);
                Assert.Equal(originalProduct.Stock, persistedProduct.Stock);
                Assert.Equal(originalProduct.Description, persistedProduct.Description);
                Assert.True(originalProduct.RowVersion.SequenceEqual(persistedProduct.RowVersion));
            }

        }
    }
}
