using System.ComponentModel.DataAnnotations;

namespace ErpPortal.Core.Config;

public sealed class BrandingConfig
{
    public const string SectionName = "Branding";

    [Required] public string CompanyName  { get; init; } = "Enterprise ERP";
    [Required, Url] public string LogoUrl { get; init; } = string.Empty;
    public string PrimaryColor   { get; init; } = "#0052cc";
    public string SecondaryColor { get; init; } = "#172b4d";
    public string AccentColor    { get; init; } = "#ffab00";
}
