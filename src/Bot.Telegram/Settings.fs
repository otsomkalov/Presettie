module Bot.Telegram.Settings

[<CLIMutable>]
type TelegramSettings =
  { Token: string
    BotUrl: string }

  static member SectionName = "Telegram"