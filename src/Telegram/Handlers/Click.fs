module Telegram.Handlers.Click

open Domain.Core
open Telegram.Core
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