module Bot.Telegram.Startup

#nowarn "20"

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
open otsom.fs.Resources.Mongo

let private configureTelegramBotClient (options: IOptions<TelegramSettings>) =
  let settings = options.Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let private configureMongoClient (options: IOptions<DatabaseSettings>) =
  let settings = options.Value

  new MongoClient(settings.ConnectionString) :> IMongoClient

let private configureMongoDatabase (options: IOptions<DatabaseSettings>) (mongoClient: IMongoClient) =
  let settings = options.Value

  mongoClient.GetDatabase(settings.Name)

let addTelegram (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<TelegramSettings>(cfg.GetSection TelegramSettings.SectionName)

  services.Configure<DatabaseSettings>(cfg.GetSection(DatabaseSettings.SectionName))

  services.BuildSingleton<IMongoClient, IOptions<DatabaseSettings>>(configureMongoClient)
  services.BuildSingleton<IMongoDatabase, IOptions<DatabaseSettings>, IMongoClient>(configureMongoDatabase)

  services.BuildSingleton<ITelegramBotClient, IOptions<TelegramSettings>>(configureTelegramBotClient)

  services |> Startup.addTelegramBot cfg |> Startup.addMongoResources cfg

  services.BuildSingleton<IMongoCollection<Entities.Chat>, IMongoDatabase>(fun db -> db.GetCollection "chats")

  services.AddSingleton<IChatRepo, ChatRepo>()