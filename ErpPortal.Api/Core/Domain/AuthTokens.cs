using System.Text.Json.Serialization;

namespace ErpPortal.Api.Core.Domain;

/// <summary>
/// Represents the token pair returned by DummyJSON's /auth/login endpoint.
/// </summary>
public sealed record AuthTokens(
    [property: JsonPropertyName("accessToken")]  string AccessToken,
    [property: JsonPropertyName("refreshToken")] string RefreshToken
);
