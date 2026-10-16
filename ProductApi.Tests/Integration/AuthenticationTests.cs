using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace ProductApi.Tests.Integration
{
    public class AuthenticationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AuthenticationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public void Factory_ShouldUseIntegrationDatabase()
        {
            // Arrange 
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Act
            var databaseName = dbContext.Database.GetDbConnection().Database;

            // Assert
            Assert.Equal("ProductApi_IntegrationTests", databaseName);
        }

        [Fact]
        public async Task GetProduct_WithoutToken_ShouldReturn401()
        {
            // Arrange
            const string endpoint = "/mediator/products/4";

            // Act
            var response = await _client.GetAsync(endpoint);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProduct_WithValidToken_ShouldNotReturn401()
        {
            // Arrange
            const string endpoint = "/mediator/products/4";

            using var scope = _factory.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
            var token = tokenService.GenerateToken(
                userId: "user-123",
                username: "integration-test",
                role: "viewer",
                permissions: ["Products.Read"]);

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
