using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

public sealed class ProductOwnerHandler
    : AuthorizationHandler<ProductOwnerRequirement, Product>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProductOwnerRequirement requirement,
        Product resource)
    {
        var userId = context.User.FindFirstValue("sub");
        if(!string.IsNullOrWhiteSpace(userId) && userId == resource.CreatedBy)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}