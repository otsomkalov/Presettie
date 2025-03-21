﻿namespace Generator.Functions

open FSharp
open Infrastructure
open Microsoft.Azure.Functions.Worker
open Microsoft.Extensions.Logging
open Domain.Core
open Telegram.Bot
open Telegram.Core
open Telegram.Repos
open otsom.fs.Bot
open otsom.fs.Core

type GeneratorFunctions
  (
    _bot: ITelegramBotClient,
    _logger: ILogger<GeneratorFunctions>,
    buildChatContext: BuildBotService,
    chatRepo: IChatRepo,
    presetService: IPresetService
  ) =

  let runPreset userId presetId chatId = task {
    let chatCtx = buildChatContext chatId

    do! Telegram.Workflows.Preset.run chatCtx presetService (userId, presetId)
  }

  [<Function("GenerateAsync")>]
  member this.GenerateAsync
    (
      [<QueueTrigger("%Storage:QueueName%")>] command:
        {| UserId: string
           PresetId: string |},
      _: FunctionContext
    ) =
    Logf.logfi _logger "Running playlist generation for user %s{UserId} and preset %s{PresetId}" command.UserId command.PresetId

    let userId = command.UserId |> UserId
    let presetId = command.PresetId |> PresetId

    task {
      let! chat = chatRepo.LoadUserChat userId

      match chat with
      | Some chat -> do! runPreset userId presetId chat.Id
      | None -> Logf.logfw _logger "No chat found for user with id %s{UserId}" command.UserId
    }