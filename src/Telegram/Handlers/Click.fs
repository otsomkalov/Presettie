module Telegram.Handlers.Click

open Domain.Core
open Domain.Repos
open MusicPlatform
open Telegram.Constants
open Telegram.Core
open Telegram.Workflows
open otsom.fs.Bot
open System
open otsom.fs.Extensions
open otsom.fs.Resources
open Domain.Core.PresetSettings
open Telegram.Resources

let presetInfoClickHandler presetRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; id; "i" ] ->
      do! Preset.show presetRepo botService resp click.MessageId (PresetId id)

      return Some()
    | _ -> return None
  }

let listPresetsClickHandler userRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p" ] ->
      do! User.listPresets resp botService userRepo click.MessageId click.Chat.UserId

      return Some()
    | _ -> return None
  }

let artistsAlbumsRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.artistsAlbumsRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.ArtistAlbums)
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let reccoBeatsRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.reccoBeatsRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.ReccoBeats)
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let spotifyRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.spotifyRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.SetRecommendationsEngine(presetId, Some RecommendationsEngine.Spotify)
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let disableRecommendationsClickHandler
  presetRepo
  (presetService: #ISetRecommendationsEngine)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.disableRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.SetRecommendationsEngine(presetId, None)
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let enableUniqueArtistsClickHandler
  presetRepo
  (presetService: #IEnableUniqueArtists)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.enableUniqueArtists ] ->
      let presetId = PresetId presetId

      do! presetService.EnableUniqueArtists presetId
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let disableUniqueArtistsClickHandler
  presetRepo
  (presetService: #IDisableUniqueArtists)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.disableUniqueArtists ] ->
      let presetId = PresetId presetId

      do! presetService.DisableUniqueArtists presetId
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let includeLikedTracksClickHandler
  presetRepo
  (presetService: #IIncludeLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.includeLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.IncludeLikedTracks presetId
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let excludeLikedTracksClickHandler
  presetRepo
  (presetService: #IExcludeLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.excludeLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.ExcludeLikedTracks presetId
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let ignoreLikedTracksClickHandler
  presetRepo
  (presetService: #IIgnoreLikedTracks)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.ignoreLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.IgnoreLikedTracks presetId
      do! botService.SendNotification(click.Id, resp[Messages.Updated])
      do! Preset.show presetRepo botService resp click.MessageId presetId

      return Some()
    | _ -> return None
  }

let showIncludedPlaylistClickHandler
  (presetRepo: #ILoadPreset)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "i" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let removeIncludedPlaylistClickHandler (presetService: #IRemoveIncludedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "rm" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! preset = presetService.RemoveIncludedPlaylist(presetId, (ReadablePlaylistId playlistId))
      do! IncludedPlaylist.list resp botService click.MessageId preset (Page 0)

      return Some()
    | _ -> return None
  }

let showExcludedPlaylistClickHandler
  (presetRepo: #ILoadPreset)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  (botService: #IEditMessageButtons)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; playlistId; "i" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! preset = presetRepo.LoadPreset presetId |> Task.map Option.get

      let excludedPlaylist =
        preset.ExcludedPlaylists
        |> List.find (fun p -> p.Id = ReadablePlaylistId playlistId)

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      let! playlistTracksCount =
        mp
        |> Option.taskMap (fun m -> m.LoadPlaylist playlistId)
        |> Task.map (
          Option.map (
            Result.map (function
              | Writable p -> p.TracksCount
              | Readable r -> r.TracksCount)
            >> Result.defaultValue 0
          )
        )

      let messageText =
        resp[Messages.ExcludedPlaylistDetails, [| excludedPlaylist.Name, playlistTracksCount |]]

      let buttons = getPlaylistButtons resp presetId playlistId "ep" Seq.empty

      do! botService.EditMessageButtons(click.MessageId, messageText, buttons)

      return Some()
    | _ -> return None
  }

let removeExcludedPlaylistClickHandler (presetService: #IRemoveExcludedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; playlistId; "rm" ] ->

      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! preset = presetService.RemoveExcludedPlaylist(presetId, (ReadablePlaylistId playlistId))
      do! ExcludedPlaylist.list resp botService click.MessageId preset (Page 0)

      return Some()
    | _ -> return None
  }

let showTargetedPlaylistClickHandler
  presetRepo
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId'; "tp"; playlistId'; "i" ] ->
      let presetId = PresetId presetId'
      let playlistId = PlaylistId playlistId'

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId (WritablePlaylistId playlistId)

      return Some()
    | _ -> return None
  }

let removeTargetedPlaylistClickHandler (presetService: #IRemoveTargetedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "rm" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! preset = presetService.RemoveTargetedPlaylist(presetId, (WritablePlaylistId playlistId))
      do! TargetedPlaylist.list resp botService click.MessageId preset (Page 0)

      return Some()
    | _ -> return None
  }

let appendToTargetedPlaylistClickHandler
  presetRepo
  (presetService: #IAppendToTargetedPlaylist)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "a" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId |> WritablePlaylistId

      do! presetService.AppendToTargetedPlaylist(presetId, playlistId)

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())
      do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let overwriteTargetedPlaylistClickHandler
  presetRepo
  (presetService: IPresetService)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "o" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId |> WritablePlaylistId

      do! presetService.OverwriteTargetedPlaylist(presetId, playlistId)

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      do! TargetedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let runPresetClickHandler
  (presetService: #Domain.Core.IQueueRun)
  (resp: IResourceProvider)
  (botService: #ISendMessage & #ISendNotification)
  : ClickHandler =
  let onSuccess clickId =
    fun (preset: Preset) -> task {
      do! botService.SendNotification(clickId, resp[Messages.PresetQueued, [| preset.Name |]])

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

      botService.SendMessage(errorsText) |> Task.ignore

  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "r" ] ->
      let presetId = PresetId presetId

      do!
        presetService.QueueRun(click.Chat.UserId, presetId)
        |> TaskResult.taskEither (onSuccess click.Id) onError

      return Some()
    | _ -> return None
  }

let setCurrentPresetClickHandler
  (userService: #ISetCurrentPreset)
  (resp: IResourceProvider)
  (chatService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; id; "c" ] ->
      let presetId = PresetId id

      do! userService.SetCurrentPreset(click.Chat.UserId, presetId)

      do! chatService.SendNotification(click.Id, resp[Messages.CurrentPresetSet])

      return Some()
    | _ -> return None
  }

let listIncludedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
      do! IncludedPlaylist.list resp botService click.MessageId preset page

      return Some()
    | _ -> return None
  }

let listExcludedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
      do! ExcludedPlaylist.list resp botService click.MessageId preset page

      return Some()
    | _ -> return None
  }

let listTargetedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      let! preset = presetRepo.LoadPreset(presetId) |> Task.map Option.get
      do! TargetedPlaylist.list resp botService click.MessageId preset page

      return Some()
    | _ -> return None
  }

let setOnlyLikedIncludedPlaylistClickHandler
  presetRepo
  (presetService: #ISetOnlyLiked)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "o" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.SetOnlyLiked(presetId, (playlistId |> ReadablePlaylistId))

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let setAllTracksIncludedPlaylistClickHandler
  presetRepo
  (presetService: #ISetAll)
  (musicPlatformFactory: IMusicPlatformFactory)
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "a" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.SetAll(presetId, (playlistId |> ReadablePlaylistId))

      let! mp = musicPlatformFactory.GetMusicPlatform(click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show resp botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let removePresetClickHandler
  presetRepo
  (userService: #IRemoveUserPreset)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "rm" ] ->
      let presetId = RawPresetId presetId

      match! userService.RemoveUserPreset(click.Chat.UserId, presetId) with
      | Ok _ ->
        do! botService.SendNotification(click.Id, resp[Messages.PresetRemoved])
        do! User.listPresets resp botService presetRepo click.MessageId click.Chat.UserId

        return Some()
      | Error Preset.GetPresetError.NotFound ->
        do! botService.SendNotification(click.Id, resp[Messages.PresetNotFound])

        return Some()
    | _ -> return None
  }