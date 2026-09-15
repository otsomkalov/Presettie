module App.Startup

open Domain.Repos
open Microsoft.Extensions.DependencyInjection
open otsom.fs.Extensions.DependencyInjection

let addApp (services: IServiceCollection) =

  services.BuildSingleton<_, IPresetRepo, _, _, _, _>(RunPreset.handler).BuildSingleton<_, IUserRepo, IPresetRepo>(RemovePreset.handler)

  services.AddSingleton<IMediator, Mediator>()