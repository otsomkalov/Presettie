module App.Startup

open Microsoft.Extensions.DependencyInjection
open otsom.fs.Extensions.DependencyInjection

let addApp (services: IServiceCollection) =

  services.BuildSingleton<_, _, _, _, _, _>(RunPreset.handler)

  services.AddSingleton<IMediator, Mediator>()