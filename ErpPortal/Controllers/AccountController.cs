using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ErpPortal.Core.Contracts;
using ErpPortal.Core.Domain;
using ErpPortal.Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Controllers;

[Route("account")]
public sealed class AccountController : Controller
{
    private readonly IErpHttpClient _http;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IErpHttpClient http, ILogger<AccountController> logger)
    {
        _http = http;
        _logger = logger;
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginForm model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Redirect("/login?error=invalid");

        try
        {
            int sessionMinutes = model.RememberMe ? 60 * 24 * 30 : 60;
            User user = await _http.PostAsync<User>(
                "/auth/login",
                new { username = model.Username, password = model.Password },
                ct);

            List<Claim> claims =
            [
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FirstName", user.FirstName),
                new("LastName", user.LastName),
                new("Token", user.Token ?? string.Empty),
            ];

            ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(1),
                });

            return Redirect("/dashboard");
        }
        catch (AppException ex)
        {
            string reason = ex.StatusCode switch
            {
                401 => "invalid",
                403 => "blocked",
                0 => "unreachable",
                _ => "failed",
            };

            return Redirect($"/login?error={reason}");
        }
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return Redirect("/login");
    }

    public sealed class LoginForm
    {
        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
