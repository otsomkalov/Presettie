module Telegram.Handlers.Click

open Domain.Core
open MusicPlatform
open Telegram.Constants
open Telegram.Core
open Telegram.Repos
open Telegram.Workflows

let presetInfoClickHandler presetRepo botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; id; "i" |] ->
      let sendPresetInfo = Preset.show presetRepo botMessageCtx

      do! sendPresetInfo (PresetId id)

      return Some()
    | _ -> return None
  }

let showPresetsClickHandler getUser (chatRepo: #ILoadChat) botMessageCtx : ClickHandler =
  let listUserPresets = User.showPresets botMessageCtx getUser

  fun click -> task {
    match click.Data.Split("|") with
    | [| "p" |] ->
      let! chat = chatRepo.LoadChat click.ChatId

      do! listUserPresets chat.UserId

      return Some()
    | _ -> return None
  }

let enableRecommendationsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.enableRecommendations |] ->
      let enableRecommendations =
        Domain.Workflows.PresetSettings.enableRecommendations presetRepo

      let enableRecommendations =
        PresetSettings.enableRecommendations presetRepo botMessageCtx enableRecommendations (showNotification click.Id)

      do! enableRecommendations (PresetId presetId)

      return Some()
    | _ -> return None
  }

let disableRecommendationsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.disableRecommendations |] ->
      let disableRecommendations =
        Domain.Workflows.PresetSettings.disableRecommendations presetRepo

      let disableRecommendations =
        PresetSettings.disableRecommendations presetRepo botMessageCtx disableRecommendations (showNotification click.Id)

      do! disableRecommendations (PresetId presetId)

      return Some()
    | _ -> return None
  }

let enableUniqueArtistsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.enableUniqueArtists |] ->
      let enableUniqueArtists =
        Domain.Workflows.PresetSettings.enableUniqueArtists presetRepo

      let enableUniqueArtists =
        PresetSettings.enableUniqueArtists presetRepo botMessageCtx enableUniqueArtists (showNotification click.Id)

      do! enableUniqueArtists (PresetId presetId)

      return Some()
    | _ -> return None
  }

let disableUniqueArtistsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.disableUniqueArtists |] ->
      let disableUniqueArtists =
        Domain.Workflows.PresetSettings.disableUniqueArtists presetRepo

      let disableUniqueArtists =
        PresetSettings.disableUniqueArtists presetRepo botMessageCtx disableUniqueArtists (showNotification click.Id)

      do! disableUniqueArtists (PresetId presetId)

      return Some()
    | _ -> return None
  }

let includeLikedTracksClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.includeLikedTracks |] ->
      let includeLikedTracks =
        Domain.Workflows.PresetSettings.includeLikedTracks presetRepo

      let includeLikedTracks =
        PresetSettings.includeLikedTracks presetRepo botMessageCtx (showNotification click.Id) includeLikedTracks

      do! includeLikedTracks (PresetId presetId)

      return Some()
    | _ -> return None
  }

let excludeLikedTracksClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.excludeLikedTracks |] ->
      let excludeLikedTracks =
        Domain.Workflows.PresetSettings.excludeLikedTracks presetRepo

      let excludeLikedTracks =
        PresetSettings.excludeLikedTracks presetRepo botMessageCtx (showNotification click.Id) excludeLikedTracks

      do! excludeLikedTracks (PresetId presetId)

      return Some()
    | _ -> return None
  }

let ignoreLikedTracksClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.ignoreLikedTracks |] ->
      let ignoreLikedTracks = Domain.Workflows.PresetSettings.ignoreLikedTracks presetRepo

      let ignoreLikedTracks =
        PresetSettings.ignoreLikedTracks presetRepo botMessageCtx (showNotification click.Id) ignoreLikedTracks

      do! ignoreLikedTracks (PresetId presetId)

      return Some()
    | _ -> return None
  }

let removeIncludedPlaylistClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; "ip"; playlistId; "rm" |] ->
      let removeIncludedPlaylist = Domain.Workflows.IncludedPlaylist.remove presetRepo

      let removeIncludedPlaylist =
        IncludedPlaylist.remove presetRepo botMessageCtx removeIncludedPlaylist (showNotification click.Id)

      do! removeIncludedPlaylist (PresetId presetId) (PlaylistId playlistId |> ReadablePlaylistId)

      return Some()
    | _ -> return None
  }

let removeExcludedPlaylistClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; "ep"; playlistId; "rm" |] ->
      let removeExcludedPlaylist = Domain.Workflows.ExcludedPlaylist.remove presetRepo

      let removeExcludedPlaylist =
        ExcludedPlaylist.remove presetRepo botMessageCtx removeExcludedPlaylist (showNotification click.Id)

      do! removeExcludedPlaylist (PresetId presetId) (PlaylistId playlistId |> ReadablePlaylistId)

      return Some()
    | _ -> return None
  }

let removeTargetedPlaylistClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; "tp"; playlistId; "rm" |] ->
      let removeTargetedPlaylist = Domain.Workflows.TargetedPlaylist.remove presetRepo

      let removeTargetedPlaylist =
        TargetedPlaylist.remove presetRepo botMessageCtx removeTargetedPlaylist (showNotification click.Id)

      do! removeTargetedPlaylist (PresetId presetId) (PlaylistId playlistId |> WritablePlaylistId)

      return Some()
    | _ -> return None
  }