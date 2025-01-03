module Telegram.Handlers.Click

open Domain.Core
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