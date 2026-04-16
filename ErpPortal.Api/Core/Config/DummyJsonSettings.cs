// Core/Config/DummyJsonSettings.cs
using System.ComponentModel.DataAnnotations;

namespace ErpPortal.Api.Core.Config;

public sealed class DummyJsonSettings
{
    public const string SectionName = "DummyJson";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int TokenExpiryMinutes { get; init; } = 30;
}
