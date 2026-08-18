module Functions.Bot.Telegram.Mappers

open System
open Bot.Core
open Telegram.Bot.Types.Enums
open otsom.fs.Bot
open otsom.fs.Extensions

module Update =
  let private mapLang (user: Telegram.Bot.Types.User) =
    user.LanguageCode
    |> Option.ofObj
    |> Option.bind (Option.noneIf String.IsNullOrEmpty)

  let map (update: Telegram.Bot.Types.Update) : Update option =
    match update.Type with
    | UpdateType.Message when update.Message.Type = MessageType.Text ->
      let message = update.Message

      Some
        { ChatId = ChatId message.Chat.Id
          Lang = mapLang message.From
          Data =
            UpdateData.Msg
              { Id = ChatMessageId message.MessageId
                Text = message.Text
                ReplyMessage =
                  message.ReplyToMessage
                  |> Option.ofObj
                  |> Option.bind (Option.noneIf (_.Text >> String.IsNullOrEmpty))
                  |> Option.map (fun m -> { Text = m.Text }) } }
    | UpdateType.CallbackQuery ->
      let callbackQuery = update.CallbackQuery

      Some
        { ChatId = ChatId callbackQuery.Message.Chat.Id
          Lang = mapLang callbackQuery.From
          Data =
            UpdateData.Click
              { Id = callbackQuery.Id |> ButtonClickId
                MessageId = BotMessageId callbackQuery.Message.MessageId
                Data = callbackQuery.Data.Split("|") |> List.ofArray } }
    | _ -> None