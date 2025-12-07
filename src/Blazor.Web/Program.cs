using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.Web;
using Blazor.Web.Options;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new(builder.HostEnvironment.BaseAddress)
});

builder.Services.Configure<RemoteAuthenticationOptions<SpotifyProviderOptions>>(builder.Configuration.GetSection(SpotifyProviderOptions.SectionName));

builder.Services
    .AddScoped<IPostConfigureOptions<RemoteAuthenticationOptions<SpotifyProviderOptions>>, DefaultSpotifyProviderOptionsConfiguration>();

builder.Services.AddRemoteAuthentication<RemoteAuthenticationState, RemoteUserAccount, SpotifyProviderOptions>();

await builder.Build().RunAsync();