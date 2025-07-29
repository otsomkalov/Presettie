namespace API.Functions

open System.Threading.Tasks
open API.Services
open Domain.Core
open Domain.Repos
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open otsom.fs.Extensions
open Domain.Extensions
open Domain.Workflows

type PresetFunctions(jwtService: IJWTService, presetRepo: IPresetRepo, presetService: IPresetService) =
  let runForUser (req: HttpRequest) handler =
    req.Headers.Authorization
    |> string
    |> Option.ofObj
    |> Option.map _.Split(" ")
    |> Option.bind (function
      | [| "Bearer"; token |] -> Some(token)
      | _ -> None)
    |> Option.taskMap jwtService.DecodeToken
    |> Task.map Option.flatten
    |> TaskOption.taskMap handler
    |> Task.map (Option.defaultValue (UnauthorizedResult() :> IActionResult))

  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (user: TokenUser) = task {
      let! presets = presetRepo.ListUserPresets user.Id

      return OkObjectResult(presets) :> IActionResult
    }

    runForUser request handler

  [<Function("GetPreset")>]
  member this.GetPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (user: TokenUser) =
      fun presetId ->
        presetService.GetPreset(user.Id, presetId)
        |> Task.map (function
          | Ok preset -> OkObjectResult preset :> IActionResult
          | Error Preset.NotFound -> NotFoundResult() :> IActionResult)

    runForUser request (flip handler (RawPresetId presetId))