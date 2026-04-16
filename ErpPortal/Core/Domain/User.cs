using System.Text.Json.Serialization;

namespace ErpPortal.Core.Domain;

/// <summary>
/// Immutable domain entity. Replaces the Zod-inferred TypeScript type.
/// Records give value-equality semantics for free.
/// </summary>
public sealed record User(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Image,
    // DummyJSON returns the token as "accessToken" — JsonPropertyName maps it to this property
    [property: JsonPropertyName("accessToken")] string? Token = null
);
