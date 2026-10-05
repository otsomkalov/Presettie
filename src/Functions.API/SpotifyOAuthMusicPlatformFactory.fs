namespace Functions.API

open System
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open MusicPlatform
open MusicPlatform.Spotify
open SpotifyAPI.Web
open otsom.fs.OAuth
open otsom.fs.OAuth.Spotify

type SpotifyOAuthMusicPlatformFactory
  (oAuthClient: IOAuthClient, oAuthOptions: IOptions<SpotifyOAuthSettings>, logger: ILogger<SpotifyMusicPlatform>) =
  let oAuthSettings = oAuthOptions.Value

  interface IMusicPlatformFactory with
    member this.GetMusicPlatform(userId) = taskOption {
      let! completedAuth = oAuthClient.GetCompleted(userId.Value |> AccountId)

      let pkceAuthenticator =
        PKCEAuthenticator(
          oAuthSettings.ClientId,
          PKCETokenResponse(
            AccessToken = completedAuth.AccessToken.Value,
            RefreshToken = completedAuth.RefreshToken.Value,
            Scope = (oAuthSettings.Scope |> String.concat " ")
          )
        )

      let retryHandler =
        SimpleRetryHandler(RetryAfter = TimeSpan.FromSeconds(30L), RetryTimes = 3, TooManyRequestsConsumesARetry = true)

      let config =
        SpotifyClientConfig.CreateDefault().WithRetryHandler(retryHandler).WithAuthenticator(pkceAuthenticator)

      let client = config |> SpotifyClient :> ISpotifyClient

      return SpotifyMusicPlatform(client, logger): IMusicPlatform
    }