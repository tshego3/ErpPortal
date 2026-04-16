using ErpPortal.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace ErpPortal.Infrastructure.Http;

public sealed class ErrorHandlingHandler : DelegatingHandler
{
    private readonly ILogger<ErrorHandlingHandler> _logger;

    public ErrorHandlingHandler(ILogger<ErrorHandlingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[API Error] Network failure calling {Url}", request.RequestUri);
            throw new AppException(ex.Message, "NETWORK_ERROR", 0);
        }

        if (!response.IsSuccessStatusCode)
        {
            int status = (int)response.StatusCode;
            AppException appError = status switch
            {
                401 => new AppException("Unauthorized", "AUTH_401", 401),
                500 => new AppException("Server Error", "SERVER_500", 500),
                _   => new AppException($"HTTP {status}", $"HTTP_{status}", status),
            };

            _logger.LogError("[API Error] {Code} — {Url}", appError.Code, request.RequestUri);
            throw appError;
        }

        return response;
    }
}
