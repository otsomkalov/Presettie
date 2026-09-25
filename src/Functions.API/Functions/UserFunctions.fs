namespace Functions.API.Functions

open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker

type UserFunctions(authnService: IAuthenticationService, userRepo: IUserRepo) =
  [<Function("User")>]
  member _.GetCurrent([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "users/current")>] request: HttpRequest) =

    let handler =
      fun (token: TokenUser) -> taskResult { return! User.loadOrCreate userRepo token.UserId }

    validateUser authnService request
    |> TaskResult.bind handler
    |> TaskResult.foldResult (fun user -> OkObjectResult(user) :> IActionResult) (function
      | Unauthorized -> UnauthorizedResult() :> IActionResult
      | Operation err -> BadRequestObjectResult(err) :> IActionResult
      | Validation errors -> BadRequestObjectResult(errors) :> IActionResult)