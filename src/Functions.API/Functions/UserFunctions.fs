namespace Functions.API.Functions

open Domain.Core
open Domain.Repos
open Domain.Workflows
open FsToolkit.ErrorHandling
open Functions.API.Shared
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging

type UserFunctions(userRepo: IUserRepo, logger: ILogger<UserFunctions>) =
  [<Function("User")>]
  member _.GetCurrent
    ([<HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "users/current")>] request: HttpRequest, ctx: FunctionContext)
    =

    logger.LogInformation("X-MS-CLIENT-PRINCIPAL={X-MS-CLIENT-PRINCIPAL}", request.Headers["X-MS-CLIENT-PRINCIPAL"])
    logger.LogInformation("X-MS-CLIENT-PRINCIPAL-ID={X-MS-CLIENT-PRINCIPAL-ID}", request.Headers["X-MS-CLIENT-PRINCIPAL-ID"])
    logger.LogInformation("X-MS-CLIENT-PRINCIPAL-NAME={X-MS-CLIENT-PRINCIPAL-NAME}", request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"])
    logger.LogInformation("X-MS-CLIENT-PRINCIPAL-IDP={X-MS-CLIENT-PRINCIPAL-IDP}", request.Headers["X-MS-CLIENT-PRINCIPAL-IDP"])

    let handler =
      fun (userId: UserId) -> taskResult { return! User.loadOrCreate userRepo userId }

    handler ctx.UserId
    |> TaskResult.foldResult (fun user -> OkObjectResult(user) :> IActionResult) (function
      | Operation err -> BadRequestObjectResult(err) :> IActionResult
      | Validation errors -> BadRequestObjectResult(errors) :> IActionResult)