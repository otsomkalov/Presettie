module Functions.Bot.Startup

#nowarn "20"

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Reflection
open App
open Azure.Identity
open Bot
open Bot.Telegram
open Domain
open Infrastructure
open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.Azure.Functions.Worker
open MusicPlatform.Spotify
open Telegram.Bot.AspNetCore

[<RequireQualifiedAccess>]
module KeyVault =
  [<Literal>]
  let KeyVaultName = "KeyVaultName"

let private configureServices (builder: FunctionsApplicationBuilder) =

  let services, cfg = (builder.Services, builder.Configuration)

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  services
  |> Startup.addSpotifyMusicPlatform cfg
  |> Startup.addDomain cfg
  |> Startup.addApp
  |> Startup.addBot cfg
  |> Startup.addInfrastructure cfg
  |> Startup.addTelegram cfg

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