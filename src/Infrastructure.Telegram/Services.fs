module Infrastructure.Telegram.Services

open System
open FSharp
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open Infrastructure
open Telegram.Bot.Types
open Telegram.Core
open Telegram.Repos
open Telegram.Resources
open otsom.fs.Extensions
open otsom.fs.Bot

type MessageService
  (
    buildChatContext: BuildBotService,
    logger: ILogger<MessageService>,
    handlersFactories: MessageHandlerFactory seq,
    chatRepo: IChatRepo,
    chatService: IChatService,
    getResp: Resources.GetResourceProvider
  ) =

  member this.ProcessAsync(message: Telegram.Bot.Types.Message) =
    let chatId = message.Chat.Id |> ChatId
    let chatCtx = buildChatContext chatId

    let lang =
      message.From
      |> Option.ofObj
      |> Option.bind (fun u ->
        u.LanguageCode
        |> Option.ofObj
        |> Option.bind (Option.someIf (String.IsNullOrEmpty >> not)))

    task {
      let! chat =
        chatRepo.LoadChat chatId
        |> Task.bind (Option.defaultWithTask (fun () -> chatService.CreateChat(chatId, lang)))

      let message' =
        { Id = ChatMessageId message.MessageId
          Chat = chat
          Text = message.Text
          ReplyMessage =
            message.ReplyToMessage
            |> Option.ofObj
            |> Option.map (fun m -> { Text = m.Text }) }

      let! resp = getResp lang

      let handlers = handlersFactories |> Seq.map (fun f -> f resp chatCtx)

      use e = handlers.GetEnumerator()

      let mutable lastHandlerResult = None

      while lastHandlerResult.IsNone && e.MoveNext() do
        let handler = e.Current
        let! currentHandlerResult = handler message'

        lastHandlerResult <- currentHandlerResult

      match lastHandlerResult with
      | Some() -> return ()
      | None ->
        Logf.logfw logger "Message content didn't match any handler. Running default one."

        return! chatCtx.SendMessage resp[Messages.UnknownCommand] &|> ignore
    }

type CallbackQueryService
  (
    buildBotService: BuildBotService,
    handlersFactories: ClickHandlerFactory seq,
    logger: ILogger<CallbackQueryService>,
    chatRepo: IChatRepo,
    chatService: IChatService,
    getResp: Resources.GetResourceProvider
  ) =

  member this.ProcessAsync(callbackQuery: CallbackQuery) =
    let chatId = callbackQuery.From.Id |> ChatId
    let clickId = callbackQuery.Id |> ButtonClickId

    let botService = buildBotService chatId

    let lang =
      callbackQuery.Message.From
      |> Option.ofObj
      |> Option.bind (fun u ->
        u.LanguageCode
        |> Option.ofObj
        |> Option.bind (Option.someIf (String.IsNullOrEmpty >> not)))

    task {
      let! chat =
        chatRepo.LoadChat chatId
        |> Task.bind (Option.defaultWithTask (fun () -> chatService.CreateChat(chatId, lang)))

      let click: Click =
        { Id = clickId
          Chat = chat
          MessageId = BotMessageId callbackQuery.Message.MessageId
          Data = callbackQuery.Data.Split("|") |> List.ofArray }

      let! resp = getResp lang

      let handlers = handlersFactories |> Seq.map (fun f -> f resp botService)

      use e = handlers.GetEnumerator()

      let mutable lastHandlerResult = None

      while lastHandlerResult.IsNone && e.MoveNext() do
        let handler = e.Current
        let! currentHandlerResult = handler click

        lastHandlerResult <- currentHandlerResult

      match lastHandlerResult with
      | Some() -> return ()
      | None ->
        Logf.logfw logger "Button click data didn't match any handler. Running default one."

        return! botService.SendNotification(clickId, resp[Messages.UnknownCommand])
    }