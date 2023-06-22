﻿module Generator.Bot.Telegram

open System.Threading.Tasks
open Database
open Domain.Core
open Domain.Workflows
open Infrastructure.Core
open Infrastructure.Helpers
open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Bot.Types.ReplyMarkups
open Microsoft.EntityFrameworkCore

type SendUserPresets = UserId -> Task<unit>
type SendPresetInfo = int -> UserId -> PresetId -> Task<unit>
type SetCurrentPreset = string -> UserId -> PresetId -> Task<unit>

let sendUserPresets (bot: ITelegramBotClient) (listPresets: User.ListPresets) : SendUserPresets =
  fun userId ->
    task {
      let! presets = listPresets userId |> Async.StartAsTask

      let keyboardMarkup =
        presets
        |> Seq.map (fun p -> InlineKeyboardButton(p.Name, CallbackData = $"p|{p.Id |> PresetId.value}|i"))
        |> InlineKeyboardMarkup

      do!
        bot.SendTextMessageAsync((userId |> UserId.value |> ChatId), "Your presets", replyMarkup = keyboardMarkup)
        |> Task.map ignore
    }

let sendPresetInfo (bot: ITelegramBotClient) (loadPreset: User.LoadPreset) : SendPresetInfo =
  fun messageId userId presetId ->
    task {
      let! preset = loadPreset presetId |> Async.StartAsTask

      let keyboardMarkup =
        [ InlineKeyboardButton("Generate", CallbackData = $"p|{presetId |> PresetId.value}|g") ]
        |> InlineKeyboardMarkup

      do!
        bot.EditMessageTextAsync(
          (userId |> UserId.value |> ChatId),
          messageId,
          $"Your preset info: {preset.Name}",
          replyMarkup = keyboardMarkup
        )
        |> Task.map ignore
    }

let setCurrentPreset (bot: ITelegramBotClient) (context: AppDbContext) : SetCurrentPreset =
  fun callbackQueryId userId presetId ->
    task {
      let userId = userId |> UserId.value
      let presetId = presetId |> PresetId.value

      let! user = context.Users.FirstOrDefaultAsync(fun u -> u.Id = userId)

      user.CurrentPresetId <- presetId

      user |> context.Update |> ignore

      do! context.SaveChangesAsync() |> Task.map ignore

      return! bot.AnswerCallbackQueryAsync(callbackQueryId, "Current playlist id successfully set!")
    }
