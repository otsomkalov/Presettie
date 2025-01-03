module Telegram.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Telegram.Core
open Telegram.Repos
open Telegram.Workflows
open otsom.fs.Extensions.DependencyInjection

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services
    .AddSingleton<MessageHandlerFactory>(faqMessageHandler)
    .AddSingleton<MessageHandlerFactory>(privacyMessageHandler)
    .AddSingleton<MessageHandlerFactory>(guideMessageHandler)
    .AddSingleton<MessageHandlerFactory>(helpMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IChatRepo>(myPresetsMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, _, IChatRepo>(backMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, _, IChatRepo>(presetSettingsMessageHandler)
