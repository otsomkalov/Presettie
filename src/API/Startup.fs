module Generator.Startup

#nowarn "20"

open System
open System.Reflection
open System.Security.Claims
open System.Text.Json
open System.Text.Json.Serialization
open Domain
open Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open MusicPlatform.Spotify
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open otsom.fs.Auth
open otsom.fs.Auth.Spotify

[<RequireQualifiedAccess>]
module internal Settings =
  [<RequireQualifiedAccess>]
  module Auth =
    let [<Literal>] SectionName = "Auth"

let private configureServices (builderContext: HostBuilderContext) (services: IServiceCollection) : unit =

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  let cfg = builderContext.Configuration

  services
  |> Startup.addSpotifyMusicPlatform cfg
  |> Startup.addDomain cfg
  |> Startup.addInfrastructure cfg
  |> Startup.addAuthCore cfg
  |> Startup.addSpotifyAuth

  services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(fun opts ->
      cfg.GetSection(Settings.Auth.SectionName).Bind(opts)
      opts.TokenValidationParameters <- TokenValidationParameters(NameClaimType = ClaimTypes.NameIdentifier)

      ())

  services.AddLocalization()

  services
    .AddMvcCore()
    .AddJsonOptions(fun opts ->
      JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags().AddToJsonSerializerOptions(opts.JsonSerializerOptions))

  ()

let private configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly())

  ()

let private configureWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts ->
    JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags().AddToJsonSerializerOptions(opts))

  ()

let private configureLogging (builder: ILoggingBuilder) =
  builder.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  ()

let host =
  HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(configureAppConfiguration)
    .ConfigureLogging(configureLogging)
    .ConfigureServices(configureServices)
    .Build()

host.Run()