module Domain.Startup

open Domain.Core
open Domain.Workflows
open Microsoft.Extensions.DependencyInjection

#nowarn "20"

let addDomain cfg (services: IServiceCollection) =
  services
    .AddSingleton<IPresetService, PresetService>()
    .AddSingleton<IUserService, UserService>()