namespace Functions.API.Functions

open Domain.Core
open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker

type UserFunctions(userRepo: IUserRepo) =
  [<Function("User")>]
  member _.GetCurrent([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "users/current")>] request: HttpRequest) =
    let handler =
      fun (userId: UserId) -> taskResult { return! User.loadOrCreate userRepo userId }

    handler request.UserId
    |> TaskResult.foldResult (fun user -> OkObjectResult(user) :> IActionResult) (function
      | Operation err -> BadRequestObjectResult(err) :> IActionResult
      | Validation errors -> BadRequestObjectResult(errors) :> IActionResult)