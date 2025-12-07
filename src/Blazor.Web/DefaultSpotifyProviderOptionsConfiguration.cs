using Blazor.Web.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace Blazor.Web;

public class DefaultSpotifyProviderOptionsConfiguration(NavigationManager navManager) : IPostConfigureOptions<RemoteAuthenticationOptions<SpotifyProviderSettings>>
{
    public void PostConfigure(string? name, RemoteAuthenticationOptions<SpotifyProviderSettings> options)
    {
        options.UserOptions.AuthenticationType ??= options.ProviderOptions.ClientId;

        var redirectUri = options.ProviderOptions.RedirectUri;
        if (redirectUri == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            redirectUri ??= "authentication/login-callback";
            options.ProviderOptions.RedirectUri = navManager
                .ToAbsoluteUri(redirectUri).AbsoluteUri;
        }

        var logoutUri = options.ProviderOptions.PostLogoutRedirectUri;
        if (logoutUri == null || !Uri.TryCreate(logoutUri, UriKind.Absolute, out _))
        {
            logoutUri ??= "authentication/logout-callback";
            options.ProviderOptions.PostLogoutRedirectUri = navManager
                .ToAbsoluteUri(logoutUri).AbsoluteUri;
        }

    }
}