module Infrastructure.Telegram.Startup

#nowarn "20"

open Generator.Settings
open Infrastructure
open Infrastructure.Telegram.Services
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MongoDB.Driver
open Telegram.Bot
open Telegram.Repos
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Bot.Telegram
open otsom.fs.Auth.Spotify

let private configureTelegramBotClient (options: IOptions<TelegramSettings>) =
  let settings = options.Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let addTelegram (configuration: IConfiguration) (services: IServiceCollection) =
  services.Configure<TelegramSettings>(configuration.GetSection TelegramSettings.SectionName)

  services.BuildSingleton<ITelegramBotClient, IOptions<TelegramSettings>>(configureTelegramBotClient)

  services |> Startup.addSpotifyAuth |> Startup.addTelegramBot configuration

  services.BuildSingleton<IMongoCollection<Entities.Chat>, IMongoDatabase>(fun db -> db.GetCollection "chats")

  services.AddSingleton<IChatRepo, ChatRepo>()

  services.AddScoped<MessageService>().AddScoped<CallbackQueryService>()