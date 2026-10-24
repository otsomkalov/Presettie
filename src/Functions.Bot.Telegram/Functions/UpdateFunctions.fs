namespace Functions.Bot.Telegram

open App
open Bot.Handlers
open Bot.Repos
open Bot.Workflows
open Domain.Core
open Domain.Repos
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging
open Telegram.Bot.Types
open otsom.fs.Auth
open otsom.fs.OAuth
open otsom.fs.Resources

type UpdateFunctions
  (
    authSvc: IAuthService,
    userRepo: IUserRepo,
    userService: IUserService,
    presetService,
    presetRepo: IPresetRepo,
    buildMusicPlatform,
    buildChatContext,
    chatRepo: IChatRepo,
    appOAuthClient: IOAuthClient,
    apiClientFactory: IApiClientFactory,
    createDefaultResp: CreateDefaultResourceProvider,
    createResp: CreateResourceProvider,
    logger: ILogger<UpdateFunctions>
  ) =
  inherit ControllerBase()

  let updateHandler =
    Update.main
      authSvc
      userRepo
      userService
      presetService
      presetRepo
      buildMusicPlatform
      buildChatContext
      chatRepo
      appOAuthClient
      apiClientFactory
      createDefaultResp
      createResp
      logger

  [<Function("HandleUpdateAsync")>]
  member this.HandleUpdateAsync
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "telegram/update")>] request: HttpRequest, [<FromBody>] update: Update)
    = task {
    try
      let upd = Mappers.Update.map update

      match upd with
      | Some upd -> do! updateHandler upd
      | None -> logger.LogInformation("Unsupported update type: {UpdateType}", update.Type)
    with e ->
      logger.LogError(e, "Error during processing update:")
  }