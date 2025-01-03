module Telegram.Handlers.Click

open Domain.Core
open Domain.Repos
open Telegram.Constants
open Telegram.Core
open Telegram.Repos
open Telegram.Workflows

let presetInfoClickHandler getPreset botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; id; "i" |] ->
      let sendPresetInfo = Preset.show getPreset botMessageCtx

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
        PresetSettings.enableRecommendations presetRepo botMessageCtx enableRecommendations showNotification

      do! enableRecommendations (PresetId presetId)

      return Some()
    | _ ->
      return None
    }

let disableRecommendationsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.disableRecommendations |] ->
      let disableRecommendations =
        Domain.Workflows.PresetSettings.disableRecommendations presetRepo

      let disableRecommendations =
        PresetSettings.disableRecommendations presetRepo botMessageCtx disableRecommendations showNotification

      do! disableRecommendations (PresetId presetId)

      return Some()
    | _ ->
      return None
    }

let enableUniqueArtistsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.enableUniqueArtists |] ->
      let enableUniqueArtists =
        Domain.Workflows.PresetSettings.enableUniqueArtists presetRepo

      let enableUniqueArtists =
        PresetSettings.enableUniqueArtists presetRepo botMessageCtx enableUniqueArtists showNotification

      do! enableUniqueArtists (PresetId presetId)

      return Some()
    | _ ->
      return None
    }

let disableUniqueArtistsClickHandler presetRepo showNotification botMessageCtx : ClickHandler =
  fun click -> task {
    match click.Data.Split("|") with
    | [| "p"; presetId; CallbackQueryConstants.disableUniqueArtists |] ->
      let disableUniqueArtists =
        Domain.Workflows.PresetSettings.disableUniqueArtists presetRepo

      let disableUniqueArtists =
        PresetSettings.disableUniqueArtists presetRepo botMessageCtx disableUniqueArtists showNotification

      do! disableUniqueArtists (PresetId presetId)

      return Some()
    | _ ->
      return None
    }