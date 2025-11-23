module Bot.Handlers.Click

open System.Threading.Tasks
open Domain.Core
open Domain.Repos
open MusicPlatform
open Bot.Constants
open Bot.Core
open Bot.Workflows
open otsom.fs.Bot
open System
open otsom.fs.Extensions
open otsom.fs.Resources
open Domain.Core.PresetSettings
open Bot.Resources

let presetInfoClickHandler presetRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; id; "i" ] -> task {
        do! Preset.show presetRepo botService resp click.MessageId (PresetId id)
        return Some()
      }
    | _ -> Task.FromResult(None)

let listPresetsClickHandler userRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset ] -> task {
        do! User.listPresets resp botService userRepo click.MessageId click.Chat.UserId
        return Some()
      }
    | _ -> Task.FromResult(None)

let artistsAlbumsRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.artistsAlbumsRecommendations ] -> task {
        let presetId = PresetId presetId
        do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.ArtistAlbums)
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let reccoBeatsRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.reccoBeatsRecommendations ] -> task {
        let presetId = PresetId presetId
        do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.ReccoBeats)
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let spotifyRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.spotifyRecommendations ] -> task {
        let presetId = PresetId presetId
        do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.Spotify)
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let disableRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.disableRecommendations ] -> task {
        let presetId = PresetId presetId
        do! presetService.SetRecommendationsEngine(presetId, None)
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let enableUniqueArtistsClickHandler
  presetRepo
  (presetService: #IEnableUniqueArtists)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.enableUniqueArtists ] -> task {
        let presetId = PresetId presetId
        do! presetService.EnableUniqueArtists presetId
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let disableUniqueArtistsClickHandler
  presetRepo
  (presetService: #IDisableUniqueArtists)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.disableUniqueArtists ] -> task {
        let presetId = PresetId presetId
        do! presetService.DisableUniqueArtists presetId
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let includeLikedTracksClickHandler
  presetRepo
  (presetService: #IIncludeLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includeLikedTracks ] -> task {
        let presetId = (PresetId presetId)
        do! presetService.IncludeLikedTracks presetId
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let excludeLikedTracksClickHandler
  presetRepo
  (presetService: #IExcludeLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludeLikedTracks ] -> task {
        let presetId = (PresetId presetId)
        do! presetService.ExcludeLikedTracks presetId
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let ignoreLikedTracksClickHandler
  presetRepo
  (presetService: #IIgnoreLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.ignoreLikedTracks ] -> task {
        let presetId = (PresetId presetId)
        do! presetService.IgnoreLikedTracks presetId
        do! botService.SendNotification(click.Id, resp[Notifications.Updated])
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedPlaylistClickHandler
  (presetRepo: #ILoadPreset)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "i" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeIncludedPlaylistClickHandler (presetService: #IRemoveIncludedPlaylist) (resp: IResourceProvider) (botService: #ISendNotification & #IEditMessageButtons) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        match! presetService.RemoveIncludedPlaylist(presetId, (ReadablePlaylistId playlistId)) with
        | Ok preset ->
          do! IncludedPlaylist.list resp botService click.MessageId preset (Page 0)
          return Some()
        | Error Preset.RemoveIncludedPlaylistError.NotIncluded ->
          do! botService.SendNotification(click.Id, resp[Notifications.IncludedPlaylistNotIncluded]) |> Task.ignore
          return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedPlaylistClickHandler
  (presetRepo: #ILoadPreset)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  (botService: #IEditMessageButtons)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; playlistId; "i" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! ExcludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeExcludedPlaylistClickHandler (presetService: #IRemoveExcludedPlaylist) (resp: IResourceProvider) (botService: #ISendNotification)  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        match! presetService.RemoveExcludedPlaylist(presetId, (ReadablePlaylistId playlistId)) with
        | Ok preset ->
          do! ExcludedPlaylist.list resp botService click.MessageId preset (Page 0)
          return Some()
        | Error Preset.RemoveExcludedPlaylistError.NotExcluded ->
          do! botService.SendNotification(click.Id, resp[Notifications.PlaylistNotExcluded]) |> Task.ignore

          return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedArtistClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) (botService: #IEditMessageButtons) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; artistId; "i" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId

        do! ExcludedArtist.show resp botService presetRepo click.MessageId presetId artistId

        return Some()
      }
    | _ -> Task.FromResult(None)

let removeExcludedArtistClickHandler (presetService: #IRemoveExcludedArtist) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; artistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = ArtistId artistId
        match! presetService.RemoveExcludedArtist(presetId, playlistId) with
        | Ok preset ->
          do! ExcludedArtist.list resp botService click.MessageId preset (Page 0)
          return Some()
        | Error Preset.RemoveExcludedArtistError.NotExcluded ->
          do! botService.SendNotification(click.Id, resp[Notifications.ArtistNotExcluded]) |> Task.ignore

          return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedArtistClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) (botService: #IEditMessageButtons) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; artistId; "i" ] -> task {
        let presetId = PresetId presetId
        let artistId = ArtistId artistId

        do! IncludedArtist.show resp botService presetRepo click.MessageId presetId artistId

        return Some()
      }
    | _ -> Task.FromResult(None)

let removeIncludedArtistClickHandler (presetService: #IRemoveIncludedArtist) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; artistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = ArtistId artistId
        match! presetService.RemoveIncludedArtist(presetId, playlistId) with
        | Ok preset ->
          do! IncludedArtist.list resp botService click.MessageId preset (Page 0)
          return Some()
        | Error Preset.RemoveIncludedArtistError.NotIncluded ->
          do! botService.SendNotification(click.Id, resp[Notifications.ArtistNotIncluded]) |> Task.ignore

          return Some()
      }
    | _ -> Task.FromResult(None)

let showTargetedPlaylistClickHandler
  presetRepo
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId'; "tp"; playlistId'; "i" ] -> task {
        let presetId = PresetId presetId'
        let playlistId = PlaylistId playlistId'
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId (WritablePlaylistId playlistId)
        return Some()
      }
    | _ -> Task.FromResult(None)

let removeTargetedPlaylistClickHandler (presetService: #IRemoveTargetedPlaylist) (resp: IResourceProvider) (botService: #ISendNotification & #IEditMessageButtons) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "rm" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        match! presetService.RemoveTargetedPlaylist(presetId, (WritablePlaylistId playlistId)) with
        | Ok preset ->
          do! TargetedPlaylist.list resp botService click.MessageId preset (Page 0)
          return Some()
        | Error Preset.RemoveTargetedPlaylistError.NotTargeted ->
          do! botService.SendNotification(click.Id, resp[Notifications.TargetedPlaylistNotTargeted]) |> Task.ignore
          return Some()
      }
    | _ -> Task.FromResult(None)

let appendToTargetedPlaylistClickHandler
  presetRepo
  (presetService: #IAppendToTargetedPlaylist)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "a" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId |> WritablePlaylistId
        do! presetService.AppendToTargetedPlaylist(presetId, playlistId)
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let overwriteTargetedPlaylistClickHandler
  presetRepo
  (presetService: IPresetService)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; playlistId; "o" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId |> WritablePlaylistId
        do! presetService.OverwriteTargetedPlaylist(presetId, playlistId)
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let runPresetClickHandler
  (presetService: #Domain.Core.IQueueRun)
  (resp: IResourceProvider)
  (botService: #ISendMessage & #ISendNotification)
  : ClickHandler =
  let onSuccess clickId =
    fun (preset: Preset) -> task {
      do! botService.SendNotification(clickId, resp[Notifications.PresetQueued, [| preset.Name |]])

      do!
        botService.SendMessage(resp[Messages.PresetQueued, [| preset.Name |]])
        &|> ignore
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

  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "r" ] -> task {
        let presetId = PresetId presetId

        do!
          presetService.QueueRun(click.Chat.UserId, presetId)
          |> TaskResult.taskEither (onSuccess click.Id) onError

        return Some()
      }
    | _ -> Task.FromResult(None)

let setCurrentPresetClickHandler
  (userService: #ISetCurrentPreset)
  (resp: IResourceProvider)
  (chatService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; id; "c" ] -> task {
        let presetId = PresetId id
        do! userService.SetCurrentPreset(click.Chat.UserId, presetId)
        do! chatService.SendNotification(click.Id, resp[Notifications.CurrentPresetSet])
        return Some()
      }
    | _ -> Task.FromResult(None)

let presetSettingsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) (botService: #IEditMessageButtons) : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "s" ] -> task {
        let presetId = PresetId presetId
        do! PresetSettings.show presetRepo botService resp click.MessageId presetId
        return Some()
      }
    | _ -> Task.FromResult(None)

let listIncludedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)
        let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
        do! IncludedPlaylist.list resp botService click.MessageId preset page
        return Some()
      }
    | _ -> Task.FromResult(None)

let listExcludedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedPlaylists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)
        let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
        do! ExcludedPlaylist.list resp botService click.MessageId preset page
        return Some()
      }
    | _ -> Task.FromResult(None)

let listExcludedArtistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedArtists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)
        let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
        do! ExcludedArtist.list resp botService click.MessageId preset page
        return Some()
      }
    | _ -> Task.FromResult(None)

let listIncludedArtistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedArtists; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)
        let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
        do! IncludedArtist.list resp botService click.MessageId preset page
        return Some()
      }
    | _ -> Task.FromResult(None)

let listTargetedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "tp"; page ] -> task {
        let presetId = PresetId presetId
        let page = Page(int page)
        let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
        do! TargetedPlaylist.list resp botService click.MessageId preset page
        return Some()
      }
    | _ -> Task.FromResult(None)

let showIncludedContentClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedContent ] -> task {
        let presetId = PresetId presetId
        do! IncludedContent.show resp botService presetRepo click.MessageId presetId

        return Some()
      }
    | _ -> Task.FromResult(None)

let showExcludedContentClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.excludedContent ] -> task {
        let presetId = PresetId presetId
        do! ExcludedContent.show resp botService presetRepo click.MessageId presetId

        return Some()
      }
    | _ -> Task.FromResult(None)

let setOnlyLikedIncludedPlaylistClickHandler
  presetRepo
  (presetService: #ISetOnlyLiked)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "o" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! presetService.SetOnlyLiked(presetId, (playlistId |> ReadablePlaylistId))
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let setAllTracksIncludedPlaylistClickHandler
  presetRepo
  (presetService: #ISetAll)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; CallbackQueryConstants.includedPlaylists; playlistId; "a" ] -> task {
        let presetId = PresetId presetId
        let playlistId = PlaylistId playlistId
        do! presetService.SetAll(presetId, (playlistId |> ReadablePlaylistId))
        let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
        do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId
        return Some()
      }
    | _ -> Task.FromResult(None)

let removePresetClickHandler
  presetRepo
  (userService: #IRemoveUserPreset)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click ->
    match click.Data with
    | [ CallbackQueryConstants.preset; presetId; "rm" ] -> task {
        let presetId = RawPresetId presetId

        match! userService.RemoveUserPreset(click.Chat.UserId, presetId) with
        | Ok _ ->
          do! botService.SendNotification(click.Id, resp[Notifications.PresetRemoved])
          do! User.listPresets resp botService presetRepo click.MessageId click.Chat.UserId
          return Some()
        | Error Preset.GetPresetError.NotFound ->
          do! botService.SendNotification(click.Id, resp[Notifications.PresetNotFound])
          return Some()
      }
    | _ -> Task.FromResult(None)