module Functions.API.Startup

#nowarn "20"

open System
open System.Net
open System.Reflection
open System.Security.Claims
open System.Text.Json
open System.Text.Json.Serialization
open Azure.Core.Serialization
open Azure.Identity
open Microsoft.AspNetCore.Http
open Microsoft.Azure.Functions.Worker.Http
open MusicPlatform
open MusicPlatform.Spotify
open Domain
open Domain.Core
open FsToolkit.ErrorHandling
open Infrastructure
open Infrastructure.Core
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Azure.Functions.Worker.Middleware
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights
open Shared
open otsom.fs.Extensions
open otsom.fs.OAuth
open otsom.fs.OAuth.Spotify
open otsom.fs.OAuth.Storage.Mongo
open App

[<RequireQualifiedAccess>]
module internal Settings =
  [<RequireQualifiedAccess>]
  module KeyVault =
    [<Literal>]
    let KeyVaultName = "KeyVaultName"

let private configureServices (builder: FunctionsApplicationBuilder) =
  let services, cfg = (builder.Services, builder.Configuration)

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  services.AddOAuth().AddMongoStore().AddSpotify(cfg)

  services.Configure<AdditionalOAuthSettings>(cfg.GetSection(AdditionalOAuthSettings.SectionName))

  services.AddSingleton<IMusicPlatformFactory, SpotifyOAuthMusicPlatformFactory>()

  services
  |> Startup.addSpotifyMusicPlatform cfg
  |> Startup.addDomain cfg
  |> Startup.addApp
  |> Startup.addInfrastructure cfg

  services
    .AddAuthentication()
    .AddJwtBearer(fun opts ->
      opts.TokenValidationParameters.NameClaimType <- ClaimTypes.NameIdentifier

      ())

  services.AddMvcCore().AddJsonOptions(_.JsonSerializerOptions >> JSON.settings.AddToJsonSerializerOptions)

  builder

type BindingErrorHandlerMiddleware(logger: ILogger<BindingErrorHandlerMiddleware>) =
  interface IFunctionsWorkerMiddleware with
    member this.Invoke(context, next) = task {
      match context.GetHttpContext() |> Option.ofObj with
      | Some httpContext ->

        try
          return! next.Invoke context
        with
        | :? InvalidOperationException as ioe ->
          logger.LogWarning(ioe, String.Empty)

          httpContext.Response.StatusCode <- int HttpStatusCode.BadRequest
          httpContext.Response.WriteAsJsonAsync({| Error = ioe.Message |}, JSON.options)

          ()
        | e -> logger.LogError(e, "Error")
      | None ->
        // Non HTTP function
        return! next.Invoke context
    }

let private configureFunctionsWebApp (builder: FunctionsApplicationBuilder) =
  builder.UseMiddleware<BindingErrorHandlerMiddleware>()

  builder

let private configureAppConfiguration (builder: FunctionsApplicationBuilder) =

  builder.Configuration.AddAzureKeyVault(
    Uri($"https://{builder.Configuration[Settings.KeyVault.KeyVaultName]}.vault.azure.net/"),
    DefaultAzureCredential()
  )

  if builder.Environment.IsDevelopment() then
    do builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly())

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