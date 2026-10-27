module Bot.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open otsom.fs.Resources

#nowarn "20"

let addBot (cfg: IConfiguration) (services: IServiceCollection) = services |> Startup.addResources cfg