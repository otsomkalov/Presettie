namespace API.Functions

open System.Threading.Tasks
open API.Services
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open otsom.fs.Core

[<CLIMutable>]
type TokenRequest = { UserId: string }

type AuthFunctions(jwtService: IJWTService) =

  [<Function("GetToken")>]
  member this.GetToken
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "auth/token")>] request: HttpRequest, [<FromBody>] body: TokenRequest)
    =

    let token = jwtService.GenerateToken(UserId body.UserId)

    OkObjectResult token :> IActionResult |> Task.FromResult