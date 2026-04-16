// Infrastructure/Http/DummyJsonAuthHandler.cs
using System.Net;
using System.Net.Http.Headers;
using ErpPortal.Api.Core.Contracts;

namespace ErpPortal.Api.Infrastructure.Http;

/// <summary>
/// DelegatingHandler that injects "Authorization: Bearer {token}" into
/// every request made by the typed DummyJsonClient.
///
/// If the upstream returns 401, the handler invalidates the cached token
/// and retries the request once with a fresh token — the "retry on 401" pattern.
/// </summary>
public sealed class DummyJsonAuthHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<DummyJsonAuthHandler> _logger;

    public DummyJsonAuthHandler(
        ITokenService tokenService,
        ILogger<DummyJsonAuthHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Attach the current access token
        string token = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // If 401 → token may have expired between our check and the upstream call.
        // Invalidate and retry exactly once.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(
                "Received 401 from {Url} — invalidating token and retrying",
                request.RequestUri);

            await _tokenService.InvalidateAsync();

            // Clone the request (original is disposed after first send)
            using HttpRequestMessage retryRequest = await CloneRequestAsync(request);
            string freshToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
            retryRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", freshToken);

            response = await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Clones an HttpRequestMessage because a sent message cannot be reused.
    /// </summary>
    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original)
    {
        HttpRequestMessage clone = new(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            byte[] content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            foreach (KeyValuePair<string, IEnumerable<string>> header
                in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key, header.Value);
            }
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header
            in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
