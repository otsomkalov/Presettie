namespace Functions.Bot.Telegram.Functions

open App
open Bot.Repos
open Bot.Resources
open Domain.Core
open Domain.Repos
open FSharp.Control
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open otsom.fs.Bot
open otsom.fs.Resources

type NotificationFunctions
  (
    chatRepo: IChatRepo,
    buildBotService: BuildBotService,
    createResourceProvider: CreateDefaultResourceProvider,
    logger: ILogger<NotificationFunctions>
  ) =
  [<Function("PresetRunEventHandler")>]
  member this.PresetRunEventHandler
    ([<QueueTrigger("%Storage:NotificationsQueueName%")>] event: DomainEvent<RunPreset.Event>, ctx: FunctionContext)
    = task {
    let! chat = chatRepo.LoadUserChat event.UserId

    match chat with
    | Some chat ->

      let chatCtx = buildBotService chat.Id
      let! resp = createResourceProvider ()

      match event.Data with
      | RunPreset.Event.Started -> return! chatCtx.SendMessage(resp[Messages.RunningPreset]) |> Task.ignore
      | RunPreset.Event.Finished(Ok preset) ->
        return!
          chatCtx.SendMessage resp[Messages.PresetExecuted, [| preset.Name |]]
          |> Task.ignore
      | RunPreset.Event.Finished(Error(RunPreset.Error.NoIncludedTracks)) ->
        return! chatCtx.SendMessage resp[Messages.NoIncludedTracks] |> Task.ignore
      | RunPreset.Event.Finished(Error(RunPreset.Error.NoPotentialTracks)) ->
        return! chatCtx.SendMessage resp[Messages.NoPotentialTracks] |> Task.ignore
      | RunPreset.Event.Finished(Error(RunPreset.Error.NoMusicPlatform)) ->
        return! chatCtx.SendMessage resp[Messages.NotAuthorized] |> Task.ignore
      | RunPreset.Event.Finished(Error(RunPreset.Error.Preset(Preset.GetPresetError.NotFound))) ->
        return! chatCtx.SendMessage resp[Messages.PresetNotFound] |> Task.ignore
      | RunPreset.Event.Finished(Error(RunPreset.Error.Preset(Preset.GetPresetError.Forbidden))) ->
        return! chatCtx.SendMessage resp[Messages.PresetAccessForbidden] |> Task.ignore
    | None ->
      logger.LogWarning("No chat found for user {UserId}. Skipping notification in Bot", event.UserId)

      return ()
  }