namespace Runners.Shared

open System.ComponentModel.DataAnnotations
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
type ExcludePlaylistRequest = { Id: Playlist.RawPlaylistId }
type ExcludeArtistRequest = { Id: Artist.RawArtistId }
type TargetPlaylistRequest = { Id: Playlist.RawPlaylistId }

[<CLIMutable>]
type CreatePresetRequest =
  {
    [<Required; MinLength(3)>]
    Name: string
  }

type CurrentUserResponse =
  {
    Id: UserId
    CurrentPresetId: PresetId option
    MusicPlatformId: MusicPlatform.UserId option
  }

type IApiClient =
  abstract ListPresets: unit -> Task<SimplePreset list>
  abstract GetPreset: PresetId -> TaskResult<Preset, Preset.GetPresetError>
  abstract RemovePreset: PresetId -> TaskResult<unit, Preset.GetPresetError>
  abstract RunPreset: PresetId -> TaskResult<SimplePreset, Preset.ValidationError list>
  abstract IncludeArtist: PresetId * IncludeArtistRequest -> TaskResult<IncludedArtist, IncludeArtist.Error>
  abstract RemoveIncludedArtist: PresetId * ArtistId -> Task<Preset>
  abstract IncludePlaylist: PresetId * IncludePlaylistRequest -> TaskResult<IncludedPlaylist, IncludePlaylist.Error>
  abstract RemoveIncludedPlaylist: PresetId * PlaylistId -> Task<Preset>
  abstract ExcludePlaylist: PresetId * ExcludePlaylistRequest -> TaskResult<ExcludedPlaylist, ExcludePlaylist.Error>
  abstract RemoveExcludedPlaylist: PresetId * PlaylistId -> Task<Preset>
  abstract ExcludeArtist: PresetId * ExcludeArtistRequest -> TaskResult<ExcludedArtist, ExcludeArtist.Error>
  abstract RemoveExcludedArtist: PresetId * ArtistId -> Task<Preset>
  abstract TargetPlaylist: PresetId * TargetPlaylistRequest -> TaskResult<TargetedPlaylist, TargetPlaylist.Error>
  abstract RemoveTargetedPlaylist: PresetId * PlaylistId -> Task<Preset>
  abstract CreatePreset: CreatePresetRequest -> Task<Preset>
  abstract PatchPresetSettings: PresetId * PatchPresetSettingsCommand list -> TaskResult<Preset, Preset.GetPresetError>

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

    member this.RemoveIncludedArtist(PresetId presetId, ArtistId artistId) = task {
      use! response =
        httpClient.DeleteAsync($"presets/{presetId}/included-artists/{artistId}")

      return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
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

    member this.RemoveIncludedPlaylist(PresetId presetId, PlaylistId playlistId) = task {
      use! response =
        httpClient.DeleteAsync($"presets/{presetId}/included-playlists/{playlistId}")

      return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
    }

    member this.ExcludePlaylist(PresetId presetId, request) = taskResult {
      use! response =
        httpClient.PostAsJsonAsync($"presets/{presetId}/excluded-playlists", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<ExcludedPlaylist>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors = response.Content.ReadFromJsonAsync<ExcludePlaylist.Error>(JSON.options)

        return! Error errors
    }

    member this.RemoveExcludedPlaylist(PresetId presetId, PlaylistId playlistId) = task {
      use! response =
        httpClient.DeleteAsync($"presets/{presetId}/excluded-playlists/{playlistId}")

      return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
    }

    member this.ExcludeArtist(PresetId presetId, request) = taskResult {
      use! response =
        httpClient.PostAsJsonAsync($"presets/{presetId}/excluded-artists", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<ExcludedArtist>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors = response.Content.ReadFromJsonAsync<ExcludeArtist.Error>(JSON.options)

        return! Error errors
    }

    member this.RemoveExcludedArtist(PresetId presetId, ArtistId artistId) = task {
      use! response =
        httpClient.DeleteAsync($"presets/{presetId}/excluded-artists/{artistId}")

      return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
    }

    member this.TargetPlaylist(PresetId presetId, request) = taskResult {
      use! response =
        httpClient.PostAsJsonAsync($"presets/{presetId}/targeted-playlists", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<TargetedPlaylist>(JSON.options)
      | HttpStatusCode.BadRequest ->
        let! errors = response.Content.ReadFromJsonAsync<TargetPlaylist.Error>(JSON.options)

        return! Error errors
    }

    member this.RemoveTargetedPlaylist(PresetId presetId, PlaylistId playlistId) = task {
      use! response =
        httpClient.DeleteAsync($"presets/{presetId}/targeted-playlists/{playlistId}")

      return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
    }

    member this.CreatePreset(request) = task {
      use! response = httpClient.PostAsJsonAsync($"presets", request, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
    }

    member this.GetPreset(PresetId presetId) = taskResult {
      return! httpClient.GetFromJsonAsync<Preset>($"presets/{presetId}", JSON.options)
    }

    member this.PatchPresetSettings(PresetId presetId, patches) = taskResult {
      use! response =
        httpClient.PatchAsJsonAsync($"presets/{presetId}/settings", patches, JSON.options)

      match response.StatusCode with
      | HttpStatusCode.OK -> return! response.Content.ReadFromJsonAsync<Preset>(JSON.options)
      | HttpStatusCode.NotFound -> return! Error Preset.GetPresetError.NotFound
      | HttpStatusCode.Forbidden -> return! Error Preset.GetPresetError.Forbidden
    }