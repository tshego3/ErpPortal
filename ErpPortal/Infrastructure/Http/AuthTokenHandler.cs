using System.Net.Http.Headers;

namespace ErpPortal.Infrastructure.Http;

/// <summary>
/// Equivalent of the Axios request interceptor that injects "Authorization: Bearer ..."
/// Registered as a transient DelegatingHandler in Program.cs.
/// </summary>
/// <summary>
/// Historically injected "Authorization: Bearer ..." into outgoing requests.
/// Since the API Gateway (ErpPortal.Api) now manages the DummyJSON JWT lifecycle,
/// this handler is preserved as a no-op pass-through to maintain architecture
/// consistency, or can be used for Blazor-to-API-Gateway authentication.
/// </summary>
public sealed class AuthTokenHandler : DelegatingHandler
{
    /*
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTokenHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;
    */

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        /*
        string? token = _httpContextAccessor.HttpContext?.User.FindFirst("Token")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        */
        return await base.SendAsync(request, cancellationToken);
    }
}
