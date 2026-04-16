using ErpPortal.Core.Domain;

namespace ErpPortal.Core.Contracts;

public interface IAuthService
{
    Task<User> LoginAsync(string username, string password, CancellationToken ct = default);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
