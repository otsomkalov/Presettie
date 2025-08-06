module Web.Startup

open System
open System.Net.Http
open Domain.Core
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Web.Repos
open System.Net.Http.Json
open Web.Util

type Env(httpClientFactory: IHttpClientFactory, logger: ILogger<Env>) =
  let httpClient = httpClientFactory.CreateClient(nameof Env)

  interface IEnv with
    member this.ListPresets() = task {
      try
        return! httpClient.GetFromJsonAsync<SimplePreset list>("api/presets", JSON.options)
      with e ->
        logger.LogError(e, "Error while fetching presets")
        return []
    }

type APIAuthorizationMessageHandler(accessTokenProvider: IAccessTokenProvider, navigationManager: NavigationManager, cfg: IConfiguration) =
  inherit AuthorizationMessageHandler(accessTokenProvider, navigationManager)

  do base.ConfigureHandler([ cfg["API:Url"] ]) |> ignore

let configureHttpClient (serviceProvider: IServiceProvider) (client: HttpClient) =
  let cfg = serviceProvider.GetRequiredService<IConfiguration>()

  client.BaseAddress <- Uri(cfg["API:Url"])

  client.DefaultRequestHeaders.Add("x-functions-key", cfg["API:Key"])

  ()

let builder = WebAssemblyHostBuilder.CreateDefault()

builder.Services.AddOidcAuthentication(fun options ->

  builder.Configuration.Bind("Oidc", options.ProviderOptions)

  options.ProviderOptions.AdditionalProviderParameters.Add("audience", builder.Configuration["Oidc:Audience"])

  ())

builder.Services.AddScoped<APIAuthorizationMessageHandler>()

builder.Services.AddScoped<IEnv, Env>()

builder.Services.AddHttpClient(nameof Env, configureHttpClient).AddHttpMessageHandler<APIAuthorizationMessageHandler>()

builder.Logging.SetMinimumLevel(LogLevel.Information)

builder.RootComponents.Add<Main.App>("#main")

builder.Build().RunAsync()