module Bot.Telegram.Services

open System
open Bot.Handlers.Click
open Domain.Core
open Domain.Repos
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open MusicPlatform
open Telegram.Bot.Types
open Bot.Core
open Bot.Repos
open Bot.Resources
open otsom.fs.Bot.Builders
open otsom.fs.Extensions
open otsom.fs.Bot
open FsToolkit.ErrorHandling

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
        logger.LogWarning "Message content didn't match any handler. Running default one."

        return! chatCtx.SendMessage resp[Messages.UnknownCommand] |> Task.map ignore
    }

type CallbackQueryService
  (
    buildBotService: BuildBotService,
    logger: ILogger<CallbackQueryService>,
    chatRepo: IChatRepo,
    chatService: IChatService,
    getResp: Resources.GetResourceProvider,
    presetRepo: IPresetRepo,
    presetService: IPresetService,
    buildMusicPlatform: IMusicPlatformFactory,
    userService: IUserService
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
          MessageId = BotMessageId callbackQuery.Message.MessageId
          Data = callbackQuery.Data.Split("|") |> List.ofArray }

      let! resp = getResp lang

      let handlers = clickHandlers {
        listPresetsClickHandler presetRepo resp botService
        presetInfoClickHandler presetRepo resp botService
        presetSettingsClickHandler presetRepo resp botService
        runPresetClickHandler presetService resp botService
        removePresetClickHandler presetRepo userService resp botService
        setCurrentPresetClickHandler userService resp botService

        artistsAlbumsRecommendationsClickHandler presetRepo presetService resp botService
        reccoBeatsRecommendationsClickHandler presetRepo presetService resp botService
        spotifyRecommendationsClickHandler presetRepo presetService resp botService
        disableRecommendationsClickHandler presetRepo presetService resp botService

        enableUniqueArtistsClickHandler presetRepo presetService resp botService
        disableUniqueArtistsClickHandler presetRepo presetService resp botService

        includeLikedTracksClickHandler presetRepo presetService resp botService
        excludeLikedTracksClickHandler presetRepo presetService resp botService
        ignoreLikedTracksClickHandler presetRepo presetService resp botService

        appendToTargetedPlaylistClickHandler presetRepo presetService buildMusicPlatform resp botService
        overwriteTargetedPlaylistClickHandler presetRepo presetService buildMusicPlatform resp botService

        showIncludedContentClickHandler presetRepo resp botService
        showExcludedContentClickHandler presetRepo resp botService

        listIncludedArtistsClickHandler presetRepo resp botService
        listExcludedArtistsClickHandler presetRepo resp botService

        showIncludedArtistClickHandler presetRepo resp botService
        showExcludedArtistClickHandler presetRepo resp botService

        listIncludedPlaylistsClickHandler presetRepo resp botService
        listExcludedPlaylistsClickHandler presetRepo resp botService
        listTargetedPlaylistsClickHandler presetRepo resp botService

        showIncludedPlaylistClickHandler presetRepo buildMusicPlatform resp botService
        showExcludedPlaylistClickHandler presetRepo buildMusicPlatform resp botService
        showTargetedPlaylistClickHandler presetRepo buildMusicPlatform resp botService

        removeIncludedArtistClickHandler presetService resp botService
        removeExcludedArtistClickHandler presetService resp botService

        removeIncludedPlaylistClickHandler presetService resp botService
        removeExcludedPlaylistClickHandler presetService resp botService
        removeTargetedPlaylistClickHandler presetService resp botService
      }

      let! result = handlers chat click

      match result with
      | Some() -> return ()
      | None ->
        logger.LogWarning "Button click data didn't match any handler. Running default one."

        return! botService.SendNotification(clickId, resp[Notifications.UnknownCommand])
    }