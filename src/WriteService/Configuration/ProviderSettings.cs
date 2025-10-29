namespace WriteService.Configuration;

public sealed class ProviderSettings
{
    public const string SectionName = "Providers";

    public ProviderEndpoint JsonProvider { get; set; } = new();
    public ProviderEndpoint XmlProvider { get; set; } = new();
}

public sealed class ProviderEndpoint
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
