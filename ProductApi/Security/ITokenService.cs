public interface ITokenService
{
    string GenerateToken(string userId, string username, string role, IEnumerable<string> permissions);
}