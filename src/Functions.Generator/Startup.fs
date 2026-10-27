module Functions.Generator.Startup

#nowarn "20"

open System
open System.Reflection
open App
open Azure.Identity
open Domain
open Infrastructure
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
open otsom.fs.OAuth
open otsom.fs.OAuth.Spotify
open otsom.fs.OAuth.Storage.Mongo

[<RequireQualifiedAccess>]
module KeyVault =
  [<Literal>]
  let KeyVaultName = "KeyVaultName"

let private configureServices (builder: FunctionsApplicationBuilder) =

  let services, cfg = (builder.Services, builder.Configuration)

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  services.AddOAuth().AddMongoStore().AddSpotify(cfg)

  services.AddSingleton<IMusicPlatformFactory, SpotifyOAuthMusicPlatformFactory>()

  services
  |> Startup.addDomain cfg
  |> Startup.addApp
  |> Startup.addSpotifyMusicPlatform cfg
  |> Startup.addCachedMusicPlatform cfg
  |> Startup.addInfrastructure cfg

  builder

let private configureAppConfiguration (builder: FunctionsApplicationBuilder) =
  builder.Configuration.AddAzureKeyVault(
    Uri($"https://{builder.Configuration[KeyVault.KeyVaultName]}.vault.azure.net/"),
    DefaultAzureCredential()
  )

  if builder.Environment.IsDevelopment() then
    do builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly())

  builder

let private configureLogging (builder: FunctionsApplicationBuilder) =
  builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  builder

let builder =
  FunctionsApplication.CreateBuilder(Environment.GetCommandLineArgs() |> Array.tail)
  |> configureAppConfiguration
  |> configureLogging
  |> configureServices

let host = builder.Build()

host.RunAsync() |> Async.AwaitTask |> Async.RunSynchronously