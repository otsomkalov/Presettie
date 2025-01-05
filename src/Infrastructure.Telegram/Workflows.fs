[<RequireQualifiedAccess>]
module Infrastructure.Telegram.Workflows

open MusicPlatform
open Domain.Core
open Telegram.Bot
open Telegram.Core
open Infrastructure.Telegram.Helpers

let answerCallbackQuery (bot: ITelegramBotClient) callbackQueryId : AnswerCallbackQuery =
  fun () ->
    task {
      do! bot.AnswerCallbackQueryAsync(callbackQueryId)

      return ()
    }

let showNotification (bot: ITelegramBotClient) : ShowNotification =
  fun (ClickId clickId) text ->
    task {
      do! bot.AnswerCallbackQueryAsync(clickId, text)

      return ()
    }

let parseAction: ParseAction =
  fun (str: string) ->
    match str.Split("|") with
    | [| "p"; id; "c" |] -> PresetId id |> Action.SetCurrentPreset
    | [| "p"; id; "rm" |] -> PresetId id |> Action.RemovePreset
    | [| "p"; id; "r" |] -> PresetId id |> PresetActions.Run |> Action.Preset

    | [| "p"; id; "ip"; Int page |] ->
      IncludedPlaylistActions.List(PresetId id, (Page page)) |> Action.IncludedPlaylist

    | [| "p"; id; "ep"; Int page |] ->
      ExcludedPlaylistActions.List(PresetId id, (Page page)) |> Action.ExcludedPlaylist

    | [| "p"; id; "tp"; Int page |] -> TargetedPlaylistActions.List(PresetId id, (Page page)) |> Action.TargetedPlaylist
