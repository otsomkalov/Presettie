using System.Text.Json.Serialization;

namespace Blazor.Web.Settings;

public class SpotifyProviderSettings
{
    public const string SectionName = "Auth";

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; }

    [JsonPropertyName("response_type")]
    public string ResponseType { get; set; }

    [JsonPropertyName("authority")]
    public string Authority { get; set; }

    [JsonPropertyName("metadataUrl")]
    public string MetadataUrl { get; set; }

    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    [JsonPropertyName("post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }
}