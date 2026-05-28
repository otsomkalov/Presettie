module Bot.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Bot.Core
open Bot.Workflows
open otsom.fs.Auth
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Resources

#nowarn "20"

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services.BuildSingleton<Resources.GetResourceProvider, CreateResourceProvider, CreateDefaultResourceProvider>(
    Resources.getResourceProvider
  )

  services.AddSingleton<IChatService, ChatService>()

  services |> Startup.addAuthCore cfg |> Startup.addResources cfg