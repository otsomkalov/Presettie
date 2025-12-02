module Bolero.Web.Startup

open System
open System.Net.Http
open Domain.Core
open Microsoft.AspNetCore.Components
open Microsoft.AspNetCore.Components.WebAssembly.Authentication
open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Bolero.Web.Repos
open System.Net.Http.Json
open Bolero.Web.Util
open otsom.fs.Extensions
open FsToolkit.ErrorHandling

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

    member this.GetPreset'(RawPresetId presetId) = task {
      try
        return! httpClient.GetFromJsonAsync<Preset>($"api/presets/{presetId}", JSON.options)
      with e ->
        logger.LogError(e, "Error while fetching preset")
        return Unchecked.defaultof<Preset>
    }

    member this.RemovePreset(PresetId presetId) = task {
      try
        do! httpClient.DeleteAsync($"api/presets/{presetId}") |> Task.ignore
      with e ->
        logger.LogError(e, "Error while removing preset")
    }

    member this.CreatePreset(name) = task {
      try
        use! response = httpClient.PostAsJsonAsync("api/presets", {| Name = name |})

        return!
          response.Content.ReadFromJsonAsync<{| Id: string |}>()
          |> Task.map (_.Id >> PresetId)
      with e ->
        logger.LogError(e, "Error while creating preset")

        return Unchecked.defaultof<PresetId>
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