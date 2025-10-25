module MusicPlatform.ReccoBeats.Startup

#nowarn "20"

open System
open System.Net.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MusicPlatform

let configureHttpClient =
  fun (serviceProvider: IServiceProvider) (client: HttpClient) ->
    let settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value

    client.BaseAddress <- Uri settings.Url

    ()

let addReccoBeatsMusicPlatform (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<Settings>(cfg.GetSection Settings.SectionName)

  services.AddHttpClient(Settings.SectionName, configureHttpClient)

  services.AddKeyedSingleton<IRecommender, ReccoBeatsRecommender>("reccoBeats")