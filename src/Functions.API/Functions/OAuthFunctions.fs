namespace Functions.Bot.Telegram

open System
open System.Threading.Tasks
open Microsoft.Extensions.Options
open MusicPlatform
open Domain.Core
open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open otsom.fs.OAuth

type OAuthFunctions
  (
    authnService,
    oAuthClient: IOAuthClient,
    additionalOAuthOptions: IOptions<AdditionalOAuthSettings>,
    userRepo: IUserRepo,
    musicPlatformFactory: IMusicPlatformFactory
  ) =
  let additionalOAuthSettings = additionalOAuthOptions.Value

  [<Function("LinkMusicPlatform")>]
  member this.LinkMusicPlatform
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "oauth/music-platform/link")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler =
      fun (token: TokenUser) -> taskResult {
        let! user = User.loadOrCreate userRepo token.UserId

        do!
          user.MusicPlatformId
          |> Result.requireNone (Operation "User already has music platform connected")

        return! oAuthClient.InitAuth(token.UserId.ToAccountId())
      }

    validateUser authnService request
    |> TaskResult.bind handler
    |> TaskResult.foldResult (fun redirectUri -> OkObjectResult(redirectUri) :> IActionResult) (function
      | Unauthorized -> UnauthorizedResult() :> IActionResult
      | Operation err -> BadRequestObjectResult(err) :> IActionResult)

  [<Function("MusicPlatformCallback")>]
  member this.MusicPlatformCallback
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "oauth/music-platform/callback")>] request: HttpRequest)
    : Task<IActionResult> =

    let handler =
      fun (request: HttpRequest) -> taskResult {
        let state = request.Query["state"]
        let code = request.Query["code"]

        let! completedAuth =
          oAuthClient.CompleteAuth(State state, Code code)
          |> TaskResult.mapError Operation

        let! user =
          User.loadOrCreate userRepo (completedAuth.AccountId.Value |> Guid.Parse |> UserId)

        let! musicPlatform =
          musicPlatformFactory.GetMusicPlatform(user.Id.ToMusicPlatformId())
          |> TaskResult.requireSome Unauthorized

        let! musicPlatformUser = musicPlatform.GetMe()

        let updatedUser =
          { user with
              MusicPlatformId = Some musicPlatformUser.Id
          }

        do! userRepo.SaveUser updatedUser

        return ()
      }

    handler request
    |> TaskResult.foldResult (fun _ -> RedirectResult(additionalOAuthSettings.ReturnUri, true) :> IActionResult) (function
      | Unauthorized -> UnauthorizedResult() :> IActionResult
      | Operation err -> BadRequestObjectResult(err) :> IActionResult)