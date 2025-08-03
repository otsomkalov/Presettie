module Web.Startup

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

let builder = WebAssemblyHostBuilder.CreateDefault()

builder.Services.AddOidcAuthentication(fun options ->

  builder.Configuration.Bind("Oidc", options.ProviderOptions)

  ())

builder.Logging.SetMinimumLevel(LogLevel.Information)

builder.RootComponents.Add<Main.App>("#main")

builder.Build().RunAsync()