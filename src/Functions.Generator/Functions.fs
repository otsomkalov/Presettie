namespace Functions.Generator

open System
open Bot
open Bot.Repos
open Domain.Core
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Telegram.Bot
open otsom.fs.Resources
open otsom.fs.Bot

type Functions
  (
    _bot: ITelegramBotClient,
    _logger: ILogger<Functions>,
    buildChatContext: BuildBotService,
    chatRepo: IChatRepo,
    presetService: IPresetService,
    getResp: CreateResourceProvider
  ) =
  let runPreset resp =
    fun userId presetId chatId -> task {
      let chatCtx = buildChatContext chatId

      do! Workflows.Preset.run resp chatCtx presetService (userId, presetId)
    }

  [<Function("GenerateAsync")>]
  member this.GenerateAsync([<QueueTrigger("%Storage:QueueName%")>] command: {| UserId: Guid; PresetId: string |}, _: FunctionContext) =
    _logger.LogInformation("Running playlist generation for user {UserId} and preset %s{PresetId}", command.UserId, command.PresetId)

    let userId = command.UserId |> UserId
    let presetId = command.PresetId |> PresetId

    task {
      let! chat = chatRepo.LoadUserChat userId

      match chat with
      | Some chat ->
        let! resp = getResp chat.Lang
        do! runPreset resp userId presetId chat.Id
      | None -> _logger.LogWarning("No chat found for user with id {UserId}", command.UserId)
    }