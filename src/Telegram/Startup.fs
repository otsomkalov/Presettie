module Telegram.Startup

open Domain.Repos
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Telegram.Core
open Telegram.Handlers.Click
open Telegram.Repos
open Telegram.Workflows
open otsom.fs.Extensions.DependencyInjection

let private addClickHandlers (services: IServiceCollection) =
  services
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(presetInfoClickHandler)
    .BuildSingleton<ClickHandlerFactory, _, IChatRepo>(showPresetsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(disableUniqueArtistsClickHandler)

let private addMessageHandlers (services: IServiceCollection) =
  services
    .AddSingleton<MessageHandlerFactory>(faqMessageHandler)
    .AddSingleton<MessageHandlerFactory>(privacyMessageHandler)
    .AddSingleton<MessageHandlerFactory>(guideMessageHandler)
    .AddSingleton<MessageHandlerFactory>(helpMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IChatRepo>(myPresetsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, _, IChatRepo>(backMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, _, IChatRepo>(presetSettingsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, _, _, IChatRepo>(setPresetSizeMessageHandler)

    .AddSingleton<MessageHandlerFactory>(createPresetButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IChatRepo>(createPresetMessageHandler)

    .BuildSingleton<MessageHandlerFactory, IChatRepo, IUserRepo, _, _, _>(includePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IChatRepo, IUserRepo, _, _, _>(excludePlaylistButtonMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IChatRepo, IUserRepo, _, _, _>(targetPlaylistButtonMessageHandler)

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services |> addClickHandlers |> addMessageHandlers