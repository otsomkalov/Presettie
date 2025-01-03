﻿module Telegram.Startup

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Telegram.Core
open Telegram.Repos
open Telegram.Workflows
open otsom.fs.Extensions.DependencyInjection
open Domain.Core
open otsom.fs.Telegram.Bot.Core

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services.BuildSingleton<User.SendCurrentPreset, User.Get, Preset.Get, SendUserKeyboard>(User.sendCurrentPreset)

  services
    .AddSingleton<MessageHandlerFactory>(faqMessageHandler)
    .AddSingleton<MessageHandlerFactory>(privacyMessageHandler)
    .AddSingleton<MessageHandlerFactory>(guideMessageHandler)
    .AddSingleton<MessageHandlerFactory>(helpMessageHandler)
    .BuildSingleton<MessageHandlerFactory, _, IChatRepo>(myPresetsMessageHandler)
