namespace Runners.Shared

open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Domain.Core
open FsToolkit.ErrorHandling
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
  abstract RemovePreset: PresetId -> TaskResult<unit, Preset.GetPresetError>

type ApiClient(httpClient: HttpClient, logger: ILogger<ApiClient>) =
  interface IApiClient with
    member this.ListPresets() = task {
      try
        return! httpClient.GetFromJsonAsync<SimplePreset list>("presets", JSON.options)
      with e ->
        logger.LogError(e, "Error while fetching presets")
        return []
    }

    member this.RemovePreset(PresetId presetId) = taskResult {
      use request = new HttpRequestMessage(HttpMethod.Delete, $"presets/{presetId}")

      try
        use! response = httpClient.SendAsync request

        match response.StatusCode with
        | HttpStatusCode.NoContent -> return ()
        | HttpStatusCode.NotFound -> return! Error(Preset.NotFound)
        | HttpStatusCode.Forbidden -> return! Error(Preset.Forbidden)
        | _ ->
          logger.LogWarning("Unexpected status code {StatusCode}", response.StatusCode)

          return ()
      with e ->
        logger.LogError(e, "Error while removing preset")

        return ()
    }