﻿module Shared.Startup

#nowarn "20"

open System
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Database
open Shared.Services
open Shared.Settings
open Telegram.Bot

let private configureTelegramBotClient (serviceProvider: IServiceProvider) =
  let settings =
    serviceProvider
      .GetRequiredService<IOptions<TelegramSettings>>()
      .Value

  settings.Token |> TelegramBotClient :> ITelegramBotClient

let private configureDbContext (serviceProvider: IServiceProvider) (builder: DbContextOptionsBuilder) =
  let settings =
    serviceProvider
      .GetRequiredService<IOptions<DatabaseSettings>>()
      .Value

  builder.UseNpgsql(settings.ConnectionString)

  ()

let addSettings (configuration: IConfiguration) (services: IServiceCollection) =
  services.Configure<SpotifySettings>(configuration.GetSection(SpotifySettings.SectionName))
  services.Configure<TelegramSettings>(configuration.GetSection(TelegramSettings.SectionName))
  services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName))
  services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName))

let addServices (services: IServiceCollection) =
  services.AddSingleton<SpotifyClientProvider>()
  services.AddDbContext<AppDbContext>(configureDbContext)

  services.AddSingleton<ITelegramBotClient>(configureTelegramBotClient)
