namespace Functions.Bot.Telegram

open Bot.Core
open Bot.Handlers
open Bot.Repos
open Domain.Core
open Domain.Repos
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open Microsoft.Extensions.Logging
open Telegram.Bot.Types
open otsom.fs.Auth

type UpdateFunctions
  (
    authSvc: IAuthService,
    userRepo: IUserRepo,
    userService: IUserService,
    presetService,
    presetRepo: IPresetRepo,
    buildMusicPlatform,
    buildChatContext,
    getResp,
    chatRepo: IChatRepo,
    chatService: IChatService,
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
      getResp
      chatRepo
      chatService
      logger

  [<Function("HandleUpdateAsync")>]
  member this.HandleUpdateAsync
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "telegram/update")>] request: HttpRequest, [<FromBody>] update: Update)
    =
    task {
      try
        let upd = Mappers.Update.map update

        match upd with
        | Some upd -> do! updateHandler upd
        | None -> logger.LogInformation("Unsupported update type: {UpdateType}", update.Type)
      with e ->
        logger.LogError(e, "Error during processing update:")
    }