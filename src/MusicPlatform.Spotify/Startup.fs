module MusicPlatform.Spotify.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open MusicPlatform
open otsom.fs.Extensions.DependencyInjection

let addSpotifyMusicPlatform (cfg: IConfiguration) (services: IServiceCollection) =
  services.AddSingleton<Playlist.ParseId>(Playlist.parseId)

  services.BuildSingleton<BuildMusicPlatform, _, _, ILogger<BuildMusicPlatform>, _, _>(Library.buildMusicPlatform)