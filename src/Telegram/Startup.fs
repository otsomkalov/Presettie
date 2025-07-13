module Telegram.Startup

open Domain.Core
open Domain.Repos
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open MusicPlatform
open Telegram.Core
open Telegram.Handlers.Click
open Telegram.Handlers.Message
open Telegram.Workflows
open otsom.fs.Auth
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Resources

#nowarn "20"

let private addClickHandlers (services: IServiceCollection) =
  services
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(listPresetsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(presetInfoClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetService>(runPresetClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IUserService>(removePresetClickHandler)
    .BuildSingleton<ClickHandlerFactory, IUserService>(setCurrentPresetClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(enableRecommendationsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(disableRecommendationsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(enableUniqueArtistsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(disableUniqueArtistsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(includeLikedTracksClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(excludeLikedTracksClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService>(ignoreLikedTracksClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(listIncludedPlaylistsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(listExcludedPlaylistsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(listTargetedPlaylistsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IMusicPlatformFactory>(showIncludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IMusicPlatformFactory>(showExcludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IMusicPlatformFactory>(showTargetedPlaylistClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService, IMusicPlatformFactory>(appendToTargetedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService, IMusicPlatformFactory>(overwriteTargetedPlaylistClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetService>(removeIncludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetService>(removeExcludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetService>(removeTargetedPlaylistClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService, IMusicPlatformFactory>(setAllTracksIncludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, IPresetService, IMusicPlatformFactory>(setOnlyLikedIncludedPlaylistClickHandler)

let private addMessageHandlers (services: IServiceCollection) =
  services
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo, IAuthService>(startMessageHandler)
    .AddSingleton<MessageHandlerFactory>(faqMessageHandler)
    .AddSingleton<MessageHandlerFactory>(privacyMessageHandler)
    .AddSingleton<MessageHandlerFactory>(guideMessageHandler)
    .AddSingleton<MessageHandlerFactory>(helpMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IPresetRepo>(myPresetsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo, IPresetRepo>(presetSettingsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService>(queuePresetRunMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IPresetService>(createPresetMessageHandler)
    .AddSingleton<MessageHandlerFactory>(createPresetButtonMessageHandler)

    .AddSingleton<MessageHandlerFactory>(setPresetSizeMessageButtonHandler)
    .BuildSingleton<MessageHandlerFactory, IUserService, IUserRepo, IPresetRepo, IPresetRepo>(setPresetSizeMessageHandler)

    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(includePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(excludePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IAuthService>(targetPlaylistButtonMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(includePlaylistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(excludePlaylistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetService, IAuthService>(targetPlaylistMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IUserRepo, IPresetRepo>(backMessageButtonHandler)

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services.BuildSingleton<Resources.GetResourceProvider, CreateResourceProvider, CreateDefaultResourceProvider>(
    Resources.getResourceProvider
  )

  services.AddSingleton<IChatService, ChatService>()

  services |> Startup.addAuthCore cfg |> Startup.addResources cfg

  services |> addClickHandlers |> addMessageHandlers