using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.Web;
using Blazor.Web.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.Configure<RemoteAuthenticationOptions<SpotifyProviderSettings>>(builder.Configuration.GetSection(SpotifyProviderSettings.SectionName));

builder.Services
    .AddScoped<IPostConfigureOptions<RemoteAuthenticationOptions<SpotifyProviderSettings>>, DefaultSpotifyProviderOptionsConfiguration>();

builder.Services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, SpotifyProviderSettings>();

await builder.Build().RunAsync();