module Infrastructure.Telegram.Services

open System.Reflection
open FSharp
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open MusicPlatform.Spotify
open MusicPlatform.Spotify.Core
open Resources
open Telegram
open Infrastructure
open Infrastructure.Workflows
open System.Threading.Tasks
open Azure.Storage.Queues
open Domain.Core
open Domain.Workflows
open MongoDB.Driver
open StackExchange.Redis
open Telegram.Bot
open Telegram.Bot.Types
open Telegram.Core
open System
open Telegram.Repos
open otsom.fs.Extensions
open otsom.fs.Extensions.String
open otsom.fs.Telegram.Bot.Auth.Spotify
open MusicPlatform
open otsom.fs.Core
open Infrastructure.Repos
open Domain.Repos
open otsom.fs.Bot
open Telegram.Helpers

type AuthState =
  | Authorized
  | Unauthorized

type MessageService
  (
    _database: IMongoDatabase,
    _queueClient: QueueClient,
    initAuth: Auth.Init,
    completeAuth: Auth.Complete,
    getSpotifyClient: GetClient,
    getPreset: Preset.Get,
    validatePreset: Preset.Validate,
    parsePlaylistId: Playlist.ParseId,
    buildMusicPlatform: BuildMusicPlatform,
    buildChatContext: BuildChatContext,
    logger: ILogger<MessageService>,
    presetRepo: IPresetRepo,
    getUser: User.Get,
    handlersFactories: MessageHandlerFactory seq,
    sendLink: SendLink
  ) =

  let defaultMessageHandler (message: Telegram.Bot.Types.Message) =
    let userId = message.From.Id |> UserId
    let musicPlatformUserId = message.From.Id |> string |> MusicPlatform.UserId

    let sendLoginMessage = Telegram.Workflows.sendLoginMessage initAuth sendLink

    fun m ->
      let chatCtx = buildChatContext m.ChatId
      let chatMessageCtx = chatCtx.BuildChatMessageContext m.Id

      task {
        let! musicPlatform = buildMusicPlatform musicPlatformUserId

        return!
          getSpotifyClient musicPlatformUserId
          |> Task.bind (function
            | Some client ->
              let targetPlaylist =
                Playlist.targetPlaylist musicPlatform parsePlaylistId presetRepo

              let targetPlaylist =
                Workflows.CurrentPreset.targetPlaylist chatMessageCtx getUser targetPlaylist initAuth sendLink

              let queuePresetRun = PresetRepo.queueRun _queueClient userId

              let queuePresetRun =
                Domain.Workflows.Preset.queueRun getPreset validatePreset queuePresetRun

              let queueCurrentPresetRun =
                Workflows.User.queueCurrentPresetRun chatCtx queuePresetRun getUser (fun _ -> Task.FromResult())

              match isNull message.ReplyToMessage with
              | false ->
                match message.ReplyToMessage.Text with
                | Equals Messages.SendTargetedPlaylist -> targetPlaylist userId (Playlist.RawPlaylistId message.Text)
              | _ ->
                match message.Text with
                | Equals "/start" -> Telegram.Workflows.User.sendCurrentPreset getUser getPreset chatCtx userId
                | CommandWithData "/start" state ->
                  let processSuccessfulLogin =
                    let create = UserRepo.create _database
                    let exists = UserRepo.exists _database
                    let createUserIfNotExists = User.createIfNotExists exists create

                    fun () -> task {
                      do! createUserIfNotExists userId
                      do! Telegram.Workflows.User.sendCurrentPreset getUser getPreset chatCtx userId
                    }

                  let sendErrorMessage =
                    function
                    | Auth.CompleteError.StateNotFound -> chatMessageCtx.ReplyToMessage "State not found. Try to login via fresh link."
                    | Auth.CompleteError.StateDoesntBelongToUser ->
                      chatMessageCtx.ReplyToMessage "State provided does not belong to your login request. Try to login via fresh link."

                  completeAuth (userId |> UserId.value |> string |> AccountId) state
                  |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)
                | Equals "/generate" -> queueCurrentPresetRun userId
                | Equals "/version" ->
                  chatMessageCtx.ReplyToMessage
                    (Assembly
                      .GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      .InformationalVersion)
                  |> Task.ignore
                | CommandWithData "/target" rawPlaylistId ->
                  if String.IsNullOrEmpty rawPlaylistId then
                    chatMessageCtx.ReplyToMessage "You have entered empty playlist url" |> Task.ignore
                  else
                    targetPlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
                | Equals Buttons.SetPresetSize -> chatCtx.AskForReply Messages.SendPresetSize
                | Equals Buttons.RunPreset -> queueCurrentPresetRun userId

                | _ -> chatMessageCtx.ReplyToMessage "Unknown command" |> Task.ignore
            | None ->
              match isNull message.ReplyToMessage with
              | false ->
                match message.ReplyToMessage.Text with
                | Equals Messages.SendTargetedPlaylist -> sendLoginMessage userId &|> ignore
                | _ -> chatMessageCtx.ReplyToMessage "Unknown command" |> Task.ignore
              | _ ->
                match message.Text with
                | StartsWith "/target"
                | Equals Buttons.RunPreset
                | StartsWith "/generate"
                | Equals "/start" -> sendLoginMessage userId &|> ignore

                | CommandWithData "/start" state ->
                  let processSuccessfulLogin =
                    let create = UserRepo.create _database
                    let exists = UserRepo.exists _database
                    let createUserIfNotExists = User.createIfNotExists exists create

                    fun () -> task {
                      do! createUserIfNotExists userId
                      do! Telegram.Workflows.User.sendCurrentPreset getUser getPreset chatCtx userId
                    }

                  let sendErrorMessage =
                    function
                    | Auth.CompleteError.StateNotFound -> chatMessageCtx.ReplyToMessage "State not found. Try to login via fresh link."
                    | Auth.CompleteError.StateDoesntBelongToUser ->
                      chatMessageCtx.ReplyToMessage "State provided does not belong to your login request. Try to login via fresh link."

                  completeAuth (userId |> UserId.value |> string |> AccountId) state
                  |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)
                | Equals Buttons.SetPresetSize -> chatCtx.AskForReply Messages.SendPresetSize

                | _ -> chatMessageCtx.ReplyToMessage "Unknown command" |> Task.ignore)
      }


  member this.ProcessAsync(message: Telegram.Bot.Types.Message) =
    let chatId = message.Chat.Id |> ChatId

    let message' =
      { Id = ChatMessageId message.MessageId
        ChatId = chatId
        Text = message.Text
        ReplyMessage =
          message.ReplyToMessage
          |> Option.ofObj
          |> Option.map (fun m -> { Text = m.Text }) }

    let chatCtx = buildChatContext chatId

    let handlers = handlersFactories |> Seq.map (fun f -> f chatCtx)

    task {
      use e = handlers.GetEnumerator()

      let mutable lastHandlerResult = None

      while lastHandlerResult.IsNone && e.MoveNext() do
        let handler = e.Current
        let! currentHandlerResult = handler message'

        lastHandlerResult <- currentHandlerResult

      match lastHandlerResult with
      | Some () ->
        return()
      | None ->
        Logf.logfw logger "Message content didn't match any handler. Running default one."

        return! defaultMessageHandler message message'
    }

type CallbackQueryService
  (
    _bot: ITelegramBotClient,
    _queueClient: QueueClient,
    _database: IMongoDatabase,
    getPreset: Preset.Get,
    buildChatContext: BuildChatContext,
    presetRepo: IPresetRepo,
    getUser: User.Get,
    userRepo: IUserRepo,
    handlersFactories: ClickHandlerFactory seq,
    logger: ILogger<CallbackQueryService>,
    showNotification: ShowNotification
  ) =

  member this.ProcessAsync(callbackQuery: CallbackQuery) =
    let userId = callbackQuery.From.Id |> UserId
    let chatId = callbackQuery.From.Id |> ChatId
    let clickId = callbackQuery.Id |> ClickId

    let showNotification = showNotification clickId

    let chatCtx = buildChatContext chatId
    let botMessageCtx = chatCtx.BuildBotMessageContext (callbackQuery.Message.MessageId |> BotMessageId)

    let click: Click = {
      Id = clickId
      ChatId = chatId
      Data = callbackQuery.Data
    }

    let defaultMessageHandler () =
      match callbackQuery.Data |> Workflows.parseAction with
      | Action.Preset presetAction ->
        match presetAction with
        | PresetActions.Run presetId ->

          let answerCallbackQuery = Telegram.Workflows.answerCallbackQuery _bot callbackQuery.Id
          let queuePresetRun = PresetRepo.queueRun _queueClient userId
          let queuePresetRun = Domain.Workflows.Preset.queueRun getPreset Preset.validate queuePresetRun
          let queuePresetRun = Telegram.Workflows.Preset.queueRun chatCtx queuePresetRun answerCallbackQuery

          queuePresetRun presetId

      | Action.SetCurrentPreset presetId ->
        let setCurrentPreset = Domain.Workflows.User.setCurrentPreset userRepo

        let setCurrentPreset =
          Workflows.User.setCurrentPreset showNotification setCurrentPreset

        setCurrentPreset userId presetId
      | Action.RemovePreset presetId ->
        let removePreset = PresetRepo.remove _database

        let removeUserPreset =
          Domain.Workflows.User.removePreset userRepo removePreset

        let removeUserPreset =
          Telegram.Workflows.User.removePreset botMessageCtx getUser removeUserPreset

        removeUserPreset userId presetId
      | Action.IncludedPlaylist(IncludedPlaylistActions.List(presetId, page)) ->
        let listIncludedPlaylists =
          Workflows.IncludedPlaylist.list presetRepo botMessageCtx
        listIncludedPlaylists presetId page
      | Action.ExcludedPlaylist(ExcludedPlaylistActions.List(presetId, page)) ->
        let listExcludedPlaylists =
          Workflows.ExcludedPlaylist.list presetRepo botMessageCtx
        listExcludedPlaylists presetId page
      | Action.TargetedPlaylist(TargetedPlaylistActions.List(presetId, page)) ->
        let listTargetedPlaylists =
          Workflows.TargetedPlaylist.list presetRepo botMessageCtx
        listTargetedPlaylists presetId page

    let handlers = handlersFactories |> Seq.map (fun f -> f botMessageCtx)

    task {
      use e = handlers.GetEnumerator()

      let mutable lastHandlerResult = None

      while lastHandlerResult.IsNone && e.MoveNext() do
        let handler = e.Current
        let! currentHandlerResult = handler click

        lastHandlerResult <- currentHandlerResult

      match lastHandlerResult with
      | Some () ->
        return()
      | None ->
        Logf.logfw logger "Button click data didn't match any handler. Running default one."

        return! defaultMessageHandler ()
    }