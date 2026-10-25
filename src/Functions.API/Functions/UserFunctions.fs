namespace Functions.API.Functions

open Domain.Core
open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker

type UserFunctions(userService: IUserService, userRepo: IUserRepo) =
  [<Function("GetCurrentUser")>]
  member _.GetCurrent([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "users/current")>] request: HttpRequest) =
    let handler =
      fun (userId: UserId) -> taskResult { return! User.loadOrCreate userRepo userId }

    handler request.UserId
    |> TaskResult.foldResult (fun user -> OkObjectResult(user) :> IActionResult) (function
      | Operation err -> BadRequestObjectResult(err) :> IActionResult
      | Validation errors -> BadRequestObjectResult(errors) :> IActionResult)

  [<Function("PatchCurrentUser")>]
  member _.PatchCurrentUser
    (
      [<HttpTrigger(AuthorizationLevel.Function, "PATCH", Route = "users/current")>] request: HttpRequest,
      [<FromBody>] patches: PatchUserCommand list
    ) =
    let handler =
      fun (userId: UserId) (patches: PatchUserCommand list) -> userService.PatchUser(userId, patches)

    handler request.UserId patches
    |> Task.map (fun user -> OkObjectResult(user) :> IActionResult)