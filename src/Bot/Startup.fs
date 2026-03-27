module Bot.Startup

open Domain.Core
open Domain.Repos
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open MusicPlatform
open Bot.Core
open Bot.Handlers.Click
open Bot.Handlers.Message
open Bot.Workflows
open otsom.fs.Auth
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Resources

#nowarn "20"

let private addMessageHandlers (services: IServiceCollection) =
  services
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo, IAuthService>(startMessageHandler)
    .AddSingleton<MessageHandlerFactory>(faqMessageHandler)
    .AddSingleton<MessageHandlerFactory>(privacyMessageHandler)
    .AddSingleton<MessageHandlerFactory>(guideMessageHandler)
    .AddSingleton<MessageHandlerFactory>(helpMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IPresetRepo>(myPresetsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo>(presetSettingsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService>(queuePresetRunMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IPresetService>(createPresetMessageHandler)
    .AddSingleton<MessageHandlerFactory>(createPresetButtonMessageHandler)

    .AddSingleton<MessageHandlerFactory>(setPresetSizeMessageButtonHandler)
    .BuildSingleton<MessageHandlerFactory, IUserService, IUserRepo, IPresetRepo>(setPresetSizeMessageHandler)

    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(includePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(excludePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(targetPlaylistButtonMessageHandler)

    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(excludeArtistButtonMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(includePlaylistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(includeArtistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(excludePlaylistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(excludeArtistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(targetPlaylistMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo>(backMessageButtonHandler)

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services.BuildSingleton<Resources.GetResourceProvider, CreateResourceProvider, CreateDefaultResourceProvider>(
    Resources.getResourceProvider
  )

  services.AddSingleton<IChatService, ChatService>()

  services |> Startup.addAuthCore cfg |> Startup.addResources cfg

  services |> addMessageHandlers