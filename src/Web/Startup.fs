module Web.Startup

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.Logging

let builder = WebAssemblyHostBuilder.CreateDefault()

builder.Logging.SetMinimumLevel(LogLevel.Information)

builder.RootComponents.Add<Main.App>("#main")

builder.Build().RunAsync()