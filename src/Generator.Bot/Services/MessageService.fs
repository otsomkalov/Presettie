﻿namespace Generator.Bot.Services

open System.Threading.Tasks
open Domain.Core
open Domain.Workflows
open Infrastructure.Workflows
open Generator.Bot
open Generator.Bot.Services
open Generator.Bot.Services.Playlist
open Microsoft.Extensions.Options
open Microsoft.FSharp.Core
open MongoDB.Driver
open Shared.Services
open Shared.Settings
open StackExchange.Redis
open Telegram.Bot
open Telegram.Bot.Types
open Generator.Bot.Helpers
open Resources
open Telegram.Bot.Types.Enums
open Domain.Extensions
open Telegram.Helpers

type AuthState =
  | Authorized
  | Unauthorized

type MessageService
  (
    _generateCommandHandler: GenerateCommandHandler,
    _addSourcePlaylistCommandHandler: AddSourcePlaylistCommandHandler,
    _addHistoryPlaylistCommandHandler: AddHistoryPlaylistCommandHandler,
    _setTargetedPlaylistCommandHandler: SetTargetPlaylistCommandHandler,
    _spotifyClientProvider: SpotifyClientProvider,
    _bot: ITelegramBotClient,
    loadUser: User.Load,
    loadPreset: Preset.Load,
    updatePreset: Preset.Update,
    _database: IMongoDatabase,
    _connectionMultiplexer: IConnectionMultiplexer,
    _spotifyOptions: IOptions<SpotifySettings>
  ) =

  let sendUserPresets sendMessage (message: Message) =
    let sendUserPresets = Telegram.Workflows.sendUserPresets sendMessage loadUser
    sendUserPresets (message.From.Id |> UserId)

  let includePlaylist replyToMessage (message: Message) =
    match message.Text with
    | CommandData data -> _addSourcePlaylistCommandHandler.HandleAsync replyToMessage data message
    | _ -> replyToMessage "You have entered empty playlist url"

  let excludePlaylist replyToMessage (message: Message) =
    match message.Text with
    | CommandData data -> _addHistoryPlaylistCommandHandler.HandleAsync replyToMessage data message
    | _ -> replyToMessage "You have entered empty playlist url"

  let targetPlaylist replyToMessage (message: Message) =
    match message.Text with
    | CommandData data -> _setTargetedPlaylistCommandHandler.HandleAsync replyToMessage data message
    | _ -> replyToMessage "You have entered empty playlist url"

  let validateUserLogin sendLoginMessage handleCommandFunction (message: Message) =
    task{
      let! spotifyClient = _spotifyClientProvider.GetAsync message.From.Id

      return!
        if spotifyClient = null then
          sendLoginMessage()
        else
          handleCommandFunction message
    }

  let getAuthState (message: Message) =
    task{
      let! spotifyClient = _spotifyClientProvider.GetAsync message.From.Id

      return
        if spotifyClient = null then
          AuthState.Unauthorized
        else
          AuthState.Authorized
    }

  member this.ProcessAsync(message: Message) =
    let userId = message.From.Id |> UserId

    let sendMessage = Telegram.sendMessage _bot userId
    let sendLink = Telegram.sendLink _bot userId
    let sendKeyboard = Telegram.sendKeyboard _bot userId
    let replyToMessage = Telegram.replyToMessage _bot userId message.MessageId
    let sendButtons = Telegram.sendButtons _bot userId
    let askForReply = Telegram.askForReply _bot userId message.MessageId
    let savePreset = Preset.save _database
    let updateUser = User.update _database
    let createPreset = Preset.create savePreset loadUser updateUser userId

    let sendCurrentPresetInfo = Telegram.Workflows.sendCurrentPresetInfo loadUser loadPreset sendKeyboard
    let sendSettingsMessage = Telegram.Workflows.sendSettingsMessage loadUser loadPreset sendKeyboard
    let sendPresetInfo =
      Telegram.Workflows.sendPresetInfo loadPreset sendButtons
    let createPreset = Telegram.Workflows.Message.createPreset createPreset sendPresetInfo

    let sendLoginMessage () =
      let initState = Auth.initState _connectionMultiplexer
      let getLoginLink = Auth.getLoginLink _spotifyOptions

      let getLoginLink = Domain.Workflows.Auth.getLoginLink initState getLoginLink

      getLoginLink userId
      |> Task.bind (sendLink Messages.LoginToSpotify Buttons.Login)

    task{
      let! authState = getAuthState message

      return!
        match message.Type with
        | MessageType.Text ->
          match isNull message.ReplyToMessage with
          | false ->
            match (message.ReplyToMessage.Text, authState) with
            | Equals Messages.SendIncludedPlaylist, Unauthorized
            | Equals Messages.SendExcludedPlaylist, Unauthorized
            | Equals Messages.SendTargetedPlaylist, Unauthorized -> sendLoginMessage()

            | Equals Messages.SendPlaylistSize, _ ->
              match message.Text with
              | Int size ->
                let setPlaylistSize = Preset.setPlaylistSize loadPreset updatePreset
                let setPlaylistSize = Telegram.setPlaylistSize sendMessage sendSettingsMessage loadUser setPlaylistSize

                setPlaylistSize userId size
              | _ ->
                replyToMessage Messages.WrongPlaylistSize
            | Equals Messages.SendIncludedPlaylist, Authorized -> _addSourcePlaylistCommandHandler.HandleAsync replyToMessage message.Text message
            | Equals Messages.SendExcludedPlaylist, Authorized -> _addHistoryPlaylistCommandHandler.HandleAsync replyToMessage message.Text message
            | Equals Messages.SendTargetedPlaylist, Authorized -> _setTargetedPlaylistCommandHandler.HandleAsync replyToMessage message.Text message
            | Equals Messages.SendPresetName, _ -> createPreset message.Text

            | _ -> replyToMessage "Unknown command"
          | _ ->
            match (message.Text, authState) with
            | StartsWith "/include", Unauthorized | StartsWith "/exclude", Unauthorized | StartsWith "/target", Unauthorized
            | Equals Buttons.IncludePlaylist, Unauthorized | Equals Buttons.ExcludePlaylist, Unauthorized | Equals Buttons.TargetPlaylist, Unauthorized
            | Equals Buttons.GeneratePlaylist, Unauthorized | StartsWith "/generate", Unauthorized
             -> sendLoginMessage()

            | Equals "/start", Unauthorized ->
              let initState = Auth.initState _connectionMultiplexer
              let getLoginLink = Auth.getLoginLink _spotifyOptions

              let getLoginLink = Domain.Workflows.Auth.getLoginLink initState getLoginLink

              getLoginLink userId
              |> Task.bind (sendLink Messages.Welcome Buttons.Login)
            | Equals "/start", Authorized ->
              sendCurrentPresetInfo userId
            | CommandWithData "/start" state, _ ->
              let tryGetAuth = Auth.tryGetCompletedAuth _connectionMultiplexer
              let getToken = Auth.getToken _spotifyOptions
              let saveCompletedAuth = Auth.saveCompletedAuth _connectionMultiplexer
              let createUserIfNotExists = User.createIfNotExists _database
              let sendErrorMessage =
                function
                | Auth.CompleteError.StateNotFound ->
                  replyToMessage "State not found. Try to login via fresh link."
                | Auth.CompleteError.StateDoesntBelongToUser ->
                  replyToMessage "State provided does not belong to your login request. Try to login via fresh link."

              let completeAuth = Domain.Workflows.Auth.complete tryGetAuth getToken saveCompletedAuth createUserIfNotExists

              completeAuth userId (state |> Auth.State.parse)
              |> TaskResult.taskEither (fun () -> sendCurrentPresetInfo userId) sendErrorMessage
            | Equals "/help", _ ->
              sendMessage Messages.Help
            | Equals "/guide", _ -> sendMessage Messages.Guide
            | Equals "/privacy", _ -> sendMessage Messages.Privacy
            | Equals "/faq", _ -> sendMessage Messages.FAQ
            | StartsWith "/generate", Authorized -> _generateCommandHandler.HandleAsync replyToMessage message
            | StartsWith "/include", Authorized -> includePlaylist replyToMessage message
            | StartsWith "/exclude", Authorized -> excludePlaylist replyToMessage message
            | StartsWith "/target", Authorized -> targetPlaylist replyToMessage message
            | Equals Buttons.SetPlaylistSize, _ -> askForReply Messages.SendPlaylistSize
            | Equals Buttons.CreatePreset, _ -> askForReply Messages.SendPresetName
            | Equals Buttons.GeneratePlaylist, Authorized -> _generateCommandHandler.HandleAsync replyToMessage message
            | Equals Buttons.MyPresets, _ -> sendUserPresets sendButtons message
            | Equals Buttons.Settings, _ -> sendSettingsMessage userId
            | Equals Buttons.IncludePlaylist, Authorized -> askForReply Messages.SendIncludedPlaylist
            | Equals Buttons.ExcludePlaylist, Authorized -> askForReply Messages.SendExcludedPlaylist
            | Equals Buttons.TargetPlaylist, Authorized -> askForReply Messages.SendTargetedPlaylist
            | Equals "Back", _ -> sendCurrentPresetInfo userId

            | _ -> replyToMessage "Unknown command"
        | _ -> Task.FromResult()
    }
