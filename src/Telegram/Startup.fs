module Telegram.Startup

open Domain.Repos
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open MusicPlatform
open Telegram.Core
open Telegram.Handlers.Click
open Telegram.Repos
open Telegram.Workflows
open otsom.fs.Extensions.DependencyInjection
open otsom.fs.Telegram.Bot.Auth.Spotify

let private addClickHandlers (services: IServiceCollection) =
  services
    .BuildSingleton<ClickHandlerFactory, IPresetRepo>(presetInfoClickHandler)
    .BuildSingleton<ClickHandlerFactory, _, IChatRepo>(showPresetsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(enableRecommendationsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(disableRecommendationsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(enableUniqueArtistsClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(disableUniqueArtistsClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(includeLikedTracksClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(excludeLikedTracksClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(ignoreLikedTracksClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, Playlist.CountTracks>(showIncludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(removeIncludedPlaylistClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, Playlist.CountTracks>(showExcludedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(removeExcludedPlaylistClickHandler)

    .BuildSingleton<ClickHandlerFactory, IPresetRepo, Playlist.CountTracks>(showTargetedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, Playlist.CountTracks, ShowNotification>(appendToTargetedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, Playlist.CountTracks, ShowNotification>(overwriteTargetedPlaylistClickHandler)
    .BuildSingleton<ClickHandlerFactory, IPresetRepo, ShowNotification>(removeTargetedPlaylistClickHandler)

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

    .BuildSingleton<MessageHandlerFactory, IUserRepo, IChatRepo, _, _, _>(includePlaylistMessageHandler)
    .BuildSingleton<MessageHandlerFactory, IUserRepo, IChatRepo, _, _, _>(excludePlaylistMessageHandler)

let addBot (cfg: IConfiguration) (services: IServiceCollection) =
  services
  |> Startup.addTelegramBotSpotifyAuthCore cfg
  |> addClickHandlers
  |> addMessageHandlers