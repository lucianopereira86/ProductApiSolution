using System.Security.Claims;

public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;
}