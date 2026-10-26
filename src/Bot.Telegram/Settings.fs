module Bot.Telegram.Settings

[<CLIMutable>]
type TelegramSettings =
  {
    Token: string
    BotUrl: string
  }

  static member SectionName = "Telegram"

[<CLIMutable>]
type DatabaseSettings =
  {
    ConnectionString: string
    Name: string
  }

  static member SectionName = "Database"