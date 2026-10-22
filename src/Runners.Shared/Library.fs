namespace Runners.Shared

open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Threading.Tasks
open Domain.Core
open FsToolkit.ErrorHandling
open MusicPlatform
open Shared

type IncludeArtistRequest = { Id: Artist.RawArtistId }
type IncludePlaylistRequest = { Id: Playlist.RawPlaylistId }

type CurrentUserResponse =
  {
    Id: UserId
    CurrentPresetId: PresetId option
    MusicPlatformId: MusicPlatform.UserId option
  }

type IApiClient =
  abstract ListPresets: unit -> Task<SimplePreset list>
  abstract RemovePreset: PresetId -> TaskResult<unit, Preset.GetPresetError>
  abstract RunPreset: PresetId -> TaskResult<SimplePreset, Preset.ValidationError list>
  abstract IncludeArtist: PresetId * IncludeArtistRequest -> TaskResult<IncludedArtist, IncludeArtist.Error>
  abstract IncludePlaylist: PresetId * IncludePlaylistRequest -> TaskResult<IncludedPlaylist, IncludePlaylist.Error>

type ApiClient(httpClient: HttpClient) =
  interface IApiClient with
    member this.ListPresets() = task { return! httpClient.GetFromJsonAsync<SimplePreset list>("presets", JSON.options) }

    member this.RemovePreset(PresetId presetId) = taskResult {
      use request = new HttpRequestMessage(HttpMethod.Delete, $"presets/{presetId}")

      use! response = httpClient.SendAsync request

      match response.StatusCode with
      | HttpStatusCode.NoContent -> return ()
      | HttpStatusCode.NotFound -> return! Error(Preset.NotFound)
      | HttpStatusCode.Forbidden -> return! Error(Preset.Forbidden)
    }

    member this.RunPreset(PresetId presetId) = taskResult {
      use request = new HttpRequestMessage(HttpMethod.Post, $"presets/{presetId}/run")

      use! response = httpClient.SendAsync request

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<SimplePreset>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors =
          response.Content.ReadFromJsonAsync<Preset.ValidationError list>(JSON.options)

        return! Error errors
    }

    member this.IncludeArtist(PresetId presetId, request) = taskResult {
      use! response =
        httpClient.PostAsJsonAsync($"presets/{presetId}/included-artists", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<IncludedArtist>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors = response.Content.ReadFromJsonAsync<IncludeArtist.Error>(JSON.options)

        return! Error errors
    }

    member this.IncludePlaylist(PresetId presetId, request) = taskResult {
      use! response =
        httpClient.PostAsJsonAsync($"presets/{presetId}/included-playlists", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<IncludedPlaylist>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors = response.Content.ReadFromJsonAsync<IncludePlaylist.Error>(JSON.options)

        return! Error errors
    }