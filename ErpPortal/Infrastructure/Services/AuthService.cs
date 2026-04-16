using System.Security.Claims;
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly IErpHttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationService _notifier;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IErpHttpClient http,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notifier,
        ILogger<AuthService> logger)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<User> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for {Username}", username);
        try
        {
            User user = await _http.PostAsync<User>("/auth/login",
                new { username, password }, ct);

            List<Claim> claims =
            [
                new(ClaimTypes.Name,  user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FirstName",      user.FirstName),
                new("LastName",       user.LastName),
                new("Token",          user.Token ?? string.Empty),
            ];

            ClaimsIdentity identity   = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal  = new(identity);

            await _httpContextAccessor.HttpContext!.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(1),
                });

            _notifier.ShowSuccess("Welcome", $"Logged in as {user.FirstName} {user.LastName}");
            _logger.LogInformation("Login successful for {Username}", username);
            return user;
        }
        catch (AppException ex)
        {
            string message = ex.StatusCode switch
            {
                401 => "Invalid username or password.",
                403 => "Login request was blocked by the upstream service.",
                0   => "Could not reach the authentication service.",
                _   => $"Login failed ({ex.Code}).",
            };

            _notifier.ShowError("Login Failed", message);
            _logger.LogWarning("Login failed for {Username}. Code: {Code}, Status: {Status}", username, ex.Code, ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            _notifier.ShowError("Login Failed", "An unexpected error occurred during login.");
            _logger.LogError(ex, "Unexpected error during login for {Username}", username);
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _notifier.ShowInfo("Logged Out", "You have been safely signed out.");
        _logger.LogInformation("User logged out");
    }

    public Task<User?> GetCurrentUserAsync()
    {
        HttpContext? ctx = _httpContextAccessor.HttpContext;
        if (ctx?.User.Identity?.IsAuthenticated is not true) return Task.FromResult<User?>(null);

        User user = new User(
            Id:        0,
            Username:  ctx.User.Identity.Name ?? string.Empty,
            Email:     ctx.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            FirstName: ctx.User.FindFirst("FirstName")?.Value ?? string.Empty,
            LastName:  ctx.User.FindFirst("LastName")?.Value ?? string.Empty,
            Image:     string.Empty,
            Token:     ctx.User.FindFirst("Token")?.Value);

        return Task.FromResult<User?>(user);
    }

    public Task<bool> IsAuthenticatedAsync()
        => Task.FromResult(_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated is true);
}
