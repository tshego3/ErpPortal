// Core/Config/ApiSettings.cs
using System.ComponentModel.DataAnnotations;

namespace ErpPortal.Core.Config;

public sealed class ApiSettings
{
    public const string SectionName = "ApiSettings";

    [Required, Url]
    public string BaseUrl { get; init; } = string.Empty;
}
