namespace Functions.Generator

open System
open App
open Domain.Core
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging

type Functions(notificationSender: INotificationSender, mediator: IMediator, _logger: ILogger<Functions>) =
  [<Function("GenerateAsync")>]
  member this.GenerateAsync([<QueueTrigger("%Storage:QueueName%")>] command: {| UserId: Guid; PresetId: string |}, _: FunctionContext) =
    _logger.LogInformation("Running playlist generation for user {UserId} and preset %s{PresetId}", command.UserId, command.PresetId)

    let userId = command.UserId |> UserId
    let presetId = command.PresetId |> PresetId

    task {
      do!
        notificationSender.Send
          {
            UserId = userId
            Data = RunPreset.Event.Started
          }

      let cmd: RunPreset.Cmd = { UserId = userId; PresetId = presetId }

      let! runResult: Result<Preset, RunPreset.Error> = mediator.Send cmd

      do!
        notificationSender.Send
          {
            UserId = userId
            Data = RunPreset.Event.Finished(runResult |> Result.map Preset.toSimple)
          }
    }