namespace API.Functions

open System.Collections.Generic
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
open Microsoft.Azure.Functions.Worker.Http
open otsom.fs.Extensions
open Domain.Extensions

[<CLIMutable>]
type CreatePresetRequest =
  { [<Required; MinLength(3)>]
    Name: string }

type CreatePresetResponse = { Id: PresetId }

type ValidRequest<'a> = { User: TokenUser; Body: 'a }

type ValidationError = { Member: string; Error: string }

type RequestError<'a> =
  | Unauthorized
  | Validation of ValidationError list
  | OperationError of 'a

type PresetFunctions
  (
    presetRepo: IPresetRepo,
    presetService: IPresetService,
    userRepo: IUserRepo,
    authService: IAuthenticationService,
    userService: IUserService
  ) =
  let validateUser (req: HttpRequest) : Task<Result<TokenUser, RequestError<_>>> =
    authService.AuthenticateAsync(req.HttpContext, JwtBearerDefaults.AuthenticationScheme)
    |> Task.map (Option.someIf _.Succeeded)
    |> Task.map (Option.bind (_.Principal >> Option.ofObj))
    |> Task.map (Option.bind (_.Identity >> Option.ofObj))
    |> Task.map (Option.bind (_.Name >> Option.ofObj))
    |> TaskOption.map (fun name -> name.Split "|" |> Array.last |> _.Split(":") |> Array.last)
    |> TaskOption.map (fun userId -> { UserId = MusicPlatform.UserId userId })
    |> Task.map (Result.ofOption RequestError.Unauthorized)

  let validateBody (request: 'a) : Result<'a, RequestError<_>> =
    let validationCtx = ValidationContext(request, null, null)

    let validationErrors = List<ValidationResult>()

    match Validator.TryValidateObject(request, validationCtx, validationErrors, true) with
    | true -> Ok request
    | false ->
      Error(
        validationErrors
        |> List.ofSeq
        |> List.map (fun e ->
          { Error = e.ErrorMessage
            Member = e.MemberNames |> Seq.head })
        |> RequestError.Validation
      )

  let validateRequest (request: HttpRequest) (body: 'a) : Task<Result<ValidRequest<'a>, RequestError<_>>> =
    validateUser request
    |> Task.map (Result.bind (fun user -> validateBody body |> Result.map (fun body -> { User = user; Body = body })))

  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) = task {
      let! user = userRepo.LoadUserByMusicPlatform token.UserId

      return! presetRepo.ListUserPresets user.Id
    }

    validateUser request
    |> Task.bind (Result.taskMap handler)
    |> Task.map (function
      | Ok presets -> OkObjectResult(presets) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(OperationError e) -> BadRequestObjectResult(e) :> IActionResult)

  [<Function("GetPreset")>]
  member this.GetPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> task {
        let! user = userRepo.LoadUserByMusicPlatform token.UserId

        let! preset = presetService.GetPreset(user.Id, presetId)

        return preset |> Result.mapError RequestError.OperationError
      }

    validateUser request
    |> TaskResult.bind (flip handler (RawPresetId presetId))
    |> Task.map (function
      | Ok preset -> OkObjectResult(preset) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(OperationError Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult)

  [<Function("CreatePreset")>]
  member this.CreatePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets")>] request: HttpRequest, [<FromBody>] body: CreatePresetRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) (body: CreatePresetRequest) = task {
      let! user = userRepo.LoadUserByMusicPlatform token.UserId

      let! newPreset = presetService.CreatePreset(user.Id, body.Name)

      return newPreset
    }

    validateRequest request body
    |> TaskResult.taskMap (fun { User = user; Body = body } -> handler user body)
    |> Task.map (function
      | Ok result -> CreatedResult("presets", { Id = result.Id }) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(OperationError e) -> BadRequestObjectResult(e) :> IActionResult)

  [<Function("DeletePreset")>]
  member this.DeletePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "DELETE", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> task {
        let! user = userRepo.LoadUserByMusicPlatform token.UserId

        let! result = userService.RemoveUserPreset(user.Id, presetId)

        return result |> Result.mapError RequestError.OperationError
      }

    validateUser request
    |> TaskResult.bind (flip handler (RawPresetId presetId))
    |> Task.map (function
      | Ok _ -> NoContentResult() :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(OperationError Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult)