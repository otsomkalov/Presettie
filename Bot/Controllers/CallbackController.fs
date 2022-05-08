﻿namespace Bot

open Bot.Services
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Options
open Shared.QueueMessages
open Shared.Services
open Shared.Settings
open SpotifyAPI.Web

[<ApiController>]
[<Route("callback")>]
type CallbackController
  (
    _spotifyClientProvider: SpotifyClientProvider,
    _spotifyOptions: IOptions<SpotifySettings>,
    _telegramOptions: IOptions<TelegramSettings>,
    _amazonOptions: IOptions<AmazonSettings>,
    _sqsService: SQSService
  ) =
  inherit ControllerBase()

  let _spotifySettings = _spotifyOptions.Value
  let _amazonSettings = _amazonOptions.Value

  let _telegramSettings =
    _telegramOptions.Value

  [<HttpGet>]
  member this.HandleCallbackAsync(code: string) =
    task {
      let! tokenResponse =
        (_spotifySettings.ClientId, _spotifySettings.ClientSecret, code, _spotifySettings.CallbackUrl)
        |> AuthorizationCodeTokenRequest
        |> OAuthClient().RequestToken

      let spotifyClient =
        (_spotifySettings.ClientId, _spotifySettings.ClientSecret, tokenResponse)
        |> AuthorizationCodeAuthenticator
        |> SpotifyClientConfig
          .CreateDefault()
          .WithAuthenticator
        |> SpotifyClient

      let! spotifyUserProfile = spotifyClient.UserProfile.Current()

      let loginMessage =
        SpotifyLoginMessage(SpotifyId = spotifyUserProfile.Id, TokenResponse = tokenResponse)

      do! _sqsService.SendMessageAsync loginMessage MessageTypes.SpotifyLogin

      (spotifyUserProfile.Id, spotifyClient)
      |> _spotifyClientProvider.SetClient

      return this.RedirectPermanent($"{_telegramSettings.BotUri}?start={spotifyUserProfile.Id}")
    }
