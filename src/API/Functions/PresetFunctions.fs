namespace API.Functions

open System.Threading.Tasks
open API.Services
open Domain.Repos
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open otsom.fs.Extensions

type PresetFunctions(jwtService: IJWTService, presetRepo: IPresetRepo) =
  let runForUser (req: HttpRequest) fn =
    req.Headers.Authorization
    |> string
    |> Option.ofObj
    |> Option.map _.Split(" ")
    |> Option.bind (function
      | [| "Bearer"; token |] -> Some(token)
      | _ -> None)
    |> Option.taskMap jwtService.DecodeToken
    |> Task.map Option.flatten
    |> TaskOption.taskMap fn
    |> Task.bind (Option.defaultWithTask (fun () -> UnauthorizedResult() :> IActionResult |> Task.FromResult))

  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (user: TokenUser) = task {
      let! presets = presetRepo.ListUserPresets user.Id

      return OkObjectResult(presets) :> IActionResult
    }

    runForUser request handler