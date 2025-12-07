using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace Blazor.Web.Options;

public class DefaultSpotifyProviderOptionsConfiguration(NavigationManager navManager)
    : IPostConfigureOptions<RemoteAuthenticationOptions<SpotifyProviderOptions>>
{
    public void PostConfigure(string? name, RemoteAuthenticationOptions<SpotifyProviderOptions> options)
    {
        options.UserOptions.AuthenticationType ??= options.ProviderOptions.ClientId;

        var redirectUri = options.ProviderOptions.RedirectUri;

        if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            redirectUri ??= "authentication/login-callback";

            options.ProviderOptions.RedirectUri = navManager
                .ToAbsoluteUri(redirectUri)
                .AbsoluteUri;
        }

        var logoutUri = options.ProviderOptions.PostLogoutRedirectUri;

        if (logoutUri == null || !Uri.TryCreate(logoutUri, UriKind.Absolute, out _))
        {
            logoutUri ??= "authentication/logout-callback";

            options.ProviderOptions.PostLogoutRedirectUri = navManager
                .ToAbsoluteUri(logoutUri)
                .AbsoluteUri;
        }
    }
}

public class SpotifyProviderOptions
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