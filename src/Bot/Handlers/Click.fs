module Bot.Handlers.Click

open System.Threading.Tasks
open Domain.Core
open MusicPlatform
open Bot.Constants
open Bot.Core
open Bot.Workflows
open Runners.Shared
open otsom.fs.Bot
open System
open otsom.fs.Extensions
open otsom.fs.Resources
open Domain.Core.PresetSettings
open Bot.Resources
open FsToolkit.ErrorHandling

let presetInfoClickHandler apiClient (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; id; "i" ] -> task {
        do! Preset.show apiClient botService resp click.Id click.MessageId (PresetId id)
        return Some()
      }
    | _ -> Task.FromResult(None)

let listPresetsClickHandler apiClient (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset ] -> task {
        do! User.listPresets resp apiClient botService click.MessageId ()
        return Some()
      }
    | _ -> Task.FromResult(None)

let artistsAlbumsRecommendationsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.artistsAlbumsRecommendations ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchRecommendationsEngine(Some RecommendationsEngine.ArtistAlbums) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let reccoBeatsRecommendationsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.reccoBeatsRecommendations ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchRecommendationsEngine(Some RecommendationsEngine.ReccoBeats) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let musicaeRecommendationsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.musicaeRecommendations ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchRecommendationsEngine(Some RecommendationsEngine.Musicae) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let spotifyRecommendationsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.spotifyRecommendations ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchRecommendationsEngine(Some RecommendationsEngine.Spotify) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let disableRecommendationsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.disableRecommendations ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchRecommendationsEngine(None) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let enableUniqueArtistsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.enableUniqueArtists ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchUniqueArtists(true) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let disableUniqueArtistsClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.disableUniqueArtists ] -> task {
        let presetId = PresetId presetId

        match! apiClient.PatchPresetSettings(presetId, [ PatchUniqueArtists(false) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let includeLikedTracksClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includeLikedTracks ] -> task {
        let presetId = (PresetId presetId)

        match! apiClient.PatchPresetSettings(presetId, [ PatchLikedTracksHandling(LikedTracksHandling.Include) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let excludeLikedTracksClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludeLikedTracks ] -> task {
        let presetId = (PresetId presetId)

        match! apiClient.PatchPresetSettings(presetId, [ PatchLikedTracksHandling(LikedTracksHandling.Exclude) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let ignoreLikedTracksClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botClient: #ISendNotification & #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.ignoreLikedTracks ] -> task {
        let presetId = (PresetId presetId)

        match! apiClient.PatchPresetSettings(presetId, [ PatchLikedTracksHandling(LikedTracksHandling.Ignore) ]) with
        | Ok preset ->
          do! botClient.SendNotification(click.Id, resp[Notifications.Updated])
          let text, keyboard = getPresetSettingsMessage resp preset
          do! botClient.EditMessageButtons(click.MessageId, text, keyboard)
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botClient click.Id e
          return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "i" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! IncludedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeIncludedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId

        let! preset = apiClient.RemoveIncludedPlaylist(presetId, playlistId)

        do! IncludedPlaylist.list resp botService click.MessageId preset (Page 0)
        return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedPlaylistClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botService: #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; playlistId; "i" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! ExcludedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeExcludedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId

        let! preset = apiClient.RemoveExcludedPlaylist(presetId, playlistId)

        do! ExcludedPlaylist.list resp botService click.MessageId preset (Page 0)
        return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedArtistClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botService: #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; artistId; "i" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId

        do! ExcludedArtist.show resp botService apiClient click.Id click.MessageId presetId artistId

        return Some()
      }
    | _ -> Task.FromResult(None)

let removeExcludedArtistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; artistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId
        let! preset = apiClient.RemoveExcludedArtist(presetId, artistId)

        do! ExcludedArtist.list resp botService click.MessageId preset (Page 0)

        return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedArtistClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botService: #IEditMessageButtons)
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; artistId; "i" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId

        do! IncludedArtist.show resp botService apiClient click.Id click.MessageId presetId artistId

        return Some()
      }
    | _ -> Task.FromResult(None)

let removeIncludedArtistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; artistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId
        let! preset = apiClient.RemoveIncludedArtist(presetId, artistId)

        do! IncludedArtist.list resp botService click.MessageId preset (Page 0)

        return Some()
      }
    | _ -> Task.FromResult(None)

let showTargetedPlaylistClickHandler apiClient (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId'; "tp"; playlistId'; "i" ] -> task {
        let presetId = PresetId presetId'
        let playlistId = PlaylistId playlistId'
        do! TargetedPlaylist.show resp botService apiClient click.Id click.MessageId presetId (WritablePlaylistId playlistId)
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeTargetedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId

        let! preset = apiClient.RemoveTargetedPlaylist(presetId, playlistId)

        do! TargetedPlaylist.list resp botService click.MessageId preset (Page 0)
        return Some()
      }
    | _ -> Task.FromResult(None)

let appendToTargetedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "a" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId |> WritablePlaylistId

        do!
          apiClient.PatchTargetedPlaylist(presetId, playlistId, [ PatchOverwrite false ])
          |> Task.ignore

        do! TargetedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let overwriteTargetedPlaylistClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "o" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId |> WritablePlaylistId

        do!
          apiClient.PatchTargetedPlaylist(presetId, playlistId, [ PatchOverwrite true ])
          |> Task.ignore

        do! TargetedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let runPresetClickHandler
  (apiClient: IApiClient)
  (resp: IResourceProvider)
  (botService: #ISendMessage & #ISendNotification)
  : ClickHandler<Chat> =
  let onSuccess clickId =
    fun (preset: SimplePreset) -> task {
      do! botService.SendNotification(clickId, resp[Notifications.PresetQueued, [| preset.Name |]])

      do!
        botService.SendMessage(resp[Messages.PresetQueued, [| preset.Name |]])
        |> Task.map ignore
    }

  let onError =
    fun errors ->
      let errorsText =
        errors
        |> Seq.map (function
          | Preset.ValidationError.NoIncludedPlaylists -> resp[Messages.NoIncludedPlaylists]
          | Preset.ValidationError.NoTargetedPlaylists -> resp[Messages.NoTargetedPlaylists])
        |> String.concat Environment.NewLine

      botService.SendMessage errorsText |> Task.ignore

  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "r" ] -> task {
        let presetId = PresetId presetId

        do!
          apiClient.RunPreset(presetId)
          |> TaskResult.taskEither (onSuccess click.Id) onError

        return Some()
      }
    | _ -> Task.FromResult(None)

let setCurrentPresetClickHandler (apiClient: IApiClient) (resp: IResourceProvider) (chatService: #ISendNotification) : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; id; "c" ] -> task {
        let presetId = PresetId id
        do! apiClient.PatchCurrentUser([ PatchCurrentPreset(Some presetId) ]) |> Task.ignore
        do! chatService.SendNotification(click.Id, resp[Notifications.CurrentPresetSet])
        return Some()
      }
    | _ -> Task.FromResult(None)

let presetSettingsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) (botService: #IEditMessageButtons) : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "s" ] -> task {
        let presetId = PresetId presetId
        do! PresetSettings.show apiClient botService resp click.Id click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let listIncludedPlaylistsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)

        match! apiClient.GetPreset(presetId) with
        | Ok preset ->

          do! IncludedPlaylist.list resp botService click.MessageId preset page
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botService click.Id e

          return Some()
      }
    | _ -> Task.FromResult(None)

let listExcludedPlaylistsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)

        match! apiClient.GetPreset(presetId) with
        | Ok preset ->
          do! ExcludedPlaylist.list resp botService click.MessageId preset page
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botService click.Id e

          return Some()
      }
    | _ -> Task.FromResult(None)

let listExcludedArtistsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)

        match! apiClient.GetPreset(presetId) with
        | Ok preset ->
          do! ExcludedArtist.list resp botService click.MessageId preset page
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botService click.Id e

          return Some()
      }
    | _ -> Task.FromResult(None)

let listIncludedArtistsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)

        match! apiClient.GetPreset(presetId) with
        | Ok preset ->
          do! IncludedArtist.list resp botService click.MessageId preset page
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botService click.Id e

          return Some()
      }
    | _ -> Task.FromResult(None)

let listTargetedPlaylistsClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)

        match! apiClient.GetPreset(presetId) with
        | Ok preset ->
          do! TargetedPlaylist.list resp botService click.MessageId preset page
          return Some()
        | Error e ->
          do! sendPresetErrorNotification resp botService click.Id e

          return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedContentClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedContent ] -> task {
        let presetId = PresetId presetId
        do! IncludedContent.show resp botService apiClient click.Id click.MessageId presetId

        return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedContentClickHandler (apiClient: IApiClient) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedContent ] -> task {
        let presetId = PresetId presetId
        do! ExcludedContent.show resp botService apiClient click.Id click.MessageId presetId

        return Some()
      }
    | _ -> Task.FromResult(None)

let setOnlyLikedIncludedPlaylistClickHandler
  apiClient
  (presetService: #ISetOnlyLiked)
  (resp: IResourceProvider)
  botService
  : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "o" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! presetService.SetOnlyLiked(presetId, (playlistId |> ReadablePlaylistId))
        do! IncludedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let setAllTracksIncludedPlaylistClickHandler apiClient (presetService: #ISetAll) (resp: IResourceProvider) botService : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "a" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! presetService.SetAll(presetId, (playlistId |> ReadablePlaylistId))
        do! IncludedPlaylist.show resp botService apiClient click.Id click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removePresetClickHandler (apiClient: IApiClient) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler<Chat> =
  fun chat click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "rm" ] -> task {
        let presetId = PresetId presetId

        match! apiClient.RemovePreset(presetId) with
        | Ok() ->
          do! botService.SendNotification(click.Id, resp[Notifications.PresetRemoved])
          do! User.listPresets resp apiClient botService click.MessageId ()
          return Some()
        | Error Preset.GetPresetError.NotFound ->
          do! botService.SendNotification(click.Id, resp[Notifications.PresetNotFound])
          return Some()
        | Error Preset.GetPresetError.Forbidden ->
          do! botService.SendNotification(click.Id, resp[Notifications.PresetAccessForbidden])
          return Some()
      }
    | _ -> Task.FromResult(None)