module Functions.Bot.Startup

#nowarn "20"

open System
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open System.Reflection
open App
open Azure.Core.Serialization
open Azure.Identity
open Bot
open Bot.Telegram
open Bot.Workflows
open Domain
open Infrastructure
open Infrastructure.Core
open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.Azure.Functions.Worker
open MusicPlatform
open MusicPlatform.Cached
open MusicPlatform.Spotify
open Shared
open Telegram.Bot.AspNetCore
open otsom.fs.OAuth
open otsom.fs.OAuth.Keycloak
open otsom.fs.OAuth.Storage.Mongo
open otsom.fs.Extensions.DependencyInjection

[<RequireQualifiedAccess>]
module KeyVault =
  [<Literal>]
  let KeyVaultName = "KeyVaultName"

let configureHttpClient (serviceProvider: IServiceProvider) (client: HttpClient) =
  let cfg = serviceProvider.GetRequiredService<IConfiguration>()

  client.BaseAddress <- Uri(cfg["API:Url"])

  client.DefaultRequestHeaders.Add("x-functions-key", cfg["API:Key"])

  ()

let private configureServices (builder: FunctionsApplicationBuilder) =

  let services, cfg = (builder.Services, builder.Configuration)

  services.Configure<WorkerOptions>(fun (opts: WorkerOptions) ->
    opts.Serializer <- JsonObjectSerializer(JSON.options)

    ())

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  services.Configure<AdditionalOAuthSettings>(cfg.GetSection(AdditionalOAuthSettings.SectionName))

  services.AddOAuth().AddKeycloak(cfg).AddMongoStore()

  services.AddSingleton<IMusicPlatformFactory, SpotifyMusicPlatformFactory>()

  services
  |> Startup.addSpotifyMusicPlatform cfg
  |> Startup.addCachedMusicPlatform cfg
  |> Startup.addDomain cfg
  |> Startup.addApp
  |> Startup.addBot cfg
  |> Startup.addInfrastructure cfg
  |> Startup.addTelegram cfg

  builder.Services.AddHttpClient<ApiClientFactory>(configureHttpClient)
  builder.Services.BuildScoped<IApiClientFactory, ApiClientFactory>(fun f -> f :> IApiClientFactory)

  services.ConfigureTelegramBotMvc()

  builder

let private configureAppConfiguration (builder: FunctionsApplicationBuilder) =
  builder.Configuration.AddAzureKeyVault(
    Uri($"https://{builder.Configuration[KeyVault.KeyVaultName]}.vault.azure.net/"),
    DefaultAzureCredential()
  )

  if builder.Environment.IsDevelopment() then
    do builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly())

  builder

let private configureFunctionsWebApp (builder: FunctionsApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts -> JsonFSharpOptions.Default().AddToJsonSerializerOptions opts)

  builder

let private configureLogging (builder: FunctionsApplicationBuilder) =
  builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  builder

let builder =
  FunctionsApplication.CreateBuilder(Environment.GetCommandLineArgs() |> Array.tail).ConfigureFunctionsWebApplication()
  |> configureAppConfiguration
  |> configureFunctionsWebApp
  |> configureLogging
  |> configureServices

let host = builder.Build()

host.RunAsync() |> Async.AwaitTask |> Async.RunSynchronously