// For more information see https://aka.ms/fsharp-console-apps
open System
open System.Net.Http
open System.Net.Http.Json
open Domain.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Runners.Shared
open Spectre.Console

let services = ServiceCollection()
let configurationBuilder =
  ConfigurationBuilder().AddJsonFile("appsettings.json")

let configuration = configurationBuilder.Build()

let configureHttpClient (serviceProvider: IServiceProvider) (client: HttpClient) =
  let cfg = serviceProvider.GetRequiredService<IConfiguration>()

  client.BaseAddress <- Uri(cfg["API:Url"])

  client.DefaultRequestHeaders.Add("x-functions-key", cfg["API:Key"])

  ()

services.AddSingleton<IConfiguration>(configuration)

services.AddScoped<IEnv, Env>()

services.AddHttpClient(nameof Env, configureHttpClient)

let provider = services.BuildServiceProvider()

let env = provider.GetRequiredService<IEnv>()

let presetConverter (preset: SimplePreset) =
  preset.Name

task {
  let! presets = env.ListPresets()

  let prompt = SelectionPrompt().Title("Select preset").AddChoices(presets).UseConverter(presetConverter)

  let! selectedPreset = AnsiConsole.PromptAsync prompt

  AnsiConsole.MarkupLine($"Selected preset: {selectedPreset}")
}
|> Async.AwaitTask
|> Async.RunSynchronously