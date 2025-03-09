module Telegram.Handlers.Click

open Domain.Core
open Domain.Repos
open MusicPlatform
open Resources
open Telegram.Constants
open Telegram.Core
open Telegram.Workflows
open otsom.fs.Bot
open System
open otsom.fs.Extensions
open otsom.fs.Resources

let presetInfoClickHandler presetRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; id; "i" ] ->
      do! Preset.show presetRepo botService click.MessageId (PresetId id)

      return Some()
    | _ -> return None
  }

let listPresetsClickHandler userRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p" ] ->
      do! User.listPresets botService userRepo click.MessageId click.Chat.UserId

      return Some()
    | _ -> return None
  }

let enableRecommendationsClickHandler presetRepo (presetService: #IEnableRecommendations) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.enableRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.EnableRecommendations presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let disableRecommendationsClickHandler
  presetRepo
  (presetService: #IDisableRecommendations)
  (resp: IResourceProvider)
  (botService: #ISendNotification)
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.disableRecommendations ] ->
      let presetId = PresetId presetId

      do! presetService.DisableRecommendations presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let enableUniqueArtistsClickHandler presetRepo (presetService: #IEnableUniqueArtists) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.enableUniqueArtists ] ->
      let presetId = PresetId presetId

      do! presetService.EnableUniqueArtists presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let disableUniqueArtistsClickHandler presetRepo (presetService: #IDisableUniqueArtists) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.disableUniqueArtists ] ->
      let presetId = PresetId presetId

      do! presetService.DisableUniqueArtists presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let includeLikedTracksClickHandler presetRepo (presetService: #IIncludeLikedTracks) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.includeLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.IncludeLikedTracks presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let excludeLikedTracksClickHandler presetRepo (presetService: #IExcludeLikedTracks) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.excludeLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.ExcludeLikedTracks presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let ignoreLikedTracksClickHandler presetRepo (presetService: #IIgnoreLikedTracks) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; CallbackQueryConstants.ignoreLikedTracks ] ->
      let presetId = (PresetId presetId)

      do! presetService.IgnoreLikedTracks presetId
      do! botService.SendNotification(click.Id, Messages.Updated)
      do! Preset.show presetRepo botService click.MessageId presetId

      return Some()
    | _ -> return None
  }

let showIncludedPlaylistClickHandler (presetRepo: #ILoadPreset) buildMusicPlatform (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "i" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let removeIncludedPlaylistClickHandler presetRepo (presetService: #IRemoveIncludedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "rm" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.RemoveIncludedPlaylist(presetId, (ReadablePlaylistId playlistId))
      do! IncludedPlaylist.list presetRepo botService click.MessageId presetId (Page 0)

      return Some()
    | _ -> return None
  }

let showExcludedPlaylistClickHandler (presetRepo: #ILoadPreset) (buildMusicPlatform: BuildMusicPlatform) (resp: IResourceProvider) (botService: #IEditMessageButtons) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; playlistId; "i" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      let! preset = presetRepo.LoadPreset presetId

      let excludedPlaylist =
        preset.ExcludedPlaylists
        |> List.find (fun p -> p.Id = ReadablePlaylistId playlistId)

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

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
        String.Format(Messages.ExcludedPlaylistDetails, excludedPlaylist.Name, playlistTracksCount)

      let buttons = getPlaylistButtons presetId playlistId "ep" Seq.empty

      do! botService.EditMessageButtons(click.MessageId, messageText, buttons)

      return Some()
    | _ -> return None
  }

let removeExcludedPlaylistClickHandler presetRepo (presetService: #IRemoveExcludedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; playlistId; "rm" ] ->

      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.RemoveExcludedPlaylist(presetId, (ReadablePlaylistId playlistId))
      do! ExcludedPlaylist.list presetRepo botService click.MessageId presetId (Page 0)

      return Some()
    | _ -> return None
  }

let showTargetedPlaylistClickHandler presetRepo buildMusicPlatform (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId'; "tp"; playlistId'; "i" ] ->
      let presetId = PresetId presetId'
      let playlistId = PlaylistId playlistId'

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

      do! TargetedPlaylist.show botService presetRepo mp click.MessageId presetId (WritablePlaylistId playlistId)

      return Some()
    | _ -> return None
  }

let removeTargetedPlaylistClickHandler presetRepo (presetService: #IRemoveTargetedPlaylist) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "rm" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.RemoveTargetedPlaylist(presetId, (WritablePlaylistId playlistId))
      do! TargetedPlaylist.list presetRepo botService click.MessageId presetId (Page 0)

      return Some()
    | _ -> return None
  }

let appendToTargetedPlaylistClickHandler
  presetRepo
  (presetService: #IAppendToTargetedPlaylist)
  buildMusicPlatform
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "a" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId |> WritablePlaylistId

      do! presetService.AppendToTargetedPlaylist(presetId, playlistId)

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())
      do! TargetedPlaylist.show botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let overwriteTargetedPlaylistClickHandler presetRepo (presetService: IPresetService) buildMusicPlatform (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; playlistId; "o" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId |> WritablePlaylistId

      do! presetService.OverwriteTargetedPlaylist(presetId, playlistId)

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

      do! TargetedPlaylist.show botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let runPresetClickHandler (presetService: #Domain.Core.IQueueRun) (resp: IResourceProvider) (botService: #ISendMessage & #ISendNotification) : ClickHandler =
  let onSuccess clickId =
    fun (preset: Preset) -> task {
      let notificationMessage = $"Preset *{preset.Name}* run is queued!"

      do! botService.SendNotification(clickId, notificationMessage)
      do! botService.SendMessage(notificationMessage) &|> ignore
    }

  let onError =
    fun errors ->
      let errorsText =
        errors
        |> Seq.map (function
          | Preset.ValidationError.NoIncludedPlaylists -> "No included playlists!"
          | Preset.ValidationError.NoTargetedPlaylists -> "No targeted playlists!")
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

let setCurrentPresetClickHandler (userService: #ISetCurrentPreset) (resp: IResourceProvider) (chatService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; id; "c" ] ->
      let presetId = PresetId id

      do! userService.SetCurrentPreset(click.Chat.UserId, presetId)

      do! chatService.SendNotification(click.Id, "Current preset is successfully set!")

      return Some()
    | _ -> return None
  }

let listIncludedPlaylistsClickHandler presetRepo (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      do! IncludedPlaylist.list presetRepo botService click.MessageId presetId page

      return Some()
    | _ -> return None
  }

let listExcludedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ep"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      do! ExcludedPlaylist.list presetRepo botService click.MessageId presetId page

      return Some()
    | _ -> return None
  }

let listTargetedPlaylistsClickHandler (presetRepo: #ILoadPreset) (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "tp"; page ] ->
      let presetId = PresetId presetId
      let page = Page(int page)

      do! TargetedPlaylist.list presetRepo botService click.MessageId presetId page

      return Some()
    | _ -> return None
  }

let setOnlyLikedIncludedPlaylistClickHandler
  presetRepo
  (presetService: #ISetOnlyLiked)
  buildMusicPlatform
  (resp: IResourceProvider)
  botService
  : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "o" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.SetOnlyLiked(presetId, (playlistId |> ReadablePlaylistId))

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let setAllTracksIncludedPlaylistClickHandler presetRepo (presetService: #ISetAll) buildMusicPlatform (resp: IResourceProvider) botService : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "ip"; playlistId; "a" ] ->
      let presetId = PresetId presetId
      let playlistId = PlaylistId playlistId

      do! presetService.SetAll(presetId, (playlistId |> ReadablePlaylistId))

      let! mp = buildMusicPlatform (click.Chat.UserId.ToMusicPlatformId())

      do! IncludedPlaylist.show botService presetRepo mp click.MessageId presetId playlistId

      return Some()
    | _ -> return None
  }

let removePresetClickHandler userRepo (userService: #IRemoveUserPreset) (resp: IResourceProvider) (botService: #ISendNotification) : ClickHandler =
  fun click -> task {
    match click.Data with
    | [ "p"; presetId; "rm" ] ->
      let presetId = PresetId presetId

      do! userService.RemoveUserPreset(click.Chat.UserId, presetId)
      do! botService.SendNotification(click.Id, Messages.PresetRemoved)
      do! User.listPresets botService userRepo click.MessageId click.Chat.UserId

      return Some()
    | _ -> return None
  }