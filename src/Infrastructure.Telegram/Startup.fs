module Infrastructure.Telegram.Startup

#nowarn "20"

open Generator.Settings
open Infrastructure
open Infrastructure.Telegram.Repos
open Infrastructure.Telegram.Services
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Telegram.Bot
open Telegram.Repos
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Telegram.Bot

let private configureTelegramBotClient (options: IOptions<TelegramSettings>) =
  let settings = options.Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let addTelegram (configuration: IConfiguration) (services: IServiceCollection) =
  services
  |> Startup.addTelegramBotCore

  services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.SectionName))

  services.BuildSingleton<ITelegramBotClient, IOptions<TelegramSettings>>(configureTelegramBotClient)

  services.AddSingleton<IChatRepo, MockChatRepo>()

  services.AddScoped<MessageService>().AddScoped<CallbackQueryService>()