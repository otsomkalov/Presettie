module Infrastructure.Telegram.Services

open System.Reflection
open FSharp
open Microsoft.ApplicationInsights
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
open otsom.fs.Telegram.Bot.Core
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
    sendUserMessage: SendUserMessage,
    replyToUserMessage: ReplyToUserMessage,
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

    let replyToMessage = replyToUserMessage userId message.MessageId
    let sendLoginMessage = Telegram.Workflows.sendLoginMessage initAuth sendLink

    fun m ->
      let chatCtx = buildChatContext m.ChatId

      task {
        let! musicPlatform = buildMusicPlatform musicPlatformUserId

        return!
          getSpotifyClient musicPlatformUserId
          |> Task.bind (function
            | Some client ->
              let includePlaylist =
                Playlist.includePlaylist musicPlatform parsePlaylistId presetRepo

              let includePlaylist =
                Workflows.CurrentPreset.includePlaylist replyToMessage getUser includePlaylist initAuth sendLink

              let excludePlaylist =
                Playlist.excludePlaylist musicPlatform parsePlaylistId presetRepo

              let excludePlaylist =
                Workflows.CurrentPreset.excludePlaylist replyToMessage getUser excludePlaylist initAuth sendLink

              let targetPlaylist =
                Playlist.targetPlaylist musicPlatform parsePlaylistId presetRepo

              let targetPlaylist =
                Workflows.CurrentPreset.targetPlaylist replyToMessage getUser targetPlaylist initAuth sendLink

              let queuePresetRun = PresetRepo.queueRun _queueClient userId

              let queuePresetRun =
                Domain.Workflows.Preset.queueRun getPreset validatePreset queuePresetRun

              let queueCurrentPresetRun =
                Workflows.User.queueCurrentPresetRun chatCtx queuePresetRun getUser (fun _ -> Task.FromResult())

              match isNull message.ReplyToMessage with
              | false ->
                match message.ReplyToMessage.Text with
                | Equals Messages.SendIncludedPlaylist -> includePlaylist userId (Playlist.RawPlaylistId message.Text)
                | Equals Messages.SendExcludedPlaylist -> excludePlaylist userId (Playlist.RawPlaylistId message.Text)
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
                    | Auth.CompleteError.StateNotFound -> replyToMessage "State not found. Try to login via fresh link."
                    | Auth.CompleteError.StateDoesntBelongToUser ->
                      replyToMessage "State provided does not belong to your login request. Try to login via fresh link."

                  completeAuth (userId |> UserId.value |> string |> AccountId) state
                  |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)
                | Equals "/generate" -> queueCurrentPresetRun userId (ChatMessageId message.MessageId)
                | Equals "/version" ->
                  sendUserMessage
                    userId
                    (Assembly
                      .GetExecutingAssembly()
                      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                      .InformationalVersion)
                  |> Task.ignore
                | CommandWithData "/include" rawPlaylistId ->
                  if String.IsNullOrEmpty rawPlaylistId then
                    replyToMessage "You have entered empty playlist url" |> Task.ignore
                  else
                    includePlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId) |> Task.ignore
                | CommandWithData "/exclude" rawPlaylistId ->
                  if String.IsNullOrEmpty rawPlaylistId then
                    replyToMessage "You have entered empty playlist url" |> Task.ignore
                  else
                    excludePlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
                | CommandWithData "/target" rawPlaylistId ->
                  if String.IsNullOrEmpty rawPlaylistId then
                    replyToMessage "You have entered empty playlist url" |> Task.ignore
                  else
                    targetPlaylist userId (rawPlaylistId |> Playlist.RawPlaylistId)
                | Equals Buttons.SetPresetSize -> chatCtx.AskForReply Messages.SendPresetSize
                | Equals Buttons.RunPreset -> queueCurrentPresetRun userId (ChatMessageId message.MessageId)

                | _ -> replyToMessage "Unknown command" |> Task.ignore
            | None ->
              match isNull message.ReplyToMessage with
              | false ->
                match message.ReplyToMessage.Text with
                | Equals Messages.SendIncludedPlaylist
                | Equals Messages.SendExcludedPlaylist
                | Equals Messages.SendTargetedPlaylist -> sendLoginMessage userId &|> ignore
                | _ -> replyToMessage "Unknown command" |> Task.ignore
              | _ ->
                match message.Text with
                | StartsWith "/include"
                | StartsWith "/exclude"
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
                    | Auth.CompleteError.StateNotFound -> replyToMessage "State not found. Try to login via fresh link."
                    | Auth.CompleteError.StateDoesntBelongToUser ->
                      replyToMessage "State provided does not belong to your login request. Try to login via fresh link."

                  completeAuth (userId |> UserId.value |> string |> AccountId) state
                  |> TaskResult.taskEither processSuccessfulLogin (sendErrorMessage >> Task.ignore)
                | Equals Buttons.SetPresetSize -> chatCtx.AskForReply Messages.SendPresetSize

                | _ -> replyToMessage "Unknown command" |> Task.ignore)
      }


  member this.ProcessAsync(message: Telegram.Bot.Types.Message) =
    let chatId = message.Chat.Id |> ChatId

    let message' =
      { ChatId = chatId
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
    _connectionMultiplexer: IConnectionMultiplexer,
    _database: IMongoDatabase,
    telemetryClient: TelemetryClient,
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

    let countPlaylistTracks =
      Playlist.countTracks telemetryClient _connectionMultiplexer

    let chatCtx = buildChatContext chatId
    let botMessageCtx = chatCtx.BuildBotMessageContext (callbackQuery.Message.MessageId |> BotMessageId)

    let showIncludedPlaylist =
      Workflows.IncludedPlaylist.show botMessageCtx presetRepo countPlaylistTracks

    let showExcludedPlaylist =
      Workflows.ExcludedPlaylist.show botMessageCtx presetRepo countPlaylistTracks

    let showTargetedPlaylist =
      Workflows.TargetedPlaylist.show botMessageCtx presetRepo countPlaylistTracks

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
      | Action.EnableIncludedPlaylist(presetId, playlistId) ->
        let enableIncludedPlaylist = IncludedPlaylist.enable presetRepo

        let enableIncludedPlaylist =
          Workflows.IncludedPlaylist.enable enableIncludedPlaylist showNotification showIncludedPlaylist

        enableIncludedPlaylist presetId playlistId
      | Action.DisableIncludedPlaylist(presetId, playlistId) ->
        let disableIncludedPlaylist = IncludedPlaylist.disable presetRepo

        let disableIncludedPlaylist =
          Workflows.IncludedPlaylist.disable disableIncludedPlaylist showNotification showIncludedPlaylist

        disableIncludedPlaylist presetId playlistId
      | Action.ExcludedPlaylist(ExcludedPlaylistActions.List(presetId, page)) ->
        let listExcludedPlaylists =
          Workflows.ExcludedPlaylist.list presetRepo botMessageCtx
        listExcludedPlaylists presetId page
      | Action.EnableExcludedPlaylist(presetId, playlistId) ->
        let enableExcludedPlaylist = ExcludedPlaylist.enable presetRepo

        let enableExcludedPlaylist =
          Workflows.ExcludedPlaylist.enable enableExcludedPlaylist showNotification showExcludedPlaylist

        enableExcludedPlaylist presetId playlistId
      | Action.DisableExcludedPlaylist(presetId, playlistId) ->
        let disableExcludedPlaylist = ExcludedPlaylist.disable presetRepo

        let disableExcludedPlaylist =
          Workflows.ExcludedPlaylist.disable disableExcludedPlaylist showNotification showExcludedPlaylist

        disableExcludedPlaylist presetId playlistId
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