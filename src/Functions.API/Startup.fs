module Functions.API.Startup

#nowarn "20"

open System
open System.Reflection
open System.Security.Claims
open System.Text.Json
open System.Text.Json.Serialization
open Domain
open Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Azure.Functions.Worker.Middleware
open Microsoft.Extensions.Logging.Console
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
    .AddAuthentication()
    .AddJwtBearer(fun opts ->
      opts.TokenValidationParameters.NameClaimType <- ClaimTypes.NameIdentifier

      ())

  services.AddLocalization()

  services
    .AddMvcCore()
    .AddJsonOptions(fun opts ->
      JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags().AddToJsonSerializerOptions(opts.JsonSerializerOptions))

  ()

type BindingErrorHandlerMiddleware(logger: ILogger<BindingErrorHandlerMiddleware>) =
  interface IFunctionsWorkerMiddleware with
    member this.Invoke(context, next) = task {
      try
        return! next.Invoke context
      with
      | :? InvalidOperationException as ioe ->
        logger.LogWarning(ioe, String.Empty)

        let! request = context.GetHttpRequestDataAsync()
        let response = request.CreateResponse()

        response.StatusCode <- System.Net.HttpStatusCode.BadRequest
        response.Headers.Add("Content-Type", "application/json")

        do! JsonSerializer.SerializeAsync(response.Body, {| Error = ioe.Message |})

        ()
      | e -> logger.LogError(e, "Error")
    }

let private configureFunctionsWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.UseMiddleware<BindingErrorHandlerMiddleware>()

  ()

let private configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly())

  ()

let private configureWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts ->
    // JsonFSharpOptions.Default().WithUnionUntagged().AddToJsonSerializerOptions(opts)

    ())

  ()

let private configureLogging (builder: ILoggingBuilder) =
  builder.AddFilter<ApplicationInsightsLoggerProvider>(String.Empty, LogLevel.Information)

  ()

let host =
  HostBuilder()
    .ConfigureFunctionsWebApplication(configureFunctionsWebApp)
    .ConfigureAppConfiguration(configureAppConfiguration)
    .ConfigureLogging(configureLogging)
    .ConfigureServices(configureServices)
    .Build()

host.Run()