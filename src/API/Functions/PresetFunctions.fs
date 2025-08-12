namespace API.Functions

open System.ComponentModel.DataAnnotations
open System.Threading.Tasks
open API.Shared
open Domain.Core
open Domain.Repos
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open otsom.fs.Extensions
open Domain.Extensions
open Domain.Workflows

type CreatePresetRequest = {
  [<Required>] Name: string
}

type CreatePresetResponse = { Id: PresetId }

type PresetFunctions
  (
    presetRepo: IPresetRepo,
    presetService: IPresetService,
    userRepo: IUserRepo,
    authService: IAuthenticationService,
    userService: IUserService
  ) =
  let runForUser (req: HttpRequest) (handler: TokenUser -> Task<IActionResult>) = task {
    let! authResult = authService.AuthenticateAsync(req.HttpContext, JwtBearerDefaults.AuthenticationScheme)
    let suceeded = authResult |> Option.someIf _.Succeeded

    let principal = suceeded |> Option.bind (_.Principal >> Option.ofObj)
    let identity = principal |> Option.bind (_.Identity >> Option.ofObj)
    let name = identity |> Option.bind (_.Name >> Option.ofObj)

    let userId =
      name
      |> Option.map (fun name -> name.Split "|" |> Array.last |> _.Split(":") |> Array.last)

    return!
      userId
      |> Option.taskMap (fun userId -> handler { UserId = MusicPlatform.UserId userId })
      |> Task.map (Option.defaultValue (UnauthorizedResult() :> IActionResult))
  }

  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) = task {
      let! user = userRepo.LoadUserByMusicPlatform token.UserId
      let! presets = presetRepo.ListUserPresets user.Id

      return OkObjectResult(presets) :> IActionResult
    }

    runForUser request handler

  [<Function("GetPreset")>]
  member this.GetPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> task {
        let! user = userRepo.LoadUserByMusicPlatform token.UserId

        let! preset = presetService.GetPreset(user.Id, presetId)

        match preset with
        | Ok preset -> return OkObjectResult preset :> IActionResult
        | Error Preset.NotFound -> return NotFoundResult() :> IActionResult
      }

    runForUser request (flip handler (RawPresetId presetId))

  [<Function("CreatePreset")>]
  member this.CreatePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets")>] request: HttpRequest, [<FromBody>] body: CreatePresetRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) = task {
      let! user = userRepo.LoadUserByMusicPlatform token.UserId

      // TODO: Validation
      let! newPreset = presetService.CreatePreset(user.Id, body.Name)

      return CreatedAtRouteResult("presets", { Id = newPreset.Id }) :> IActionResult
    }

    runForUser request handler

  [<Function("DeletePreset")>]
  member this.DeletePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "DELETE", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> task {
        let! user = userRepo.LoadUserByMusicPlatform token.UserId

        let! result = userService.RemoveUserPreset(user.Id, presetId)

        match result with
        | Ok _ -> return NoContentResult() :> IActionResult
        | Error Preset.NotFound -> return NotFoundResult() :> IActionResult
      }

    runForUser request (flip handler (RawPresetId presetId))