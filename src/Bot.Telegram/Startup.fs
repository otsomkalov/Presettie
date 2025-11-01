module Bot.Telegram.Startup

#nowarn "20"

open Infrastructure
open Bot.Telegram.Services
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MongoDB.Driver
open Telegram.Bot
open Bot.Repos
open Bot.Telegram.Repos
open Bot.Telegram.Settings
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Bot.Telegram
open otsom.fs.Auth.Spotify
open otsom.fs.Resources.Mongo

let private configureTelegramBotClient (options: IOptions<TelegramSettings>) =
  let settings = options.Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let addTelegram (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<TelegramSettings>(cfg.GetSection TelegramSettings.SectionName)

  services.BuildSingleton<ITelegramBotClient, IOptions<TelegramSettings>>(configureTelegramBotClient)

  services
  |> Startup.addSpotifyAuth
  |> Startup.addTelegramBot cfg
  |> Startup.addMongoResources cfg

  services.BuildSingleton<IMongoCollection<Entities.Chat>, IMongoDatabase>(fun db -> db.GetCollection "chats")

  services.AddSingleton<IChatRepo, ChatRepo>()

  services.AddScoped<MessageService>().AddScoped<CallbackQueryService>()