module Domain.Startup

open Domain.Core
open Domain.Repos
open Domain.Workflows
open Microsoft.Extensions.DependencyInjection
open otsom.fs.Extensions.DependencyInjection
open MusicPlatform

let addDomain (services: IServiceCollection) =
  services
    .BuildSingleton<PresetSettings.SetPresetSize, IPresetRepo>(PresetSettings.setPresetSize)

    .BuildSingleton<Preset.Get, IPresetRepo>(Preset.get)

    .BuildSingleton<Preset.IncludePlaylist, Playlist.ParseId, IPresetRepo, BuildMusicPlatform>(Preset.includePlaylist)
    .BuildSingleton<Preset.ExcludePlaylist, Playlist.ParseId, IPresetRepo, BuildMusicPlatform>(Preset.excludePlaylist)

    .BuildSingleton<User.Get, IUserRepo>(User.get)
    .BuildSingleton<User.CreatePreset, IPresetRepo, IUserRepo>(User.createPreset)
    .BuildSingleton<User.SetCurrentPresetSize, IUserRepo, _>(User.setCurrentPresetSize)

    .AddSingleton<Preset.Validate>(Preset.validate)