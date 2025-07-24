module Generator.Startup

#nowarn "20"

open System
open System.Text.Json
open System.Text.Json.Serialization
open System.Reflection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Microsoft.Azure.Functions.Worker

let private configureServices (builderContext: HostBuilderContext) (services: IServiceCollection) : unit =

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  let cfg = builderContext.Configuration

  services
  |> Domain.Startup.addDomain cfg
  |> MusicPlatform.Spotify.Startup.addSpotifyMusicPlatform cfg
  |> MusicPlatform.Cached.Startup.addCachedMusicPlatform cfg
  |> Telegram.Startup.addBot cfg
  |> Infrastructure.Startup.addInfrastructure cfg
  |> Infrastructure.Telegram.Startup.addTelegram cfg

  services.AddLocalization()

  services.ConfigureTelegramBotMvc()

  ()

let private configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly())

  ()

let private configureWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts -> JsonFSharpOptions.Default().AddToJsonSerializerOptions(opts))

  ()

let private configureLogging (builder: ILoggingBuilder) =
  builder.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  ()

let host =
  HostBuilder()
    .ConfigureFunctionsWebApplication(configureWebApp)
    .ConfigureAppConfiguration(configureAppConfiguration)
    .ConfigureLogging(configureLogging)
    .ConfigureServices(configureServices)
    .Build()

host.Run()