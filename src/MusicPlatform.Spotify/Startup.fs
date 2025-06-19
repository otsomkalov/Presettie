[<RequireQualifiedAccess>]
module MusicPlatform.Spotify.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open MusicPlatform

let addSpotifyMusicPlatform (cfg: IConfiguration) (services: IServiceCollection) =
  services.AddSingleton<Playlist.ParseId>(Playlist.parseId)

  services.AddSingleton<IMusicPlatformFactory, SpotifyMusicPlatformFactory>()