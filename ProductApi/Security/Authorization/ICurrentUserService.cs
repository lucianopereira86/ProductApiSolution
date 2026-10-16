using System.Security.Claims;

public interface ICurrentUserService
{
    string? UserId { get; }
    ClaimsPrincipal? User { get; }
}