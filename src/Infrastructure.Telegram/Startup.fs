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
open Telegram.Core
open Telegram.Repos
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Telegram.Bot

let private configureTelegramBotClient (options: IOptions<TelegramSettings>) =
  let settings = options.Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let addTelegram (configuration: IConfiguration) (services: IServiceCollection) =
  services
  |> otsom.fs.Bot.Telegram.Startup.addTelegramBot configuration

  services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.SectionName))

  services.BuildSingleton<ITelegramBotClient, IOptions<TelegramSettings>>(configureTelegramBotClient)
  services.BuildSingleton<SendLink, ITelegramBotClient>(sendLink)
  services.BuildSingleton<ShowNotification, ITelegramBotClient>(Workflows.showNotification)

  services.AddSingleton<IChatRepo, MockChatRepo>()

  services.AddScoped<MessageService>().AddScoped<CallbackQueryService>()