﻿namespace Bot.Services.Bot

open Telegram.Bot
open Telegram.Bot.Types

type EmptyCommandDataHandler(_bot: ITelegramBotClient) =
  member this.HandleAsync(message: Message) =
    task {
      let! _ =
        (ChatId(message.From.Id), "You have entered empty playlist url")
        |> _bot.SendTextMessageAsync

      return ()
    }
