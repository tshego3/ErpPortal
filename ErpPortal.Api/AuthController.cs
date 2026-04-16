using ErpPortal.Api.Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ErpPortal.Api.Controllers;

/// <summary>
/// Exposes a login endpoint that triggers the DummyJSON JWT acquisition.
/// Useful for health checks and explicit token refresh requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/auth/login — acquires a fresh DummyJSON token.
    /// Returns 200 with the token (for debugging/testing) or 500 on failure.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(CancellationToken ct)
    {
        try
        {
            string token = await _tokenService.GetAccessTokenAsync(ct);
            _logger.LogInformation("Token acquired via API login endpoint");
            return Ok(new { message = "Authenticated", tokenPreview = token[..20] + "..." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire DummyJSON token");
            return StatusCode(500, new { error = "Authentication failed", detail = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/auth/invalidate — forces token cache invalidation.
    /// </summary>
    [HttpPost("invalidate")]
    public async Task<IActionResult> Invalidate()
    {
        await _tokenService.InvalidateAsync();
        return Ok(new { message = "Token cache cleared" });
    }
}
