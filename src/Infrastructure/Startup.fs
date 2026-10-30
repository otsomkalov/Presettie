module Infrastructure.Startup

#nowarn "20"

open App
open Domain.Repos
open Infrastructure.Repos
open Infrastructure.Settings
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MongoDB.Driver
open MusicPlatform.Musicae
open MusicPlatform.ReccoBeats
open otsom.fs.Extensions.DependencyInjection

let private configureMongoClient (options: IOptions<DatabaseSettings>) =
  let settings = options.Value

  new MongoClient(settings.ConnectionString) :> IMongoClient

let private configureMongoDatabase (options: IOptions<DatabaseSettings>) (mongoClient: IMongoClient) =
  let settings = options.Value

  mongoClient.GetDatabase(settings.Name)

let addInfrastructure (configuration: IConfiguration) (services: IServiceCollection) =
  services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName))
  services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName))

  services.AddSingleton<IRecommenderFactory, RecommenderFactory>()

  services.AddSingleton<INotifier, AzureStorageQueueNotifier>()

  services.BuildSingleton<IMongoClient, IOptions<DatabaseSettings>>(configureMongoClient)
  services.BuildSingleton<IMongoDatabase, IOptions<DatabaseSettings>, IMongoClient>(configureMongoDatabase)

  services
  |> Startup.addReccoBeatsMusicPlatform configuration
  |> Startup.addMusicaeMusicPlatform configuration

  services.AddSingleton<IPresetRepo, PresetRepo>()
  services.AddSingleton<IUserRepo, UserRepo>()