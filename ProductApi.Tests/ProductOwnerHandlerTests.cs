using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ProductApi.Tests;

public class ProductOwnerHandlerTests
{
    [Theory]
    [InlineData("user-123", "user-123", true)]
    [InlineData("user-456", "user-123", false)]
    [InlineData(null, "user-123", false)]
    [InlineData("", "user-123", false)]
    public async Task ProductOwner_ShouldAuthorizeOnlyOwner(
       string? userId,
       string createdBy,
       bool expected)
    {
        // Arrange & Act
        var context = await AuthorizeAsync(
            userId,
            createdBy);

        // Assert
        Assert.Equal(expected, context.HasSucceeded);
    }
    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(string? userId, string createdBy)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : [new Claim("sub", userId)];

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "Tests");

        var user = new ClaimsPrincipal(identity);

        var product = new Product(
            createdBy,
            "product 1",
            1,
            1);

        var requirement = new ProductOwnerRequirement();

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            product);

        var handler = new ProductOwnerHandler();

        await handler.HandleAsync(context);

        return context;
    }
}
