namespace Functions.API.Functions

open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks
open App
open Functions.API.Shared
open Domain.Core
open Domain.Repos
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Azure.Functions.Worker
open Microsoft.Azure.Functions.Worker.Http
open otsom.fs.Extensions
open Domain.Extensions
open FsToolkit.ErrorHandling

[<CLIMutable>]
type CreatePresetRequest =
  {
    [<Required; MinLength(3)>]
    Name: string
  }

type CreatePresetResponse = { Id: PresetId }

type PresetFunctions(presetRepo: IPresetRepo, presetService: IPresetService, userRepo: IUserRepo, authnService, mediator: IMediator) =
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
          {
            Error = e.ErrorMessage
            Member = e.MemberNames |> Seq.head
          })
        |> RequestError.Validation
      )

  let validateRequest (request: HttpRequest) (body: 'a) : Task<Result<ValidRequest<'a>, RequestError<_>>> =
    validateUser authnService request
    |> Task.map (Result.bind (fun user -> validateBody body |> Result.map (fun body -> { User = user; Body = body })))

  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) = task {
      let! user = userRepo.LoadUser token.UserId

      return! presetRepo.ListUserPresets user.Id
    }

    validateUser authnService request
    |> Task.bind (Result.taskMap handler)
    |> Task.map (function
      | Ok presets -> OkObjectResult(presets) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(Operation e) -> BadRequestObjectResult(e) :> IActionResult)

  [<Function("GetPreset")>]
  member this.GetPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> taskResult {
        let! user = userRepo.LoadUser token.UserId

        let! preset =
          presetService.GetPreset(user.Id, presetId)
          |> TaskResult.mapError RequestError.Operation

        return preset
      }

    validateUser authnService request
    |> TaskResult.bind (flip handler (PresetId presetId))
    |> Task.map (function
      | Ok preset -> OkObjectResult(preset) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.Forbidden) -> ForbidResult() :> IActionResult)

  [<Function("CreatePreset")>]
  member this.CreatePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets")>] request: HttpRequest, [<FromBody>] body: CreatePresetRequest)
    : Task<IActionResult> =
    let handler (token: TokenUser) (body: CreatePresetRequest) = task {
      let! user = userRepo.LoadUser token.UserId

      let! newPreset = presetService.CreatePreset(user.Id, body.Name)

      return newPreset
    }

    validateRequest request body
    |> TaskResult.taskMap (fun { User = user; Body = body } -> handler user body)
    |> Task.map (function
      | Ok result -> CreatedResult("presets", { Id = result.Id }) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(Operation e) -> BadRequestObjectResult(e) :> IActionResult)

  [<Function("DeletePreset")>]
  member this.DeletePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "DELETE", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (token: TokenUser) =
      fun presetId -> task {
        let cmd: RemovePreset.Cmd =
          {
            UserId = token.UserId
            PresetId = presetId
          }

        let! result = mediator.Send cmd

        return result |> Result.mapError RequestError.Operation
      }

    validateUser authnService request
    |> TaskResult.bind (flip handler (PresetId presetId))
    |> Task.map (function
      | Ok _ -> NoContentResult() :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error Unauthorized -> UnauthorizedResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.Forbidden) -> ForbidResult() :> IActionResult)