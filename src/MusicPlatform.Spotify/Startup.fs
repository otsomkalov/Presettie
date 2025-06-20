[<RequireQualifiedAccess>]
module MusicPlatform.Spotify.Startup

#nowarn "20"

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open MusicPlatform

let addSpotifyMusicPlatform (cfg: IConfiguration) (services: IServiceCollection) =
  services.AddSingleton<Playlist.ParseId>(Playlist.parseId)

  services.AddSingleton<IMusicPlatformFactory, SpotifyMusicPlatformFactory>()