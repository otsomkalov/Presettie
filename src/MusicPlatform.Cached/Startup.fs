module MusicPlatform.Cached.Startup

#nowarn "20"

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open MusicPlatform
open MusicPlatform.Cached.Settings
open StackExchange.Redis
open otsom.fs.Extensions.DependencyInjection

let private configureRedisCache (options: IOptions<RedisSettings>) =
  let settings = options.Value

  ConnectionMultiplexer.Connect(settings.ConnectionString) :> IConnectionMultiplexer

let addCachedMusicPlatform (cfg: IConfiguration) (services: IServiceCollection) =
  services.Configure<RedisSettings>(cfg.GetSection(RedisSettings.SectionName))

  services.BuildSingleton<IConnectionMultiplexer, IOptions<RedisSettings>>(configureRedisCache)

  services.Decorate<IMusicPlatformFactory, RedisMusicPlatformFactory>().Decorate<IMusicPlatformFactory, MemoryCachedMusicPlatformFactory>()