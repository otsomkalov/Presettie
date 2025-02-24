module Domain.Startup

open Domain.Core
open Domain.Workflows
open Microsoft.Extensions.DependencyInjection
open MusicPlatform

#nowarn "20"

let addDomain cfg (services: IServiceCollection) =
  services
    .AddSingleton<Shuffler<Track>>(List.randomShuffle)
    .AddSingleton<IPresetService, PresetService>()
    .AddSingleton<IUserService, UserService>()