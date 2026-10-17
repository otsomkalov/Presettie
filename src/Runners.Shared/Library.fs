namespace Runners.Shared

open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Domain.Core
open Microsoft.Extensions.Logging

module JSON =
  let settings = JsonFSharpOptions.Default().WithUnionUnwrapFieldlessTags()

  let options =
    let options = settings.ToJsonSerializerOptions()

    options.PropertyNameCaseInsensitive <- true

    options

  let serialize value =
    JsonSerializer.Serialize(value, options)

  let deserialize<'a> (json: string) =
    JsonSerializer.Deserialize<'a>(json, options)

type CurrentUserResponse =
  {
    Id: UserId
    CurrentPresetId: PresetId option
    MusicPlatformId: MusicPlatform.UserId option
  }

type IApiClient =
  abstract ListPresets: unit -> Task<SimplePreset list>

type ApiClient(httpClient: HttpClient, logger: ILogger<ApiClient>) =
  interface IApiClient with
    member this.ListPresets() = task {
      try
        return! httpClient.GetFromJsonAsync<SimplePreset list>("presets", JSON.options)
      with e ->
        logger.LogError(e, "Error while fetching presets")
        return []
    }