namespace ErpPortal.Api.Core.Contracts;

/// <summary>
/// Manages the DummyJSON JWT lifecycle:
/// acquire on first call, cache in memory, refresh before expiry.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Returns a valid access token. Authenticates or refreshes automatically.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces a fresh login, discarding cached tokens.
    /// </summary>
    Task InvalidateAsync();
}
