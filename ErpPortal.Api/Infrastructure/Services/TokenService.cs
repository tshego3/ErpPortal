// Infrastructure/Services/TokenService.cs
using System.Net.Http.Json;
using System.Text.Json;
using ErpPortal.Api.Core.Config;
using ErpPortal.Api.Core.Contracts;
using ErpPortal.Api.Core.Domain;
using Microsoft.Extensions.Options;

namespace ErpPortal.Api.Infrastructure.Services;

/// <summary>
/// Singleton service that acquires a DummyJSON JWT via /auth/login,
/// caches both tokens in memory, and transparently refreshes via /auth/refresh
/// before the access token expires.
///
/// This is the .NET equivalent of an Axios interceptor that silently
/// refreshes a token on 401 — but proactive rather than reactive.
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DummyJsonSettings _settings;
    private readonly ILogger<TokenService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public TokenService(
        IHttpClientFactory httpClientFactory,
        IOptions<DummyJsonSettings> settings,
        ILogger<TokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: token is still valid (with 60-second safety margin)
        if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _expiresAt)
        {
            return _accessToken;
        }

        // Serialize access: only one thread can acquire/refresh at a time
        await _semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock
            if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _expiresAt)
            {
                return _accessToken;
            }

            // Try refresh first if we have a refresh token
            if (_refreshToken is not null)
            {
                try
                {
                    await RefreshAsync(ct);
                    return _accessToken!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Token refresh failed — falling back to full login");
                }
            }

            // Full login
            await LoginAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task InvalidateAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _expiresAt = DateTimeOffset.MinValue;
        _logger.LogInformation("Token cache invalidated");
        return Task.CompletedTask;
    }

    private async Task LoginAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Authenticating to DummyJSON as {Username}", _settings.Username);

        using HttpClient http = _httpClientFactory.CreateClient("DummyJsonRaw");

        HttpResponseMessage response = await http.PostAsJsonAsync(
            "/auth/login",
            new
            {
                username = _settings.Username,
                password = _settings.Password,
                expiresInMins = _settings.TokenExpiryMinutes,
            },
            _jsonOptions,
            ct);

        response.EnsureSuccessStatusCode();

        AuthTokens tokens = await response.Content
            .ReadFromJsonAsync<AuthTokens>(_jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/login");

        _accessToken  = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;
        _expiresAt    = DateTimeOffset.UtcNow.AddMinutes(_settings.TokenExpiryMinutes);

        _logger.LogInformation(
            "DummyJSON login successful. Token expires at {ExpiresAt:u}", _expiresAt);
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing DummyJSON access token");

        using HttpClient http = _httpClientFactory.CreateClient("DummyJsonRaw");

        HttpResponseMessage response = await http.PostAsJsonAsync(
            "/auth/refresh",
            new
            {
                refreshToken = _refreshToken,
                expiresInMins = _settings.TokenExpiryMinutes,
            },
            _jsonOptions,
            ct);

        response.EnsureSuccessStatusCode();

        AuthTokens tokens = await response.Content
            .ReadFromJsonAsync<AuthTokens>(_jsonOptions, ct)
            ?? throw new InvalidOperationException("Null response from /auth/refresh");

        _accessToken  = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;
        _expiresAt    = DateTimeOffset.UtcNow.AddMinutes(_settings.TokenExpiryMinutes);

        _logger.LogInformation(
            "Token refreshed. New expiry: {ExpiresAt:u}", _expiresAt);
    }
}
