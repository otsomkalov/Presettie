module Functions.API.Startup

#nowarn "20"

open System
open System.Reflection
open System.Security.Claims
open System.Text.Json
open System.Text.Json.Serialization
open Azure.Core
open Azure.Identity
open Domain
open Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Azure.Functions.Worker.Builder
open Microsoft.Azure.Functions.Worker.Middleware
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
    [<Literal>]
    let SectionName = "Auth"

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
  |> Startup.addInfrastructure cfg
  |> Startup.addAuthCore cfg
  |> Startup.addSpotifyAuth

  services
    .AddAuthentication()
    .AddJwtBearer()

  services.AddLocalization()

  services
    .AddMvcCore()
    .AddJsonOptions(fun opts ->
      JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags().AddToJsonSerializerOptions(opts.JsonSerializerOptions))

  builder

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

let private configureFunctionsWebApp (builder: FunctionsApplicationBuilder) =
  builder.UseMiddleware<BindingErrorHandlerMiddleware>()

  builder

let private configureAppConfiguration (builder: FunctionsApplicationBuilder) =

  builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly())

  let credential: TokenCredential =
    if builder.Environment.IsDevelopment() then
      DefaultAzureCredential()
    else
      ManagedIdentityCredential()

  builder.Configuration.AddAzureKeyVault(
    Uri($"https://{builder.Configuration[Settings.KeyVault.KeyVaultName]}.vault.azure.net/"),
    credential
  )

  builder

let private configureWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts ->
    // JsonFSharpOptions.Default().WithUnionUntagged().AddToJsonSerializerOptions(opts)

    ())

  ()

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