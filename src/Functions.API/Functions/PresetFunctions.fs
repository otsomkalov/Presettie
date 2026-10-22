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
open Runners.Shared
open otsom.fs.Extensions
open FsToolkit.ErrorHandling

[<CLIMutable>]
type CreatePresetRequest =
  {
    [<Required; MinLength(3)>]
    Name: string
  }

type CreatePresetResponse = { Id: PresetId }

type PresetFunctions(presetRepo: IPresetRepo, presetService: IPresetService, mediator: IMediator) =
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

  [<Function("RunPreset")>]
  member this.RunPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/run")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (userId: UserId) = task {
      return!
        presetService.QueueRun(userId, PresetId presetId)
        |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation errors -> BadRequestObjectResult(errors) :> IActionResult)

  [<Function("IncludeArtist")>]
  member this.IncludeArtist
    (
      [<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/included-artists")>] request: HttpRequest,
      presetId: string,
      [<FromBody>] body: IncludeArtistRequest
    ) : Task<IActionResult> =
    let handler (userId: UserId) = taskResult {
      let cmd: IncludeArtist.Cmd =
        {
          UserId = userId
          PresetId = PresetId presetId
          ArtistId = body.Id
        }

      return! presetService.IncludeArtist(cmd) |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation error -> BadRequestObjectResult(error) :> IActionResult)

  [<Function("IncludePlaylist")>]
  member this.IncludePlaylist
    (
      [<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/included-playlists")>] request: HttpRequest,
      presetId: string,
      [<FromBody>] body: IncludePlaylistRequest
    ) : Task<IActionResult> =
    let handler (userId: UserId) = taskResult {
      let cmd: IncludePlaylist.Cmd =
        {
          UserId = userId
          PresetId = PresetId presetId
          PlaylistId = body.Id
        }

      return! presetService.IncludePlaylist(cmd) |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation error -> BadRequestObjectResult(error) :> IActionResult)

  [<Function("ExcludePlaylist")>]
  member this.ExcludePlaylist
    (
      [<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/excluded-playlists")>] request: HttpRequest,
      presetId: string,
      [<FromBody>] body: ExcludePlaylistRequest
    ) : Task<IActionResult> =
    let handler (userId: UserId) = taskResult {
      let cmd: ExcludePlaylist.Cmd =
        {
          UserId = userId
          PresetId = PresetId presetId
          PlaylistId = body.Id
        }

      return! presetService.ExcludePlaylist(cmd) |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation error -> BadRequestObjectResult(error) :> IActionResult)

  [<Function("ExcludeArtist")>]
  member this.ExcludeArtist
    (
      [<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/excluded-artists")>] request: HttpRequest,
      presetId: string,
      [<FromBody>] body: ExcludeArtistRequest
    ) : Task<IActionResult> =
    let handler (userId: UserId) = taskResult {
      let cmd: ExcludeArtist.Cmd =
        {
          UserId = userId
          PresetId = PresetId presetId
          ArtistId = body.Id
        }

      return! presetService.ExcludeArtist(cmd) |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation error -> BadRequestObjectResult(error) :> IActionResult)

  [<Function("TargetPlaylist")>]
  member this.TargetPlaylist
    (
      [<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets/{presetId}/targeted-playlists")>] request: HttpRequest,
      presetId: string,
      [<FromBody>] body: TargetPlaylistRequest
    ) : Task<IActionResult> =
    let handler (userId: UserId) = taskResult {
      let cmd: TargetPlaylist.Cmd =
        {
          UserId = userId
          PresetId = PresetId presetId
          PlaylistId = body.Id
        }

      return! presetService.TargetPlaylist(cmd) |> TaskResult.mapError Operation
    }

    handler request.UserId
    |> TaskResult.foldResult (fun preset -> OkObjectResult(preset) :> IActionResult) (function
      | Operation error -> BadRequestObjectResult(error) :> IActionResult)


  [<Function("ListPresets")>]
  member this.ListPresets
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets")>] request: HttpRequest)
    : Task<IActionResult> =
    let handler (userId: UserId) = task { return! presetRepo.ListUserPresets userId }

    handler request.UserId
    |> Task.map (fun presets -> OkObjectResult(presets) :> IActionResult)

  [<Function("GetPreset")>]
  member this.GetPreset
    ([<HttpTrigger(AuthorizationLevel.Function, "GET", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (userId: UserId) =
      fun presetId -> taskResult {
        let! preset =
          presetService.GetPreset(userId, presetId)
          |> TaskResult.mapError RequestError.Operation

        return preset
      }

    handler request.UserId (PresetId presetId)
    |> Task.map (function
      | Ok preset -> OkObjectResult(preset) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error(Operation Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.Forbidden) -> ForbidResult() :> IActionResult)

  [<Function("CreatePreset")>]
  member this.CreatePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "POST", Route = "presets")>] request: HttpRequest, [<FromBody>] body: CreatePresetRequest)
    : Task<IActionResult> =
    let handler (userId: UserId) (body: CreatePresetRequest) = task {
      let! newPreset = presetService.CreatePreset(userId, body.Name)

      return newPreset
    }

    validateBody body
    |> Result.taskMap (fun body -> handler request.UserId body)
    |> Task.map (function
      | Ok result -> CreatedResult("presets", { Id = result.Id }) :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error(Operation e) -> BadRequestObjectResult(e) :> IActionResult)

  [<Function("DeletePreset")>]
  member this.DeletePreset
    ([<HttpTrigger(AuthorizationLevel.Function, "DELETE", Route = "presets/{presetId}")>] request: HttpRequest, presetId: string)
    : Task<IActionResult> =
    let handler (userId: UserId) =
      fun presetId -> task {
        let cmd: RemovePreset.Cmd = { UserId = userId; PresetId = presetId }

        let! result = mediator.Send cmd

        return result |> Result.mapError RequestError.Operation
      }

    handler request.UserId (PresetId presetId)
    |> Task.map (function
      | Ok() -> NoContentResult() :> IActionResult
      | Error(Validation errors) -> BadRequestObjectResult(errors) :> IActionResult
      | Error(Operation Preset.GetPresetError.NotFound) -> NotFoundResult() :> IActionResult
      | Error(Operation Preset.GetPresetError.Forbidden) -> ForbidResult() :> IActionResult)