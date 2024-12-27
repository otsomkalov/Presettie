module Generator.Startup

#nowarn "20"

open System
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open API.Services
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.ApplicationInsights

let private configureServices (builderContext: HostBuilderContext) (services: IServiceCollection) : unit =

  services.AddApplicationInsightsTelemetryWorkerService()
  services.ConfigureFunctionsApplicationInsights()

  let configuration = builderContext.Configuration

  services.Configure<JWTSettings>(configuration.GetSection(JWTSettings.SectionName))

  services.AddSingleton<IJWTService, JWTService>()
  |> Infrastructure.Startup.addInfrastructure configuration

  services.AddLocalization()

  services
    .AddMvcCore()
    .AddJsonOptions(fun opts ->
      JsonFSharpOptions
        .Default()
        .WithUnionUnwrapFieldlessTags()
        .AddToJsonSerializerOptions(opts.JsonSerializerOptions))

  ()

let private configureAppConfiguration _ (configBuilder: IConfigurationBuilder) =

  configBuilder.AddUserSecrets(Assembly.GetExecutingAssembly())

  ()

let private configureWebApp (builder: IFunctionsWorkerApplicationBuilder) =
  builder.Services.Configure<JsonSerializerOptions>(fun opts ->
    JsonFSharpOptions
      .Default()
      .WithUnionUnwrapFieldlessTags()
      .AddToJsonSerializerOptions(opts))

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